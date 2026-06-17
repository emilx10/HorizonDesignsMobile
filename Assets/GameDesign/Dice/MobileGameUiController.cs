using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;

[DisallowMultipleComponent]
public sealed class MobileGameUiController : MonoBehaviour
{
    public static MobileGameUiController Instance { get; private set; }
    public event Action<bool> MainViewActiveChanged;

    [Header("Value Text References")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text pinkCurrencyText;
    [SerializeField] private TMP_Text blueCurrencyText;
    [SerializeField] private TMP_Text healthText;

    [Header("Value Text Formats")]
    [SerializeField] private string levelFormat = "LEVEL {0}";
    [SerializeField] private string coinsFormat = "{0}";
    [SerializeField] private string pinkCurrencyFormat = "{0}";
    [SerializeField] private string blueCurrencyFormat = "{0}";
    [SerializeField] private string healthFormat = "{0}/{1}";

    [Header("Inspector Preview Values")]
    [SerializeField, Min(1)] private int previewLevel = 1;
    [SerializeField, Min(0)] private int previewCoins;
    [SerializeField, Min(0)] private int previewPinkCurrency = 4;
    [SerializeField, Min(0)] private int previewBlueCurrency = 2;
    [SerializeField, Min(1)] private int previewMaxHealth = 20;
    [SerializeField, Min(0)] private float previewCurrentHealth = 20f;

    [Header("Button Text References")]
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private TMP_Text retryLevelButtonText;
    [SerializeField] private TMP_Text upgradeSectionButtonText;
    [SerializeField] private TMP_Text summonsButtonText;
    [SerializeField] private TMP_Text diceEditorButtonText;
    [SerializeField] private TMP_Text settingsButtonText;
    [SerializeField] private TMP_Text infoButtonText;
    [SerializeField] private TMP_Text backToGameplayButtonText;
    [SerializeField] private TMP_Text settingsOkButtonText;
    [SerializeField] private TMP_Text equipSpellButtonText;
    [SerializeField] private TMP_Text closeInfoButtonText;

    [Header("Upgrade Cost Text References")]
    [SerializeField] private TMP_Text damageUpgradeCostText;
    [SerializeField] private TMP_Text spellDamageUpgradeCostText;
    [SerializeField] private TMP_Text cooldownUpgradeCostText;

    [Header("Button Labels")]
    [SerializeField] private string upgradeButtonLabel = "Upgrade";
    [SerializeField] private string upgradeButtonProgressFormat = "{0}\n{1}";
    [SerializeField] private string upgradeLevelText = "Upgrade Level";
    [SerializeField] private string upgradeCostText = "Cost";
    [SerializeField] private string retryLevelLabel = "Retry Level";
    [SerializeField] private string upgradeSectionLabel = "Upgrade Section";
    [SerializeField] private string summonsLabel = "Summons";
    [SerializeField] private string diceEditorLabel = "Dice Editor";
    [SerializeField] private string settingsLabel = "Settings";
    [SerializeField] private string infoLabel = "Info";
    [SerializeField] private string backToGameplayLabel = "Back To Gameplay";
    [SerializeField] private string settingsOkLabel = "OK";
    [SerializeField] private string equippedSpellLabel = "Unequip";
    [SerializeField] private string unequippedSpellLabel = "Equip";
    [SerializeField] private string closeInfoLabel = "X";

    [Header("Upgrade Costs")]
    [SerializeField, Min(0)] private int damageUpgradeBaseCost = 3;
    [SerializeField, Min(0)] private int spellDamageUpgradeBaseCost = 3;
    [SerializeField, Min(0)] private int cooldownUpgradeBaseCost = 3;
    [SerializeField, Min(0)] private int damageUpgradeCostIncreasePerBuy = 2;
    [SerializeField, Min(0)] private int spellDamageUpgradeCostIncreasePerBuy = 2;
    [SerializeField, Min(0)] private int cooldownUpgradeCostIncreasePerBuy = 2;
    [SerializeField, Min(0)] private int damageUpgradeBuys;
    [SerializeField, Min(0)] private int spellDamageUpgradeBuys;
    [SerializeField, Min(0)] private int cooldownUpgradeBuys;
    [SerializeField] private string upgradeCostAmountFormat = "{0}";

    [Header("Images")]
    [SerializeField] private RectTransform healthBarRoot;
    [SerializeField] private Image healthFillImage;
    [SerializeField] private bool stylizeHealthBar = true;
    [SerializeField] private Color healthBarFrameColor = new(0.2f, 0.02f, 0.08f, 1f);
    [SerializeField] private Color healthBarTrackColor = new(0.03f, 0.01f, 0.015f, 0.95f);
    [SerializeField] private Color healthBarFillColor = new(0.84f, 1f, 0.22f, 1f);
    [SerializeField] private Color healthBarFillLowColor = new(1f, 0.25f, 0.18f, 1f);
    [SerializeField] private Color healthBarShineColor = new(1f, 1f, 1f, 0.28f);
    [SerializeField, Min(0f)] private float healthBarPadding = 10f;
    [SerializeField] private Vector2 playerHealthBarPositionOffset;

    [Header("Buttons")]
    [SerializeField] private Button upgradeDamageButton;
    [SerializeField] private Button retryLevelButton;
    [SerializeField] private Button upgradeSectionButton;
    [SerializeField] private Button summonsButton;
    [SerializeField] private Button diceEditorButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button backToGameplayButton;
    [SerializeField] private Button[] backToGameplayButtons;
    [SerializeField] private Button settingsOkButton;
    [SerializeField] private Button equipSpellButton;
    [SerializeField] private Button closeInfoButton;

    [Header("Sections")]
    [SerializeField] private GameObject gameplayChrome;
    [SerializeField] private GameObject mainView;
    [SerializeField] private GameObject upgradeView;
    [SerializeField] private GameObject spellsView;
    [SerializeField] private GameObject settingsView;
    [SerializeField] private GameObject infoView;
    [SerializeField] private DiceSummonPopupController summonPopup;

    [Header("Behaviour")]
    [SerializeField] private bool showMainOnStart = true;
    [SerializeField] private bool hideGameplayChromeOnSettingsAndInfo = true;
    [SerializeField] private bool applyInspectorTextOnStart = true;

    [Header("Responsive Layout")]
    [SerializeField] private bool configureCanvasScalerForPortrait = true;
    [SerializeField] private bool stretchFullScreenViewsToCanvas;
    [SerializeField] private bool anchorUpgradeViewToBottom;
    [SerializeField] private bool adaptCenterAnchoredLayout;
    [SerializeField] private Vector2 referenceResolution = new(1080f, 1920f);
    [SerializeField, Range(0f, 1f)] private float matchWidthOrHeight = 0f;
    [SerializeField, Min(0f)] private float topBottomZoneThreshold = 300f;
    [SerializeField, Min(0f)] private float upgradeViewHorizontalMargin = 44f;
    [SerializeField, Min(0f)] private float upgradeViewBottomMargin = 0f;

    public TMP_Text LevelText => levelText;
    public TMP_Text CoinsText => coinsText;
    public TMP_Text PinkCurrencyText => pinkCurrencyText;
    public TMP_Text BlueCurrencyText => blueCurrencyText;
    public TMP_Text HealthText => healthText;
    public TMP_Text UpgradeButtonText => upgradeButtonText;
    public Image HealthFillImage => healthFillImage;
    public RectTransform HealthBarRoot => healthBarRoot;
    public Vector2 PlayerHealthBarPositionOffset => playerHealthBarPositionOffset;
    public Button UpgradeDamageButton => upgradeDamageButton;
    public Button RetryLevelButton => retryLevelButton;
    public Button BackToGameplayButton => backToGameplayButton;
    public Button[] BackToGameplayButtons => backToGameplayButtons;
    public Button SettingsOkButton => settingsOkButton;
    public Button EquipSpellButton => equipSpellButton;
    public Button CloseInfoButton => closeInfoButton;
    public bool IsMainViewActive => mainView == null || mainView.activeInHierarchy;
    public int DamageUpgradeCost => CalculateCost(damageUpgradeBaseCost, damageUpgradeCostIncreasePerBuy, damageUpgradeBuys);
    public int SpellDamageUpgradeCost => CalculateCost(spellDamageUpgradeBaseCost, spellDamageUpgradeCostIncreasePerBuy, spellDamageUpgradeBuys);
    public int CooldownUpgradeCost => CalculateCost(cooldownUpgradeBaseCost, cooldownUpgradeCostIncreasePerBuy, cooldownUpgradeBuys);

    private bool listenersBound;
    private bool spellEquipped;
    private bool lastMainViewActive;
    private Canvas cachedCanvas;
    private RectTransform cachedCanvasRect;
    private Vector2 lastCanvasSize;
    private Rect lastSafeArea;
    private bool responsiveRectsCached;
    private Image styledHealthFillImage;
    private Image styledHealthShineImage;
    private readonly List<ResponsiveRect> responsiveRects = new();

    private const string StyledHealthTrackName = "Stylized Health Track";
    private const string StyledHealthFillName = "Stylized Health Fill";
    private const string StyledHealthShineName = "Stylized Health Shine";

    private struct ResponsiveRect
    {
        public RectTransform Rect;
        public Vector2 DesignAnchoredPosition;
        public ResponsiveZone Zone;
    }

    private enum ResponsiveZone
    {
        Middle,
        Top,
        Bottom
    }

    public static MobileGameUiController FindExisting()
    {
        if (Instance != null)
        {
            return Instance;
        }

        Instance = FindFirstObjectByType<MobileGameUiController>();
        return Instance;
    }

    private void Awake()
    {
        Instance = this;
        lastMainViewActive = IsMainViewActive;
        CacheMissingButtonTextReferences();
        ConfigureHealthBarStyle();
        ConfigureCanvas();
        ConfigureViewRoots();
        CacheResponsiveRects();
        ApplyResponsiveLayout();
    }

    private void OnEnable()
    {
        AddButtonListeners();
        ApplyResponsiveLayout();
    }

    private void Start()
    {
        CacheMissingButtonTextReferences();
        ConfigureHealthBarStyle();
        ConfigureCanvas();
        ConfigureViewRoots();
        CacheResponsiveRects();
        ApplyResponsiveLayout();

        if (applyInspectorTextOnStart)
        {
            ApplyInspectorText();
        }

        if (showMainOnStart)
        {
            ShowMain();
        }
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
    }

    private void LateUpdate()
    {
        ApplyResponsiveLayout();
    }

    [ContextMenu("Apply Inspector Text")]
    public void ApplyInspectorText()
    {
        SetLevel(previewLevel);
        SetCoins(previewCoins);
        SetPinkCurrency(previewPinkCurrency);
        SetBlueCurrency(previewBlueCurrency);
        SetHealth(previewCurrentHealth, previewMaxHealth);
        SetText(upgradeButtonText, upgradeButtonLabel);
        SetText(retryLevelButtonText, retryLevelLabel);
        SetSpellEquipped(spellEquipped);

        SetText(upgradeSectionButtonText, upgradeSectionLabel);
        SetText(summonsButtonText, summonsLabel);
        SetText(diceEditorButtonText, diceEditorLabel);
        SetText(settingsButtonText, settingsLabel);
        SetText(infoButtonText, infoLabel);
        SetText(backToGameplayButtonText, backToGameplayLabel);
        SetText(settingsOkButtonText, settingsOkLabel);
        SetText(closeInfoButtonText, closeInfoLabel);
        RefreshUpgradeCostTexts();
    }

    public void ShowMain()
    {
        SetView(mainView, true);
    }

    public void ShowUpgrade()
    {
        SetView(upgradeView, true);
    }

    public void ShowSpells()
    {
        SetView(spellsView, true);
    }

    public void ShowSettings()
    {
        SetView(settingsView, !hideGameplayChromeOnSettingsAndInfo);
    }

    public void ShowInfo()
    {
        SetView(infoView, !hideGameplayChromeOnSettingsAndInfo);
    }

    public void SetCoins(int coins)
    {
        previewCoins = Mathf.Max(0, coins);
        SetText(coinsText, Format(coinsFormat, previewCoins));
    }

    public void SetPinkCurrency(int amount)
    {
        previewPinkCurrency = Mathf.Max(0, amount);
        SetText(pinkCurrencyText, Format(pinkCurrencyFormat, previewPinkCurrency));
    }

    public void SetBlueCurrency(int amount)
    {
        previewBlueCurrency = Mathf.Max(0, amount);
        SetText(blueCurrencyText, Format(blueCurrencyFormat, previewBlueCurrency));
    }

    public void SetLevel(int level)
    {
        previewLevel = Mathf.Max(1, level);
        SetText(levelText, Format(levelFormat, previewLevel));
    }

    public void SetHealth(float current, int max)
    {
        previewMaxHealth = Mathf.Max(1, max);
        previewCurrentHealth = Mathf.Clamp(current, 0f, previewMaxHealth);
        SetText(healthText, Format(healthFormat, FormatNumber(previewCurrentHealth), previewMaxHealth));

        if (healthFillImage != null)
        {
            healthFillImage.fillAmount = previewCurrentHealth / previewMaxHealth;
            healthFillImage.color = Color.Lerp(healthBarFillLowColor, healthBarFillColor, healthFillImage.fillAmount);
        }

        if (styledHealthShineImage != null)
        {
            styledHealthShineImage.fillAmount = healthFillImage != null ? healthFillImage.fillAmount : 0f;
        }
    }

    public void SetUpgradeButtonLabel(string label)
    {
        SetText(upgradeButtonText, label);
    }

    public void SetUpgradeButtonProgress(int upgradeLevel, int cost)
    {
        var levelLine = Format("{0} {1}", upgradeLevelText, Mathf.Max(1, upgradeLevel));
        var costLine = Format("{0}: {1}", upgradeCostText, Mathf.Max(0, cost));
        SetText(upgradeButtonText, Format(upgradeButtonProgressFormat, levelLine, costLine, Mathf.Max(1, upgradeLevel), Mathf.Max(0, cost)));
    }

    public void RecordDamageUpgradeBought()
    {
        damageUpgradeBuys++;
        RefreshUpgradeCostTexts();
    }

    public void RecordSpellDamageUpgradeBought()
    {
        spellDamageUpgradeBuys++;
        RefreshUpgradeCostTexts();
    }

    public void RecordCooldownUpgradeBought()
    {
        cooldownUpgradeBuys++;
        RefreshUpgradeCostTexts();
    }

    public void RefreshUpgradeCostTexts()
    {
        SetText(damageUpgradeCostText, Format(upgradeCostAmountFormat, DamageUpgradeCost));
        SetText(spellDamageUpgradeCostText, Format(upgradeCostAmountFormat, SpellDamageUpgradeCost));
        SetText(cooldownUpgradeCostText, Format(upgradeCostAmountFormat, CooldownUpgradeCost));
    }

    public void SetUpgradeButtonInteractable(bool interactable)
    {
        if (upgradeDamageButton != null)
        {
            upgradeDamageButton.interactable = interactable;
        }
    }

    public void SetRetryButtonVisible(bool visible)
    {
        if (retryLevelButton != null)
        {
            retryLevelButton.gameObject.SetActive(visible);
        }
    }

    public void ToggleSpellEquipped()
    {
        SetSpellEquipped(!spellEquipped);
    }

    public void SetSpellEquipped(bool equipped)
    {
        spellEquipped = equipped;
        SetText(equipSpellButtonText, spellEquipped ? equippedSpellLabel : unequippedSpellLabel);
    }

    private void SetView(GameObject activeView, bool showGameplayChrome)
    {
        var wasMainViewActive = IsMainViewActive;

        SetActiveIfAssigned(gameplayChrome, showGameplayChrome);
        SetActiveIfAssigned(mainView, mainView == activeView);
        SetActiveIfAssigned(upgradeView, upgradeView == activeView);
        SetActiveIfAssigned(spellsView, spellsView == activeView);
        SetActiveIfAssigned(settingsView, settingsView == activeView);
        SetActiveIfAssigned(infoView, infoView == activeView);

        var isMainViewActive = IsMainViewActive;
        if (wasMainViewActive != isMainViewActive || lastMainViewActive != isMainViewActive)
        {
            lastMainViewActive = isMainViewActive;
            MainViewActiveChanged?.Invoke(isMainViewActive);
        }
    }

    private void AddButtonListeners()
    {
        if (listenersBound)
        {
            return;
        }

        listenersBound = true;
        AddListener(upgradeSectionButton, ShowUpgrade);
        AddListener(summonsButton, OpenSummons);
        AddListener(diceEditorButton, ShowSpells);
        AddListener(settingsButton, ShowSettings);
        AddListener(infoButton, ShowInfo);
        AddListener(backToGameplayButton, ShowMain);
        AddListeners(backToGameplayButtons, ShowMain);
        AddListener(settingsOkButton, ShowMain);
        AddListener(closeInfoButton, ShowMain);
        AddListener(equipSpellButton, ToggleSpellEquipped);
    }

    private void RemoveButtonListeners()
    {
        if (!listenersBound)
        {
            return;
        }

        listenersBound = false;
        RemoveListener(upgradeSectionButton, ShowUpgrade);
        RemoveListener(summonsButton, OpenSummons);
        RemoveListener(diceEditorButton, ShowSpells);
        RemoveListener(settingsButton, ShowSettings);
        RemoveListener(infoButton, ShowInfo);
        RemoveListener(backToGameplayButton, ShowMain);
        RemoveListeners(backToGameplayButtons, ShowMain);
        RemoveListener(settingsOkButton, ShowMain);
        RemoveListener(closeInfoButton, ShowMain);
        RemoveListener(equipSpellButton, ToggleSpellEquipped);
    }

    private void CacheMissingButtonTextReferences()
    {
        upgradeButtonText = GetTextIfMissing(upgradeButtonText, upgradeDamageButton);
        equipSpellButtonText = GetTextIfMissing(equipSpellButtonText, equipSpellButton);
        upgradeSectionButtonText = GetTextIfMissing(upgradeSectionButtonText, upgradeSectionButton);
        summonsButtonText = GetTextIfMissing(summonsButtonText, summonsButton);
        diceEditorButtonText = GetTextIfMissing(diceEditorButtonText, diceEditorButton);
        settingsButtonText = GetTextIfMissing(settingsButtonText, settingsButton);
        infoButtonText = GetTextIfMissing(infoButtonText, infoButton);
        backToGameplayButtonText = GetTextIfMissing(backToGameplayButtonText, backToGameplayButton);
        settingsOkButtonText = GetTextIfMissing(settingsOkButtonText, settingsOkButton);
        closeInfoButtonText = GetTextIfMissing(closeInfoButtonText, closeInfoButton);
        retryLevelButtonText = GetTextIfMissing(retryLevelButtonText, retryLevelButton);
    }

    private void OpenSummons()
    {
        if (summonPopup != null)
        {
            summonPopup.Open();
            return;
        }

        ShowSpells();
    }

    private void ConfigureHealthBarStyle()
    {
        if (!stylizeHealthBar || healthFillImage == null)
        {
            return;
        }

        if (styledHealthFillImage != null && healthFillImage == styledHealthFillImage)
        {
            return;
        }

        var rootImage = healthFillImage;
        var rootRect = rootImage.rectTransform;
        healthBarRoot = rootRect;
        var currentFill = Mathf.Clamp01(rootImage.fillAmount);

        rootImage.type = Image.Type.Sliced;
        rootImage.color = healthBarFrameColor;
        rootImage.raycastTarget = false;

        var outline = rootImage.GetComponent<Outline>();
        if (outline == null)
        {
            outline = rootImage.gameObject.AddComponent<Outline>();
        }

        outline.effectColor = new Color(0.03f, 0.005f, 0.015f, 1f);
        outline.effectDistance = new Vector2(5f, -5f);
        outline.useGraphicAlpha = true;

        var shadow = rootImage.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = rootImage.gameObject.AddComponent<Shadow>();
        }

        shadow.effectColor = new Color(0f, 0f, 0f, 0.35f);
        shadow.effectDistance = new Vector2(0f, -5f);
        shadow.useGraphicAlpha = true;

        var track = GetOrCreateHealthBarImage(rootRect, StyledHealthTrackName);
        var trackRect = track.rectTransform;
        trackRect.anchorMin = Vector2.zero;
        trackRect.anchorMax = Vector2.one;
        trackRect.offsetMin = new Vector2(healthBarPadding, healthBarPadding);
        trackRect.offsetMax = new Vector2(-healthBarPadding, -healthBarPadding);
        track.color = healthBarTrackColor;
        track.type = Image.Type.Sliced;
        track.raycastTarget = false;

        styledHealthFillImage = GetOrCreateHealthBarImage(trackRect, StyledHealthFillName);
        var fillRect = styledHealthFillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 4f);
        fillRect.offsetMax = new Vector2(-4f, -4f);
        styledHealthFillImage.type = Image.Type.Filled;
        styledHealthFillImage.fillMethod = Image.FillMethod.Horizontal;
        styledHealthFillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        styledHealthFillImage.fillAmount = currentFill;
        styledHealthFillImage.color = Color.Lerp(healthBarFillLowColor, healthBarFillColor, currentFill);
        styledHealthFillImage.raycastTarget = false;

