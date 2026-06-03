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
    [SerializeField, Min(1)] private int baseUpgradeCost = 3;
    [SerializeField, Min(1)] private int levelsPerUpgradeCostStep = 5;
    [SerializeField, Min(0)] private int upgradeCostIncreasePerStep = 2;
    [SerializeField, Min(1)] private int damageIncreasePerUpgrade = 1;
    [SerializeField] private D6DiceDamage diceDamage;
    [SerializeField] private D6DiceDamageDisplay damageDisplay;
    [SerializeField] private Text coinsText;
    [SerializeField] private Button upgradeButton;

    public int Coins { get; private set; }
    public int CurrentLevel { get; private set; } = 1;
    public int UpgradeLevel { get; private set; } = 1;
    public int CurrentUpgradeCost => baseUpgradeCost + (((CurrentLevel - 1) / levelsPerUpgradeCostStep) * upgradeCostIncreasePerStep);

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

    public void SetCurrentLevel(int level)
    {
        CurrentLevel = Mathf.Max(1, level);
        RefreshUi();
    }

    public void AddCoins(int amount)
    {
        Coins += Mathf.Max(0, amount);
        RefreshUi();
    }

    public void UpgradeDamage()
    {
        if (Coins < CurrentUpgradeCost || diceDamage == null)
        {
            return;
        }

        Coins -= CurrentUpgradeCost;
        UpgradeLevel++;
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

        var uiRoot = MobileLayoutUtility.GetUiRoot();

        if (coinsText == null)
        {
            var textObject = new GameObject("Coins Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(uiRoot, false);

            coinsText = textObject.GetComponent<Text>();
            coinsText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            coinsText.fontSize = 38;
            coinsText.fontStyle = FontStyle.Bold;
            coinsText.alignment = TextAnchor.MiddleLeft;
            coinsText.color = new Color(1f, 0.82f, 0.18f, 1f);
            coinsText.raycastTarget = false;
            MobileLayoutUtility.ConfigureText(coinsText, 38, 22);

            var rect = coinsText.rectTransform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(32f, -32f);
            rect.sizeDelta = new Vector2(460f, 80f);
        }

        if (upgradeButton == null)
        {
            var buttonObject = new GameObject("Upgrade Button", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(uiRoot, false);

            var rect = buttonObject.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(1f, 0.5f);
            rect.anchorMax = new Vector2(1f, 0.5f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchoredPosition = new Vector2(-32f, 0f);
            rect.sizeDelta = new Vector2(320f, 112f);

            var image = buttonObject.GetComponent<Image>();
            image.color = new Color(0.18f, 0.44f, 0.9f, 0.95f);

            upgradeButton = buttonObject.GetComponent<Button>();

            var labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
            labelObject.transform.SetParent(buttonObject.transform, false);

            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 8f);
            labelRect.offsetMax = new Vector2(-10f, -8f);

            var label = labelObject.GetComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            label.fontSize = 26;
            label.fontStyle = FontStyle.Bold;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 18;
            label.resizeTextMaxSize = 26;
            label.text = GetUpgradeButtonText();
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
        if (coinsText != null)
        {
            coinsText.text = $"Coins: {Coins}";
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = Coins >= CurrentUpgradeCost;
            var label = upgradeButton.GetComponentInChildren<Text>();
            if (label != null)
            {
                label.text = GetUpgradeButtonText();
            }
        }
    }

    private string GetUpgradeButtonText()
    {
        return $"UPGRADE LEVEL {UpgradeLevel}\nCOST: {CurrentUpgradeCost}";
    }
}
