using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceRoller))]
[RequireComponent(typeof(D6DiceDamage))]
[RequireComponent(typeof(DiceSpellLoadout))]
[RequireComponent(typeof(DicePlayerProgress))]
[RequireComponent(typeof(DicePlayerHealth))]
public sealed class DiceBattleController : MonoBehaviour
{
    [SerializeField] private D6DiceRoller diceRoller;
    [SerializeField] private D6DiceDamage diceDamage;
    [SerializeField] private DiceSpellLoadout spellLoadout;
    [SerializeField] private DicePlayerProgress playerProgress;
    [SerializeField] private DicePlayerHealth playerHealth;
    [SerializeField] private DiceEnemyAI enemy;
    [SerializeField] private DiceEnemyAI enemyPrefab;
    [SerializeField] private DiceSpellVfxController spellVfx;
    [SerializeField] private Vector3 enemySpawnPosition = new(0f, 2.5f, 2.5f);
    [SerializeField, Min(0.25f)] private float enemySpacing = 1.45f;
    [SerializeField, Min(1)] private int twoEnemyStartStage = 5;
    [SerializeField, Min(1)] private int threeEnemyStartStage = 10;
    [SerializeField, Min(1)] private int enemySpellStartStage = 5;
    [SerializeField, Min(0)] private int enemyFireballDefaultDamage = 1;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button retryLastLevelButton;

    private readonly List<DiceEnemyAI> activeEnemies = new();
    private int currentLevel = 1;
    private int retryLevel = 1;
    private bool canRetryLastLevel;
    private Coroutine levelTransitionRoutine;
    private DiceSpellDefinition enemyFireballSpell;
    private int currentEnemyCount = 1;
    private bool playerRollInputRequested = true;
    private MobileGameUiController mobileUi;

    private void Awake()
    {
        if (diceRoller == null)
        {
            diceRoller = GetComponent<D6DiceRoller>();
        }

        if (diceDamage == null)
        {
            diceDamage = GetComponent<D6DiceDamage>();
        }

        if (spellLoadout == null)
        {
            spellLoadout = GetComponent<DiceSpellLoadout>();
        }

        if (spellLoadout == null)
        {
            spellLoadout = gameObject.AddComponent<DiceSpellLoadout>();
        }

        if (TryGetComponent(out D6DiceVisual diceVisual))
        {
            diceVisual.SetSpellLoadout(spellLoadout);
        }

        if (playerProgress == null)
        {
            playerProgress = GetComponent<DicePlayerProgress>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<DicePlayerHealth>();
        }

        if (spellVfx == null)
        {
            spellVfx = GetComponent<DiceSpellVfxController>();
        }

        if (spellVfx == null)
        {
            spellVfx = gameObject.AddComponent<DiceSpellVfxController>();
        }

        EnsurePrimaryEnemy();
        ConfigureEnemiesForCurrentLevel();
        ConfigureCameraForMobile();
        BindUiIfAvailable();
        SetPlayerRollInput(true);
        RefreshLevelUi();
    }