        styledHealthShineImage = GetOrCreateHealthBarImage(fillRect, StyledHealthShineName);
        var shineRect = styledHealthShineImage.rectTransform;
        shineRect.anchorMin = new Vector2(0f, 0.58f);
        shineRect.anchorMax = Vector2.one;
        shineRect.offsetMin = Vector2.zero;
        shineRect.offsetMax = Vector2.zero;
        styledHealthShineImage.type = Image.Type.Filled;
        styledHealthShineImage.fillMethod = Image.FillMethod.Horizontal;
        styledHealthShineImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        styledHealthShineImage.fillAmount = currentFill;
        styledHealthShineImage.color = healthBarShineColor;
        styledHealthShineImage.raycastTarget = false;

        healthFillImage = styledHealthFillImage;

        if (healthText != null)
        {
            healthText.transform.SetAsLastSibling();
        }
    }

    private static Image GetOrCreateHealthBarImage(RectTransform parent, string objectName)
    {
        var existing = parent.Find(objectName);
        if (existing != null && existing.TryGetComponent(out Image existingImage))
        {
            return existingImage;
        }

        var imageObject = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        return imageObject.GetComponent<Image>();
    }

    private void ConfigureCanvas()
    {
        cachedCanvas = FindManagedCanvas();
        if (cachedCanvas == null)
        {
            cachedCanvasRect = null;
            responsiveRectsCached = false;
            return;
        }

        cachedCanvasRect = cachedCanvas.transform as RectTransform;

        if (!configureCanvasScalerForPortrait)
        {
            return;
        }

        var scaler = cachedCanvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            return;
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = referenceResolution;
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = GetResponsiveMatchWidthOrHeight();
    }

    private void ConfigureViewRoots()
    {
        if (cachedCanvasRect == null)
        {
            return;
        }

        if (stretchFullScreenViewsToCanvas)
        {
            StretchToParent(mainView);
            StretchToParent(spellsView);
            StretchToParent(settingsView);
            StretchToParent(infoView);
        }

        if (anchorUpgradeViewToBottom)
        {
            AnchorBottomPanel(upgradeView);
        }
    }

    private Canvas FindManagedCanvas()
    {
        if (levelText != null)
        {
            return levelText.GetComponentInParent<Canvas>();
        }

        if (mainView != null)
        {
            return mainView.GetComponentInParent<Canvas>();
        }

        if (upgradeView != null)
        {
            return upgradeView.GetComponentInParent<Canvas>();
        }

        return FindFirstObjectByType<Canvas>();
    }

    private void CacheResponsiveRects()
    {
        if (responsiveRectsCached)
        {
            return;
        }

        responsiveRects.Clear();
        responsiveRectsCached = true;

        if (!adaptCenterAnchoredLayout || cachedCanvasRect == null)
        {
            return;
        }

        AddResponsiveChildren(mainView != null ? mainView.transform as RectTransform : null);
        AddResponsiveChildren(spellsView != null ? spellsView.transform as RectTransform : null);
        AddResponsiveChildren(settingsView != null ? settingsView.transform as RectTransform : null);
        AddResponsiveChildren(infoView != null ? infoView.transform as RectTransform : null);
    }

    private void AddResponsiveChildren(RectTransform root)
    {
        if (root == null)
        {
            return;
        }

        for (var i = 0; i < root.childCount; i++)
        {
            var child = root.GetChild(i) as RectTransform;
            if (child == null || !IsCenteredAnchor(child))
            {
                continue;
            }

            var designPosition = child.anchoredPosition;
            var zone = GetResponsiveZone(designPosition.y);
            if (zone == ResponsiveZone.Middle)
            {
                continue;
            }

            responsiveRects.Add(new ResponsiveRect
            {
                Rect = child,
                DesignAnchoredPosition = designPosition,
                Zone = zone
            });
        }
    }

    private void ApplyResponsiveLayout()
    {
        if (!adaptCenterAnchoredLayout || cachedCanvasRect == null || responsiveRects.Count == 0)
        {
            return;
        }

        var currentSize = cachedCanvasRect.rect.size;
        var currentSafeArea = Screen.safeArea;
        if (currentSize == lastCanvasSize && currentSafeArea == lastSafeArea)
        {
            return;
        }

        lastCanvasSize = currentSize;
        lastSafeArea = currentSafeArea;

        if (configureCanvasScalerForPortrait && cachedCanvas != null)
        {
            var scaler = cachedCanvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.matchWidthOrHeight = GetResponsiveMatchWidthOrHeight();
            }
        }

        if (anchorUpgradeViewToBottom)
        {
            UpdateBottomPanelLayout(upgradeView, currentSize, currentSafeArea);
        }

        var halfHeightDelta = (currentSize.y - referenceResolution.y) * 0.5f;
        var canvasUnitsPerPixel = Screen.width > 0 ? currentSize.x / Screen.width : 1f;
        var topInset = Screen.height > 0 ? Mathf.Max(0f, Screen.height - currentSafeArea.yMax) * canvasUnitsPerPixel : 0f;
        var bottomInset = Screen.height > 0 ? Mathf.Max(0f, currentSafeArea.yMin) * canvasUnitsPerPixel : 0f;

        foreach (var item in responsiveRects)
        {
            if (item.Rect == null)
            {
                continue;
            }

            var position = item.DesignAnchoredPosition;
            if (item.Zone == ResponsiveZone.Top)
            {
                position.y += halfHeightDelta - topInset;
            }
            else if (item.Zone == ResponsiveZone.Bottom)
            {
                position.y -= halfHeightDelta - bottomInset;
            }

            item.Rect.anchoredPosition = position;
        }
    }

    private float GetResponsiveMatchWidthOrHeight()
    {
        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return matchWidthOrHeight;
        }

        var portraitAspect = (float)Screen.height / Screen.width;
        if (portraitAspect <= 1.75f)
        {
            return 0.72f;
        }

        if (portraitAspect <= 1.95f)
        {
            return 0.5f;
        }

        if (portraitAspect <= 2.1f)
        {
            return 0.32f;
        }

        return 0.18f;
    }

    private void UpdateBottomPanelLayout(GameObject target, Vector2 currentSize, Rect currentSafeArea)
    {
        if (target == null || target.transform is not RectTransform rect)
        {
            return;
        }

        var canvasUnitsPerPixel = Screen.width > 0 ? currentSize.x / Screen.width : 1f;
        var bottomInset = Screen.height > 0 ? Mathf.Max(0f, currentSafeArea.yMin) * canvasUnitsPerPixel : 0f;
        var sideInsetLeft = Screen.width > 0 ? Mathf.Max(0f, currentSafeArea.xMin) * canvasUnitsPerPixel : 0f;
        var sideInsetRight = Screen.width > 0 ? Mathf.Max(0f, Screen.width - currentSafeArea.xMax) * canvasUnitsPerPixel : 0f;
        var height = rect.rect.height > 0f ? rect.rect.height : Mathf.Abs(rect.sizeDelta.y);
        if (height <= 0f)
        {
            height = referenceResolution.y * 0.33f;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(upgradeViewHorizontalMargin + sideInsetLeft, upgradeViewBottomMargin + bottomInset);
        rect.offsetMax = new Vector2(-(upgradeViewHorizontalMargin + sideInsetRight), upgradeViewBottomMargin + bottomInset + height);
    }

    private static void StretchToParent(GameObject target)
    {
        if (target == null || target.transform is not RectTransform rect)
        {
            return;
        }

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private void AnchorBottomPanel(GameObject target)
    {
        if (target == null || target.transform is not RectTransform rect)
        {
            return;
        }

        var height = rect.rect.height > 0f ? rect.rect.height : Mathf.Abs(rect.sizeDelta.y);
        if (height <= 0f)
        {
            height = referenceResolution.y * 0.33f;
        }

        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.offsetMin = new Vector2(upgradeViewHorizontalMargin, upgradeViewBottomMargin);
        rect.offsetMax = new Vector2(-upgradeViewHorizontalMargin, upgradeViewBottomMargin + height);
    }

    private ResponsiveZone GetResponsiveZone(float anchoredY)
    {
        if (anchoredY >= topBottomZoneThreshold)
        {
            return ResponsiveZone.Top;
        }

        if (anchoredY <= -topBottomZoneThreshold)
        {
            return ResponsiveZone.Bottom;
        }

        return ResponsiveZone.Middle;
    }

    private static bool IsCenteredAnchor(RectTransform rect)
    {
        return Mathf.Approximately(rect.anchorMin.x, 0.5f)
            && Mathf.Approximately(rect.anchorMin.y, 0.5f)
            && Mathf.Approximately(rect.anchorMax.x, 0.5f)
            && Mathf.Approximately(rect.anchorMax.y, 0.5f);
    }

    private static int CalculateCost(int baseCost, int increasePerBuy, int buys)
    {
        return Mathf.Max(0, baseCost) + (Mathf.Max(0, increasePerBuy) * Mathf.Max(0, buys));
    }

    private static TMP_Text GetTextIfMissing(TMP_Text current, Button button)
    {
        return current != null || button == null ? current : button.GetComponentInChildren<TMP_Text>(true);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void AddListeners(Button[] buttons, UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            AddListener(buttons[i], action);
        }
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private static void RemoveListeners(Button[] buttons, UnityEngine.Events.UnityAction action)
    {
        if (buttons == null)
        {
            return;
        }

        for (var i = 0; i < buttons.Length; i++)
        {
            RemoveListener(buttons[i], action);
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static string Format(string format, params object[] values)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            return values.Length > 0 ? values[0].ToString() : string.Empty;
        }

        try
        {
            return string.Format(format, values);
        }
        catch (System.FormatException)
        {
            return format;
        }
    }

    private static string FormatNumber(float value)
    {
        return Mathf.Approximately(value % 1f, 0f) ? Mathf.RoundToInt(value).ToString() : value.ToString("0.0");
    }
}
