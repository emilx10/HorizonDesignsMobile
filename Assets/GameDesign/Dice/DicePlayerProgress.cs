using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem.UI;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceDamage))]
[RequireComponent(typeof(D6DiceDamageDisplay))]
public sealed class DicePlayerProgress : MonoBehaviour
{
    [SerializeField, Min(1)] private int experienceToUpgrade = 2;
    [SerializeField, Min(1)] private int damageIncreasePerUpgrade = 1;
    [SerializeField] private D6DiceDamage diceDamage;
    [SerializeField] private D6DiceDamageDisplay damageDisplay;
    [SerializeField] private Text experienceText;
    [SerializeField] private Button upgradeButton;

    public int Experience { get; private set; }

    private void Awake()
    {
        if (diceDamage == null)
        {
            diceDamage = GetComponent<D6DiceDamage>();
        }

        if (damageDisplay == null)
        {
            damageDisplay = GetComponent<D6DiceDamageDisplay>();
        }

        CreateUiIfNeeded();
        RefreshUi();
    }

    private void OnEnable()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.AddListener(UpgradeDamage);
        }
    }

    private void OnDisable()
    {
        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(UpgradeDamage);
        }
    }

    public void AddExperience(int amount)
    {
        Experience += Mathf.Max(0, amount);
        RefreshUi();
    }

    public void UpgradeDamage()
    {
        if (Experience < experienceToUpgrade || diceDamage == null)
        {
            return;
        }

        Experience -= experienceToUpgrade;
        diceDamage.AddDefaultDamage(damageIncreasePerUpgrade);

        if (damageDisplay != null)
        {
            damageDisplay.Refresh();
        }

        RefreshUi();
    }

    private void CreateUiIfNeeded()
    {
        EnsureEventSystem();

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

        if (experienceText == null)
        {
            var textObject = new GameObject("Experience Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(canvas.transform, false);

            experienceText = textObject.GetComponent<Text>();
            experienceText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            experienceText.fontSize = 38;
            experienceText.fontStyle = FontStyle.Bold;
            experienceText.alignment = TextAnchor.MiddleLeft;
            experienceText.color = Color.white;
            experienceText.raycastTarget = false;

            var rect = experienceText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(32f, -32f);
            rect.sizeDelta = new Vector2(360f, 80f);
        }

        if (upgradeButton == null)
        {
            var buttonObject = new GameObject("Upgrade Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(canvas.transform, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-32f, 0f);
            rect.sizeDelta = new Vector2(260f, 92f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.44f, 0.9f, 0.95f);

            upgradeButton = buttonObject.GetComponent<Button>();

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
            label.text = "UPGRADE";
            label.raycastTarget = false;
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

    private void RefreshUi()
    {
        if (experienceText != null)
        {
            experienceText.text = $"EXP {Experience}/{experienceToUpgrade}";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = Experience >= experienceToUpgrade;
        }
    }
}