    private void OnEnable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished += HandleRollFinished;
        }

        SubscribeEnemies();

        if (playerHealth != null)
        {
            playerHealth.Defeated += HandlePlayerDefeated;
        }

        if (retryLastLevelButton != null)
        {
            retryLastLevelButton.onClick.AddListener(RetryLastLevel);
        }

        SubscribeMobileUi();
        ApplyPlayerRollInput();
    }

    private void OnDisable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished -= HandleRollFinished;
        }

        for (var i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].Defeated -= HandleEnemyDefeated;
            }
        }

        if (playerHealth != null)
        {
            playerHealth.Defeated -= HandlePlayerDefeated;
        }

        if (retryLastLevelButton != null)
        {
            retryLastLevelButton.onClick.RemoveListener(RetryLastLevel);
        }

        UnsubscribeMobileUi();
    }

    private void HandleRollFinished(int _)
    {
        EnsurePrimaryEnemy();
        enemy = GetPlayerTargetEnemy();

        if (enemy == null || diceDamage == null)
        {
            return;
        }

        var topFacePip = diceRoller != null
            ? Mathf.Clamp(diceRoller.CurrentValue, 1, 6)
            : Mathf.Clamp(diceDamage.DiceValue, 1, 6);

        StartCoroutine(ResolvePlayerTurn(topFacePip));
    }

    private IEnumerator ResolvePlayerTurn(int topFacePip)
    {
        SetPlayerRollInput(false);

        var targetEnemy = GetPlayerTargetEnemy();
        enemy = targetEnemy;

        var spell = spellLoadout != null ? spellLoadout.GetSpellForPip(topFacePip) : null;
        if (spellVfx != null && spell != null)
        {
            yield return spellVfx.PlayCast(spell, transform, targetEnemy);
        }

        if (targetEnemy == null || targetEnemy.IsDefeated || diceDamage == null)
        {
            SetPlayerRollInput(true);
            yield break;
        }

        var baseDamage = diceDamage.DefaultDamage * topFacePip;
        var spellDamage = spellLoadout != null
            ? spellLoadout.CastSpellForPip(topFacePip, diceDamage, targetEnemy)
            : 0;
        var totalDamage = baseDamage + spellDamage;

        var enemyWasDefeated = targetEnemy.TakeDamage(totalDamage);

        if (enemyWasDefeated || targetEnemy.IsDefeated)
        {
            if (!AreAllEnemiesDefeated())
            {
                yield return ExecuteEnemyTurns();
            }

            yield break;
        }

        yield return ExecuteEnemyTurns();
    }

    private void ApplyEnemyDamage()
    {
        if (enemy == null || enemy.IsDefeated || playerHealth == null)
        {
            SetPlayerRollInput(true);
            return;
        }

        playerHealth.TakeDamage(enemy.CurrentDamage);
        SetPlayerRollInput(true);
    }

    private IEnumerator ExecuteEnemyTurns()
    {
        for (var i = 0; i < currentEnemyCount && i < activeEnemies.Count; i++)
        {
            var actingEnemy = activeEnemies[i];
            if (actingEnemy == null || actingEnemy.IsDefeated)
            {
                continue;
            }

            enemy = actingEnemy;

            var defeatedByStatuses = actingEnemy.AdvanceTurnStatuses(out var skipEnemyAction);
            if (defeatedByStatuses || actingEnemy.IsDefeated)
            {
                if (AreAllEnemiesDefeated())
                {
                    yield break;
                }

                continue;
            }

            if (skipEnemyAction)
            {
                continue;
            }

            ConfigureEnemySpellForRound(actingEnemy);

            var finished = false;
            actingEnemy.SpinThenAct(() => finished = true);
            yield return new WaitUntil(() => finished || actingEnemy == null || actingEnemy.IsDefeated);

            if (actingEnemy == null || actingEnemy.IsDefeated || playerHealth == null)
            {
                continue;
            }

            var damage = actingEnemy.CurrentDamage;
            var actionPip = Mathf.Clamp(actingEnemy.CurrentValue, 1, 6);
            var enemySpell = GetEnemySpellForPip(actingEnemy, actionPip);

            if (enemySpell != null)
            {
                if (spellVfx != null)
                {
                    yield return spellVfx.PlayCastToTransform(enemySpell, actingEnemy.transform, transform);
                }

                damage += CalculateEnemySpellImmediateDamage(actingEnemy, actionPip, enemySpell);
            }

            var playerDefeated = playerHealth.TakeDamage(damage);
            if (playerDefeated)
            {
                yield break;
            }
        }

        enemy = GetPlayerTargetEnemy();
        SetPlayerRollInput(true);
    }

    private void HandleEnemyDefeated(DiceEnemyAI defeatedEnemy, int coinReward)
    {
        if (playerProgress != null)
        {
            playerProgress.AddCoins(coinReward);
        }

        if (!AreAllEnemiesDefeated())
        {
            enemy = GetPlayerTargetEnemy();
            return;
        }

        currentLevel++;
        canRetryLastLevel = false;
        retryLevel = currentLevel;
        RefreshLevelUi();

        if (levelTransitionRoutine != null)
        {
            StopCoroutine(levelTransitionRoutine);
        }

        levelTransitionRoutine = StartCoroutine(ConfigureNextLevelAfterRespawn(defeatedEnemy));
    }

    private void HandlePlayerDefeated()
    {
        retryLevel = currentLevel;
        currentLevel = Mathf.Max(1, currentLevel - 1);
        canRetryLastLevel = retryLevel > currentLevel;

        if (playerHealth != null)
        {
            playerHealth.RestoreFull();
        }

        ConfigureEnemiesForCurrentLevel();
        SetPlayerRollInput(true);
        RefreshLevelUi();
    }

    private void RetryLastLevel()
    {
        if (!canRetryLastLevel)
        {
            return;
        }

        currentLevel = retryLevel;
        canRetryLastLevel = false;

        if (playerHealth != null)
        {
            playerHealth.RestoreFull();
        }

        ConfigureEnemiesForCurrentLevel();
        SetPlayerRollInput(true);
        RefreshLevelUi();
    }

    private void EnsurePrimaryEnemy()
    {
        if (enemy != null)
        {
            return;
        }

        enemy = enemyPrefab != null
            ? Instantiate(enemyPrefab, enemySpawnPosition, Quaternion.identity)
            : FindFirstObjectByType<DiceEnemyAI>();

        if (enemy != null)
        {
            enemy.transform.position = enemySpawnPosition;
        }

        ConfigureCameraForMobile();
        SubscribeEnemies();
    }

    private void ConfigureEnemiesForCurrentLevel()
    {
        EnsurePrimaryEnemy();

        if (enemy == null)
        {
            return;
        }

        currentEnemyCount = GetEnemyCountForLevel(currentLevel);
        EnsureEnemyRoster(currentEnemyCount);

        for (var i = 0; i < currentEnemyCount; i++)
        {
            var stageEnemy = activeEnemies[i];
            stageEnemy.SetAutoRespawn(false);
            stageEnemy.gameObject.SetActive(true);
            stageEnemy.transform.position = GetEnemyPosition(i, currentEnemyCount);
            stageEnemy.ConfigureForLevel(currentLevel);
            ClearEnemySpell(stageEnemy);
        }

        for (var i = currentEnemyCount; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null)
            {
                activeEnemies[i].SetBattleVisible(false);
            }
        }

        enemy = GetPlayerTargetEnemy();
        SubscribeEnemies();
        ConfigureCameraForMobile();
    }

    private IEnumerator ConfigureNextLevelAfterRespawn(DiceEnemyAI defeatedEnemy)
    {
        SetPlayerRollInput(false);

        if (defeatedEnemy != null && defeatedEnemy.RespawnDelay > 0f)
        {
            yield return new WaitForSeconds(defeatedEnemy.RespawnDelay);
        }

        ConfigureEnemiesForCurrentLevel();
        SetPlayerRollInput(true);
        levelTransitionRoutine = null;
    }

    private void SetPlayerRollInput(bool enabled)
    {
        playerRollInputRequested = enabled;
        ApplyPlayerRollInput();
    }

    private void ApplyPlayerRollInput()
    {
        if (diceRoller != null)
        {
            diceRoller.SetInputEnabled(playerRollInputRequested && IsMainViewAllowed());
        }
    }

    private bool IsMainViewAllowed()
    {
        return mobileUi == null || mobileUi.IsMainViewActive;
    }

    private void SubscribeEnemies()
    {
        for (var i = 0; i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] == null)
            {
                continue;
            }

            activeEnemies[i].Defeated -= HandleEnemyDefeated;
            activeEnemies[i].Defeated += HandleEnemyDefeated;
        }

        if (enemy != null && !activeEnemies.Contains(enemy))
        {
            enemy.Defeated -= HandleEnemyDefeated;
            enemy.Defeated += HandleEnemyDefeated;
        }
    }

    private void BindUiIfAvailable()
    {
        mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi == null)
        {
            return;
        }

        SubscribeMobileUi();

        if (levelText == null)
        {
            levelText = mobileUi.LevelText;
        }

        if (retryLastLevelButton == null)
        {
            retryLastLevelButton = mobileUi.RetryLevelButton;
        }
    }

    private void SubscribeMobileUi()
    {
        if (mobileUi == null)
        {
            mobileUi = MobileGameUiController.FindExisting();
        }

        if (mobileUi == null)
        {
            return;
        }

        mobileUi.MainViewActiveChanged -= HandleMainViewActiveChanged;
        mobileUi.MainViewActiveChanged += HandleMainViewActiveChanged;
    }

    private void UnsubscribeMobileUi()
    {
        if (mobileUi != null)
        {
            mobileUi.MainViewActiveChanged -= HandleMainViewActiveChanged;
        }
    }

    private void HandleMainViewActiveChanged(bool _)
    {
        ApplyPlayerRollInput();
    }

    private void ConfigureCameraForMobile()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null)
        {
            return;
        }

        var fitter = mainCamera.GetComponent<MobilePortraitCameraFitter>();
        if (fitter == null)
        {
            fitter = mainCamera.gameObject.AddComponent<MobilePortraitCameraFitter>();
        }

        fitter.SetTargets(transform, enemy != null ? enemy.transform : null);
    }

    private int GetEnemyCountForLevel(int level)
    {
        if (level >= threeEnemyStartStage)
        {
            return 3;
        }

        return level >= twoEnemyStartStage ? 2 : 1;
    }

    private void EnsureEnemyRoster(int count)
    {
        if (activeEnemies.Count == 0 && enemy != null)
        {
            activeEnemies.Add(enemy);
        }

        while (activeEnemies.Count < count)
        {
            var source = enemyPrefab != null ? enemyPrefab : enemy;
            if (source == null)
            {
                return;
            }

            var spawnedEnemy = Instantiate(source, GetEnemyPosition(activeEnemies.Count, count), Quaternion.identity);
            spawnedEnemy.RecreateRuntimeUi();
            activeEnemies.Add(spawnedEnemy);
        }
    }

    private Vector3 GetEnemyPosition(int index, int total)
    {
        var centerOffset = (total - 1) * 0.5f;
        return enemySpawnPosition + Vector3.right * ((index - centerOffset) * enemySpacing);
    }

    private DiceEnemyAI GetPlayerTargetEnemy()
    {
        for (var i = 0; i < currentEnemyCount && i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].IsDefeated)
            {
                return activeEnemies[i];
            }
        }

        return enemy != null && !enemy.IsDefeated ? enemy : null;
    }

    private bool AreAllEnemiesDefeated()
    {
        for (var i = 0; i < currentEnemyCount && i < activeEnemies.Count; i++)
        {
            if (activeEnemies[i] != null && !activeEnemies[i].IsDefeated)
            {
                return false;
            }
        }

        return activeEnemies.Count > 0;
    }

    private DiceSpellLoadout GetOrCreateEnemyLoadout(DiceEnemyAI stageEnemy)
    {
        if (stageEnemy == null)
        {
            return null;
        }

        var enemyLoadout = stageEnemy.GetComponent<DiceSpellLoadout>();
        if (enemyLoadout == null)
        {
            enemyLoadout = stageEnemy.gameObject.AddComponent<DiceSpellLoadout>();
        }

        if (stageEnemy.TryGetComponent(out D6DiceVisual enemyDiceVisual))
        {
            enemyDiceVisual.SetSpellLoadout(enemyLoadout);
        }

        return enemyLoadout;
    }

    private void ClearEnemySpell(DiceEnemyAI stageEnemy)
    {
        var enemyLoadout = GetOrCreateEnemyLoadout(stageEnemy);
        if (enemyLoadout == null)
        {
            return;
        }

        for (var pip = 1; pip <= 6; pip++)
        {
            enemyLoadout.UnequipSpell(pip);
        }
    }

    private void ConfigureEnemySpellForRound(DiceEnemyAI stageEnemy)
    {
        var enemyLoadout = GetOrCreateEnemyLoadout(stageEnemy);
        if (enemyLoadout == null)
        {
            return;
        }

        ClearEnemySpell(stageEnemy);

        if (currentLevel < enemySpellStartStage)
        {
            return;
        }

        var randomPip = Random.Range(1, 7);
        enemyLoadout.EquipSpell(randomPip, GetEnemyFireballSpell());
    }

    private DiceSpellDefinition GetEnemySpellForPip(DiceEnemyAI stageEnemy, int pip)
    {
        if (stageEnemy == null || currentLevel < enemySpellStartStage)
        {
            return null;
        }

        var enemyLoadout = stageEnemy.GetComponent<DiceSpellLoadout>();
        return enemyLoadout != null ? enemyLoadout.GetSpellForPip(pip) : null;
    }

    private int CalculateEnemySpellImmediateDamage(DiceEnemyAI stageEnemy, int pip, DiceSpellDefinition spell)
    {
        if (stageEnemy == null || spell == null)
        {
            return 0;
        }

        var rawSpellDamage = stageEnemy.CurrentDamage
                             * Mathf.Clamp(pip, 1, 6)
                             * Mathf.Max(0, spell.SpellDefaultDamage);
        return Mathf.Max(0, Mathf.RoundToInt(rawSpellDamage * spell.ImmediateDamageMultiplier));
    }

    private DiceSpellDefinition GetEnemyFireballSpell()
    {
        if (enemyFireballSpell != null)
        {
            return enemyFireballSpell;
        }

        var fireballSprite = Resources.Load<Sprite>("MobileUI/Spells/Fire ball");
        enemyFireballSpell = DiceSpellDefinition.CreateRuntime(
            "enemy_fire_blast",
            "Enemy Fire Blast",
            fireballSprite,
            enemyFireballDefaultDamage,
            DiceSpellEffectType.FireBlast);
        return enemyFireballSpell;
    }

    private void RefreshLevelUi()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi != null)
        {
            mobileUi.SetLevel(currentLevel);
            mobileUi.SetRetryButtonVisible(canRetryLastLevel);
        }

        if (playerProgress != null)
        {
            playerProgress.SetCurrentLevel(currentLevel);
        }

        if (mobileUi == null && levelText != null)
        {
            levelText.text = $"LEVEL {currentLevel}";
        }

        if (mobileUi == null && retryLastLevelButton != null)
        {
            retryLastLevelButton.gameObject.SetActive(canRetryLastLevel);
        }
    }

}
