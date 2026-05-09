using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DicePlayerHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maxHealth = 20;
    [SerializeField] private Text healthText;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }

    private void Awake()
    {
        CurrentHealth = maxHealth;
        CreateUiIfNeeded();
        RefreshUi();
    }

    public void TakeDamage(int amount)
    {
        CurrentHealth = Mathf.Max(0, CurrentHealth - Mathf.Max(0, amount));

        if (CurrentHealth <= 0)
        {
            CurrentHealth = maxHealth;
        }

        RefreshUi();
    }

    private void CreateUiIfNeeded()
    {
        if (healthText != null)
        {
            return;
        }

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

        var textObject = new GameObject("Player Health Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvas.transform, false);

        healthText = textObject.GetComponent<Text>();
        healthText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        healthText.fontSize = 38;
        healthText.fontStyle = FontStyle.Bold;
        healthText.alignment = TextAnchor.MiddleLeft;
        healthText.color = Color.green;
        healthText.raycastTarget = false;

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
            healthText.text = $"HP {CurrentHealth}/{maxHealth}";
        }
    }
}
