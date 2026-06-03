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
    [SerializeField, Min(0.2f)] private float rollDuration = 0.8f;
    [SerializeField, Min(1)] private int extraSpinTurns = 2;
    [SerializeField, Min(0f)] private float swingHeight = 0.25f;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Text healthText;
    [SerializeField] private Text damageText;
    [SerializeField] private Image healthBarFill;
    [SerializeField] private Vector3 healthTextOffset = new(0f, 0.85f, 0f);
    [SerializeField] private Vector3 healthBarOffset = new(0f, 0.68f, 0f);
    [SerializeField] private Vector3 damageTextOffset = new(0f, -0.85f, 0f);

    public event Action<DiceEnemyAI, int> Defeated;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public float DamageMultiplier { get; private set; } = 1f;
    public int CurrentValue { get; private set; } = 1;
    public float CurrentDamage => CurrentValue * DamageMultiplier;
    public int CoinReward { get; private set; } = 1;
    public bool IsDefeated { get; private set; }
    public bool IsRolling { get; private set; }
    public float RespawnDelay => respawnDelay;

    private Renderer[] renderers;
    private Collider[] colliders;
    private Coroutine respawnRoutine;
    private Coroutine rollRoutine;
    private Vector3 homePosition;

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

    public void SpinThenAct(Action onComplete)
    {
        if (IsDefeated)
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
        SetVisible(true);
        RefreshUi();
    }

    private void Defeat()
    {
        IsDefeated = true;
        Defeated?.Invoke(this, CoinReward);

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
            damageText.text = $"ENEMY DMG {FormatNumber(CurrentDamage)}";
        }
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
