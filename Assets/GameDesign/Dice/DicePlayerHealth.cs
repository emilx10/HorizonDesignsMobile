using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

[DisallowMultipleComponent]
public sealed class DicePlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private Vector3 healthBarWorldOffset = new(0f, -0.85f, 0f);
    [SerializeField] private Image healthFillImage;
    [SerializeField] private TMP_Text healthText;

    public event Action Defeated;

    public int MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        BindUiIfAvailable();
        RefreshUi();
    }

    private void LateUpdate()
    {
        RefreshBarPosition();
    }

    public bool TakeDamage(float amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0f, amount));
        var wasDefeated = CurrentHealth <= 0;

        if (wasDefeated)
        {
            Defeated?.Invoke();
        }

        RefreshUi();
        return wasDefeated;
    }

    public void RestoreFull()
    {
        CurrentHealth = maxHealth;
        RefreshUi();
    }

    private void BindUiIfAvailable()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi == null || healthText != null)
        {
            return;
        }

        healthText = mobileUi.HealthText;

        if (healthFillImage == null)
        {
            healthFillImage = mobileUi.HealthFillImage;
        }
    }

    private void RefreshUi()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi != null)
        {
            mobileUi.SetHealth(CurrentHealth, maxHealth);
        }

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = maxHealth > 0 ? Mathf.Clamp01(CurrentHealth / maxHealth) : 0f;
        }

        if (mobileUi == null && healthText != null)
        {
            healthText.color = Color.white;
            healthText.text = $"{FormatNumber(CurrentHealth)}/{maxHealth}";
        }
    }

    private void RefreshBarPosition()
    {
        if (healthBarRoot == null)
        {
            return;
        }

        var canvas = healthBarRoot.GetComponentInParent<Canvas>();
        var canvasRect = canvas != null ? canvas.transform as RectTransform : null;
        var camera = Camera.main;

        if (canvasRect == null || camera == null)
        {
            return;
        }

        var screenPosition = camera.WorldToScreenPoint(transform.position + healthBarWorldOffset);
        if (screenPosition.z < 0f)
        {
            healthBarRoot.gameObject.SetActive(false);
            return;
        }

        healthBarRoot.gameObject.SetActive(true);

        var uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition, uiCamera, out var localPoint))
        {
            healthBarRoot.anchoredPosition = localPoint;
        }
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }
}
