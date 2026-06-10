using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DiceEnemyAI : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(0.1f)] private float baseDamageMultiplier = 1f;
    [SerializeField, Min(1)] private int levelsPerDamageStep = 5;
    [SerializeField, Min(0f)] private float damageMultiplierStep = 0.5f;
    [SerializeField, Min(1)] private int baseCoinReward = 1;
    [SerializeField, Min(1)] private int levelsPerCoinRewardStep = 5;
    [SerializeField, Min(0f)] private float respawnDelay = 1.25f;
    [SerializeField] private bool autoRespawn = true;
    [SerializeField, Min(0.2f)] private float rollDuration = 0.8f;
    [SerializeField, Min(1)] private int extraSpinTurns = 2;
    [SerializeField, Min(0f)] private float swingHeight = 0.25f;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Text healthText;
    [SerializeField] private Text damageText;
    [SerializeField] private Text burnIndicatorText;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Vector3 healthTextOffset = new(0f, 0.85f, 0f);
    [SerializeField] private Vector3 healthBarOffset = new(0f, 0.68f, 0f);
    [SerializeField] private Vector3 damageTextOffset = new(0f, -0.85f, 0f);
    [SerializeField] private Vector3 burnIndicatorOffset = new(0.55f, 0.85f, 0f);

    public event Action<DiceEnemyAI, int> Defeated;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public int CurrentValue { get; private set; } = 1;
    public float CurrentDamage => CurrentValue * DamageMultiplier;
    public int CoinReward { get; private set; } = 1;
    public bool IsDefeated { get; private set; }
    public bool IsRolling { get; private set; }
    public bool IsBurning => burnTurnsRemaining > 0;
    public bool IsFrozen => frozenTurnsRemaining > 0;
    public int BurnTurnsRemaining => burnTurnsRemaining;
    public int BurnDamagePerTurn => burnDamagePerTurn;
    public int FreezeStacks => freezeStacks;
    public int FrozenTurnsRemaining => frozenTurnsRemaining;
    public float RespawnDelay => respawnDelay;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Coroutine respawnRoutine;
    private Coroutine rollRoutine;
    private Vector3 homePosition;
    private int burnTurnsRemaining;
    private int burnDamagePerTurn;
    private int freezeStacks;
    private int freezeThreshold = 10;
    private int frozenTurnsRemaining;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        homePosition = transform.position;
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (healthText == null)
        {
            healthText = CreateText("Enemy Health Text", 34, Color.red);
        }

        if (damageText == null)
        {
            damageText = CreateText("Enemy Damage Text", 34, Color.red);
        }

        if (burnIndicatorText == null)
        {
            burnIndicatorText = CreateText("Enemy Burn Indicator", 24, new Color(1f, 0.42f, 0.02f, 1f));
            burnIndicatorText.text = "BURN";
            burnIndicatorText.rectTransform.sizeDelta = new Vector2(110f, 42f);
        }

        if (healthBarFill == null)
        {
            healthBarFill = CreateHealthBar();
        }

        RefreshUi();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        if (healthText != null)
        {
            var rectTransform = healthText.rectTransform;
            var screenPosition = targetCamera.WorldToScreenPoint(transform.position + healthTextOffset);
            rectTransform.position = MobileLayoutUtility.ClampToScreen(screenPosition, rectTransform.sizeDelta);
        }

        if (healthBarFill != null)
        {
            var rootRect = (RectTransform)healthBarFill.transform.parent;
            var screenPosition = targetCamera.WorldToScreenPoint(transform.position + healthBarOffset);
            rootRect.position = MobileLayoutUtility.ClampToScreen(screenPosition, rootRect.sizeDelta);
        }

        if (damageText != null)
        {
            var rectTransform = damageText.rectTransform;
            var screenPosition = targetCamera.WorldToScreenPoint(transform.position + damageTextOffset);
            rectTransform.position = MobileLayoutUtility.ClampToScreen(screenPosition, rectTransform.sizeDelta);
        }

        if (burnIndicatorText != null)
        {
            var rectTransform = burnIndicatorText.rectTransform;
            var screenPosition = targetCamera.WorldToScreenPoint(transform.position + burnIndicatorOffset);
            rectTransform.position = MobileLayoutUtility.ClampToScreen(screenPosition, rectTransform.sizeDelta);
        }
    }

    public bool TakeDamage(int amount)
    {
        if (IsDefeated)
        {
            return false;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
        RefreshUi();

        if (CurrentHealth <= 0)
        {
            Defeat();
            return true;
        }

        return false;
    }

    public void ApplyBurn(int damagePerTurn, int turns)
    {
        if (IsDefeated)
        {
            return;
        }

        burnDamagePerTurn = Mathf.Max(burnDamagePerTurn, Mathf.Max(1, damagePerTurn));
        burnTurnsRemaining = Mathf.Max(1, turns);
        RefreshUi();
    }

    public void ApplyFreezeStacks(int amount, int threshold, int frozenTurns)
    {
        if (IsDefeated || IsFrozen)
        {
            return;
        }

        freezeThreshold = Mathf.Max(1, threshold);
        freezeStacks = Mathf.Min(freezeThreshold, freezeStacks + Mathf.Max(1, amount));

        if (freezeStacks >= freezeThreshold)
        {
            freezeStacks = 0;
            frozenTurnsRemaining = Mathf.Max(1, frozenTurns);
        }

        RefreshUi();
    }

    public bool AdvanceTurnStatuses(out bool skipAction)
    {
        skipAction = false;

        if (IsDefeated)
        {
            return true;
        }

        if (burnTurnsRemaining > 0)
        {
            var defeatedByBurn = TakeDamage(burnDamagePerTurn);
            burnTurnsRemaining = Mathf.Max(0, burnTurnsRemaining - 1);

            if (burnTurnsRemaining == 0)
            {
                burnDamagePerTurn = 0;
            }

            if (defeatedByBurn || IsDefeated)
            {
                return true;
            }
        }

        if (frozenTurnsRemaining > 0)
        {
            frozenTurnsRemaining = Mathf.Max(0, frozenTurnsRemaining - 1);
            skipAction = true;
        }

        RefreshUi();
        return false;
    }

    public void SpinThenAct(Action onComplete)
    {
        if (IsDefeated || IsFrozen)
        {
            return;
        }

        if (rollRoutine != null)
        {
            StopCoroutine(rollRoutine);
        }

        rollRoutine = StartCoroutine(SpinRoutine(onComplete));
    }

    public void ConfigureForLevel(int level)
    {
        level = Mathf.Max(1, level);
        maxHealth = 8 + (level * 2);
        DamageMultiplier = baseDamageMultiplier + (((level - 1) / levelsPerDamageStep) * damageMultiplierStep);
        CoinReward = baseCoinReward * (int)Mathf.Pow(2, (level - 1) / levelsPerCoinRewardStep);

        homePosition = transform.position;
        CurrentHealth = maxHealth;
        CurrentValue = GetCameraFacingValue();
        IsDefeated = false;
        ClearStatuses();
        SetVisible(true);
        RefreshUi();
    }

    public void SetAutoRespawn(bool enabled)
    {
        autoRespawn = enabled;
    }

    public void SetBattleVisible(bool visible)
    {
        SetVisible(visible);
    }

    public void RecreateRuntimeUi()
    {
        healthText = CreateText("Enemy Health Text", 34, Color.red);
        damageText = CreateText("Enemy Damage Text", 34, Color.red);
        burnIndicatorText = CreateText("Enemy Burn Indicator", 24, new Color(1f, 0.42f, 0.02f, 1f));
        burnIndicatorText.rectTransform.sizeDelta = new Vector2(110f, 42f);
        healthBarFill = CreateHealthBar();
        RefreshUi();
    }

    private void Defeat()
    {
        IsDefeated = true;
        Defeated?.Invoke(this, CoinReward);

        if (!autoRespawn)
        {
            SetVisible(false);
            return;
        }

        if (respawnRoutine != null)
        {
            StopCoroutine(respawnRoutine);
        }

        respawnRoutine = StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        SetVisible(false);
        yield return new WaitForSeconds(respawnDelay);

        CurrentHealth = maxHealth;
        IsDefeated = false;
        ClearStatuses();
        SetVisible(true);
        RefreshUi();
        respawnRoutine = null;
    }

    private IEnumerator SpinRoutine(Action onComplete)
    {
        IsRolling = true;

        var startPosition = transform.position;
        var startEuler = transform.eulerAngles;
        CurrentValue = UnityEngine.Random.Range(1, 7);

        var targetEuler = GetEulerShowingValueToCamera(CurrentValue);

        var animatedEndEuler = targetEuler + new Vector3(
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f,
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f,
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f);

        var time = 0f;
        while (time < rollDuration)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / rollDuration);
            var eased = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, homePosition, eased)
                                 + Vector3.up * (Mathf.Sin(t * Mathf.PI) * swingHeight);
            transform.rotation = Quaternion.Euler(Vector3.LerpUnclamped(startEuler, animatedEndEuler, eased));

            yield return null;
        }

        transform.position = homePosition;
        transform.rotation = Quaternion.Euler(targetEuler);
        CurrentValue = GetCameraFacingValue();
        RefreshUi();
        IsRolling = false;
        rollRoutine = null;

        if (!IsDefeated)
        {
            onComplete?.Invoke();
        }
    }

    private void SetVisible(bool visible)
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);

        foreach (var enemyRenderer in renderers)
        {
            enemyRenderer.enabled = visible;
        }

        foreach (var enemyCollider in colliders)
        {
            enemyCollider.enabled = visible;
        }

        if (healthText != null)
        {
            healthText.enabled = visible;
        }

        if (damageText != null)
        {
            damageText.enabled = visible;
        }

        if (burnIndicatorText != null)
        {
            burnIndicatorText.enabled = visible && IsBurning;
        }

        if (healthBarFill != null)
        {
            healthBarFill.transform.parent.gameObject.SetActive(visible);
        }
    }

    private void RefreshUi()
    {
        if (healthText != null)
        {
            healthText.text = $"ENEMY HP {CurrentHealth}/{maxHealth}";
        }

        if (healthBarFill != null)
        {
            var fillPercent = maxHealth > 0 ? Mathf.Clamp01((float)CurrentHealth / maxHealth) : 0f;
            healthBarFill.rectTransform.anchorMax = new Vector2(fillPercent, 1f);
            healthBarFill.rectTransform.offsetMax = new Vector2(-3f, -3f);
        }

        if (damageText != null)
        {
            var statusText = GetStatusText();
            damageText.text = string.IsNullOrEmpty(statusText)
                ? $"ENEMY DMG {FormatNumber(CurrentDamage)}"
                : $"ENEMY DMG {FormatNumber(CurrentDamage)}\n{statusText}";
        }

        if (burnIndicatorText != null)
        {
            burnIndicatorText.enabled = !IsDefeated && IsBurning;
            burnIndicatorText.text = $"BURN {burnTurnsRemaining}";
        }
    }

    private void ClearStatuses()
    {
        burnTurnsRemaining = 0;
        burnDamagePerTurn = 0;
        freezeStacks = 0;
        frozenTurnsRemaining = 0;
    }

    private string GetStatusText()
    {
        var statusText = string.Empty;

        if (burnTurnsRemaining > 0)
        {
            statusText = $"BURN {burnTurnsRemaining}";
        }

        if (frozenTurnsRemaining > 0)
        {
            return string.IsNullOrEmpty(statusText)
                ? $"FROZEN {frozenTurnsRemaining}"
                : $"{statusText}  FROZEN {frozenTurnsRemaining}";
        }

        if (freezeStacks > 0)
        {
            var freezeText = $"FREEZE {freezeStacks}/{freezeThreshold}";
            return string.IsNullOrEmpty(statusText) ? freezeText : $"{statusText}  {freezeText}";
        }

        return statusText;
    }

    private Vector3 GetEulerShowingValueToCamera(int value)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return Vector3.zero;
        }

        var directionToCamera = (targetCamera.transform.position - transform.position).normalized;
        var targetLocalNormal = GetLocalNormalForValue(value);
        var bestEuler = Vector3.zero;
        var bestDot = float.NegativeInfinity;

        for (var x = 0; x < 360; x += 90)
        {
            for (var y = 0; y < 360; y += 90)
            {
                for (var z = 0; z < 360; z += 90)
                {
                    var euler = new Vector3(x, y, z);
                    var rotation = Quaternion.Euler(euler);
                    var dot = Vector3.Dot(rotation * targetLocalNormal, directionToCamera);

                    if (dot > bestDot)
                    {
                        bestDot = dot;
                        bestEuler = euler;
                    }
                }
            }
        }

        return bestEuler;
    }

    private int GetCameraFacingValue()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return CurrentValue;
        }

        var directionToCamera = (targetCamera.transform.position - transform.position).normalized;
        var bestValue = 1;
        var bestDot = float.NegativeInfinity;

        CheckFace(Vector3.up, 1, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.down, 6, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.forward, 2, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.back, 5, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.right, 3, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.left, 4, directionToCamera, ref bestValue, ref bestDot);

        return bestValue;
    }

    private void CheckFace(Vector3 localNormal, int value, Vector3 directionToCamera, ref int bestValue, ref float bestDot)
    {
        var dot = Vector3.Dot(transform.TransformDirection(localNormal), directionToCamera);

        if (dot > bestDot)
        {
            bestDot = dot;
            bestValue = value;
        }
    }

    private static Vector3 GetLocalNormalForValue(int value)
    {
        return value switch
        {
            2 => Vector3.forward,
            3 => Vector3.right,
            4 => Vector3.left,
            5 => Vector3.back,
            6 => Vector3.down,
            _ => Vector3.up,
        };
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }

    private static Text CreateText(string objectName, int fontSize, Color color)
    {
        var uiRoot = MobileLayoutUtility.GetUiRoot();

        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(uiRoot, false);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(360f, 72f);
        MobileLayoutUtility.ConfigureText(text, fontSize, 18);

        return text;
    }

    private static Image CreateHealthBar()
    {
        var uiRoot = MobileLayoutUtility.GetUiRoot();

        var root = new GameObject("Enemy Health Bar", typeof(RectTransform), typeof(Image));
        root.transform.SetParent(uiRoot, false);

        var rootRect = root.GetComponent<RectTransform>();
        rootRect.sizeDelta = new Vector2(260f, 24f);

        var background = root.GetComponent<Image>();
        background.color = new Color(0.12f, 0.04f, 0.04f, 0.9f);
        background.raycastTarget = false;

        var fillObject = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillObject.transform.SetParent(root.transform, false);

        var fillRect = fillObject.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(3f, 3f);
        fillRect.offsetMax = new Vector2(-3f, -3f);

        var fill = fillObject.GetComponent<Image>();
        fill.color = new Color(0.9f, 0.08f, 0.06f, 1f);
        fill.raycastTarget = false;

        return fill;
    }
}
