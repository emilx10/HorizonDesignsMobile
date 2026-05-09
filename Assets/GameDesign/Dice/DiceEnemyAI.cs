using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class DiceEnemyAI : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 10;
    [SerializeField, Min(0)] private int damage = 1;
    [SerializeField, Min(0)] private int experienceReward = 1;
    [SerializeField, Min(0f)] private float respawnDelay = 1.25f;
    [SerializeField, Min(0.2f)] private float rollDuration = 0.8f;
    [SerializeField, Min(1)] private int extraSpinTurns = 2;
    [SerializeField, Min(0f)] private float swingHeight = 0.25f;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Text healthText;
    [SerializeField] private Text damageText;
    [SerializeField] private Vector3 healthTextOffset = new(0f, 0.85f, 0f);
    [SerializeField] private Vector3 damageTextOffset = new(0f, -0.85f, 0f);

    public event Action<DiceEnemyAI, int> Defeated;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }
    public int Damage => damage;
    public int ExperienceReward => experienceReward;
    public bool IsDefeated { get; private set; }
    public bool IsRolling { get; private set; }

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
            healthText.rectTransform.position = targetCamera.WorldToScreenPoint(transform.position + healthTextOffset);
        }

        if (damageText != null)
        {
            damageText.rectTransform.position = targetCamera.WorldToScreenPoint(transform.position + damageTextOffset);
        }
    }

    public void TakeDamage(int amount)
    {
        if (IsDefeated)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));
        RefreshUi();

        if (CurrentHealth <= 0)
        {
            Defeat();
        }
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

    private void Defeat()
    {
        IsDefeated = true;
        Defeated?.Invoke(this, experienceReward);

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
        var targetEuler = startEuler + new Vector3(
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
            transform.rotation = Quaternion.Euler(Vector3.LerpUnclamped(startEuler, targetEuler, eased));

            yield return null;
        }

        transform.position = homePosition;
        transform.rotation = Quaternion.Euler(targetEuler);
        IsRolling = false;
        rollRoutine = null;

        if (!IsDefeated)
        {
            onComplete?.Invoke();
        }
    }

    private void SetVisible(bool visible)
    {
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
    }

    private void RefreshUi()
    {
        if (healthText != null)
        {
            healthText.text = $"ENEMY HP {CurrentHealth}/{maxHealth}";
        }

        if (damageText != null)
        {
            damageText.text = $"ENEMY DMG {damage}";
        }
    }

    private static Text CreateText(string objectName, int fontSize, Color color)
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Dice UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var textObject = new GameObject(objectName, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvas.transform, false);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = fontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = color;
        text.raycastTarget = false;
        text.rectTransform.sizeDelta = new Vector2(360f, 72f);

        return text;
    }
}
