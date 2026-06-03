using UnityEngine;
using UnityEngine.UI;
using System;

[DisallowMultipleComponent]
public sealed class DicePlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField] private Text healthText;

    public event Action Defeated;

    public int MaxHealth => maxHealth;
    public float CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CreateUiIfNeeded();
        RefreshUi();
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

    private void CreateUiIfNeeded()
    {
        if (healthText != null)
        {
            return;
        }

        var uiRoot = MobileLayoutUtility.GetUiRoot();

        var textObject = new GameObject("Player Health Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(uiRoot, false);

        healthText = textObject.GetComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 38;
        healthText.fontStyle = FontStyle.Bold;
        healthText.alignment = TextAnchor.MiddleLeft;
        healthText.color = Color.green;
        healthText.raycastTarget = false;
        MobileLayoutUtility.ConfigureText(healthText, 38, 22);

        var rect = healthText.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(32f, -108f);
        rect.sizeDelta = new Vector2(360f, 80f);
    }

    private void RefreshUi()
    {
        if (healthText != null)
        {
            healthText.text = $"HP {FormatNumber(CurrentHealth)}/{maxHealth}";
        }
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }
}
