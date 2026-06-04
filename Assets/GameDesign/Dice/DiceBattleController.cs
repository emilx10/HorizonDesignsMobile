using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceRoller))]
[RequireComponent(typeof(D6DiceDamage))]
[RequireComponent(typeof(DicePlayerProgress))]
[RequireComponent(typeof(DicePlayerHealth))]
public sealed class DiceBattleController : MonoBehaviour
{
    [SerializeField] private D6DiceRoller diceRoller;
    [SerializeField] private D6DiceDamage diceDamage;
    [SerializeField] private DicePlayerProgress playerProgress;
    [SerializeField] private DicePlayerHealth playerHealth;
    [SerializeField] private DiceEnemyAI enemy;
    [SerializeField] private DiceEnemyAI enemyPrefab;
    [SerializeField] private Vector3 enemySpawnPosition = new(0f, 2.5f, 2.5f);
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button retryLastLevelButton;

    private int currentLevel = 1;
    private int retryLevel = 1;
    private bool canRetryLastLevel;
    private Coroutine levelTransitionRoutine;

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

        if (playerProgress == null)
        {
            playerProgress = GetComponent<DicePlayerProgress>();
        }

        if (playerHealth == null)
        {
            playerHealth = GetComponent<DicePlayerHealth>();
        }

        EnsureEnemy();
        ConfigureEnemyForCurrentLevel();
        ConfigureCameraForMobile();
        SetPlayerRollInput(true);
        BindUiIfAvailable();
        RefreshLevelUi();
    }

    private void OnEnable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished += HandleRollFinished;
        }

        SubscribeEnemy();

        if (playerHealth != null)
        {
            playerHealth.Defeated += HandlePlayerDefeated;
        }

        if (retryLastLevelButton != null)
        {
            retryLastLevelButton.onClick.AddListener(RetryLastLevel);
        }
    }

    private void OnDisable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished -= HandleRollFinished;
        }

        if (enemy != null)
        {
            enemy.Defeated -= HandleEnemyDefeated;
        }

        if (playerHealth != null)
        {
            playerHealth.Defeated -= HandlePlayerDefeated;
        }

        if (retryLastLevelButton != null)
        {
            retryLastLevelButton.onClick.RemoveListener(RetryLastLevel);
        }
    }

    private void HandleRollFinished(int _)
    {
        EnsureEnemy();

        if (enemy == null || diceDamage == null)
        {
            return;
        }

        var enemyWasDefeated = enemy.TakeDamage(diceDamage.DiceDamage);

        if (!enemyWasDefeated && !enemy.IsDefeated && playerHealth != null)
        {
            SetPlayerRollInput(false);
            enemy.SpinThenAct(ApplyEnemyDamage);
        }
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

    private void HandleEnemyDefeated(DiceEnemyAI defeatedEnemy, int coinReward)
    {
        if (playerProgress != null)
        {
            playerProgress.AddCoins(coinReward);
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

        ConfigureEnemyForCurrentLevel();
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

        ConfigureEnemyForCurrentLevel();
        SetPlayerRollInput(true);
        RefreshLevelUi();
    }

    private void EnsureEnemy()
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
        SubscribeEnemy();
    }

    private void ConfigureEnemyForCurrentLevel()
    {
        EnsureEnemy();

        if (enemy != null)
        {
            enemy.transform.position = enemySpawnPosition;
            enemy.ConfigureForLevel(currentLevel);
        }
    }

    private IEnumerator ConfigureNextLevelAfterRespawn(DiceEnemyAI defeatedEnemy)
    {
        SetPlayerRollInput(false);

        if (defeatedEnemy != null && defeatedEnemy.RespawnDelay > 0f)
        {
            yield return new WaitForSeconds(defeatedEnemy.RespawnDelay);
        }

        ConfigureEnemyForCurrentLevel();
        SetPlayerRollInput(true);
        levelTransitionRoutine = null;
    }

    private void SetPlayerRollInput(bool enabled)
    {
        if (diceRoller != null)
        {
            diceRoller.SetInputEnabled(enabled);
        }
    }

    private void SubscribeEnemy()
    {
        if (enemy != null)
        {
            enemy.Defeated -= HandleEnemyDefeated;
            enemy.Defeated += HandleEnemyDefeated;
        }
    }

    private void BindUiIfAvailable()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi == null)
        {
            return;
        }

        if (levelText == null)
        {
            levelText = mobileUi.LevelText;
        }

        if (retryLastLevelButton == null)
        {
            retryLastLevelButton = mobileUi.RetryLevelButton;
        }
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

    private void RefreshLevelUi()
    {
        if (playerProgress != null)
        {
            playerProgress.SetCurrentLevel(currentLevel);
        }

        if (levelText != null)
        {
            levelText.text = $"LEVEL {currentLevel}";
        }

        if (retryLastLevelButton != null)
        {
            retryLastLevelButton.gameObject.SetActive(canRetryLastLevel);
        }
    }

}
