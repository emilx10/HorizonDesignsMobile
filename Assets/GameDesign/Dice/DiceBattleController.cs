using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

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
    [SerializeField] private Text levelText;
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
        CreateUiIfNeeded();
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

    private void CreateUiIfNeeded()
    {
        EnsureEventSystem();

        var uiRoot = MobileLayoutUtility.GetUiRoot();

        if (levelText == null)
        {
            var textObject = new GameObject("Level Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(uiRoot, false);

            levelText = textObject.GetComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            levelText.fontSize = 40;
            levelText.fontStyle = FontStyle.Bold;
            levelText.alignment = TextAnchor.UpperCenter;
            levelText.color = Color.white;
            levelText.raycastTarget = false;
            MobileLayoutUtility.ConfigureText(levelText, 40, 24);

            var rect = levelText.rectTransform;
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -32f);
            rect.sizeDelta = new Vector2(360f, 80f);
        }

        if (retryLastLevelButton == null)
        {
            retryLastLevelButton = CreateButton(uiRoot, "Retry Last Level Button", "RETRY", new Vector2(-32f, -112f));
        }
    }

    private static Button CreateButton(Transform parent, string objectName, string labelText, Vector2 anchoredPosition)
    {
        var buttonObject = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        var rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0.5f);
        rect.anchorMax = new Vector2(1f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(260f, 92f);

        var image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.78f, 0.28f, 0.18f, 0.95f);

        var button = buttonObject.GetComponent<Button>();

        var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);

        var labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 34;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.text = labelText;
        label.raycastTarget = false;
        MobileLayoutUtility.ConfigureText(label, 34, 20);

        return button;
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

    private static void EnsureEventSystem()
    {
        if (FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
#else
        eventSystemObject.AddComponent<StandaloneInputModule>();
#endif
    }
}
