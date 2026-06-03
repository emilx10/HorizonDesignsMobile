using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class MobileGameUiController : MonoBehaviour
{
    private const float ReferenceWidth = 1080f;
    private const float ReferenceHeight = 1920f;

    public static MobileGameUiController Instance { get; private set; }

    public Text LevelText { get; private set; }
    public Text CoinsText { get; private set; }
    public Text PinkCurrencyText { get; private set; }
    public Text BlueCurrencyText { get; private set; }
    public Text HealthText { get; private set; }
    public Text UpgradeButtonText { get; private set; }
    public Button UpgradeDamageButton { get; private set; }
    public Button RetryLevelButton { get; private set; }
    public Button BackToGameplayButton { get; private set; }

    private readonly Dictionary<string, Sprite> spriteCache = new();
    private RectTransform shell;
    private GameObject mainView;
    private GameObject upgradeView;
    private GameObject spellsView;

    private Font font;

    public static MobileGameUiController Ensure()
    {
        if (Instance != null)
        {
            return Instance;
        }

        var canvas = MobileLayoutUtility.EnsureCanvas();
        var existing = canvas.GetComponentInChildren<MobileGameUiController>(true);
        if (existing != null)
        {
            Instance = existing;
            return existing;
        }

        var host = new GameObject("Mobile Game UI", typeof(RectTransform), typeof(MobileGameUiController));
        host.transform.SetParent(MobileLayoutUtility.GetUiRoot(), false);
        var controller = host.GetComponent<MobileGameUiController>();
        controller.Build();
        Instance = controller;
        return controller;
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Build()
    {
        font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        shell = (RectTransform)transform;
        shell.anchorMin = new Vector2(0.5f, 0.5f);
        shell.anchorMax = new Vector2(0.5f, 0.5f);
        shell.pivot = new Vector2(0.5f, 0.5f);
        shell.sizeDelta = new Vector2(ReferenceWidth, ReferenceHeight);
        shell.anchoredPosition = Vector2.zero;

        AddGameplayTint();
        BuildTopBar();
        BuildBottomShell();
        BuildMainView();
        BuildUpgradeView();
        BuildSpellsView();

        ShowMain();
    }

    private void BuildTopBar()
    {
        var top = AddImage(shell, "Top Profile Bar", "MobileUI/UI/Up Profile Bar", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(1120f, 252f), new Vector2(0f, 12f));
        top.raycastTarget = false;

        AddImage(top.rectTransform, "Dice Profile Icon", "MobileUI/UI/Blue Cube Icon", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(96f, 96f), new Vector2(116f, -99f));

        var nameText = AddText(top.rectTransform, "Player Name", "GG", 36, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(170f, 52f), new Vector2(205f, -96f));
        nameText.fontStyle = FontStyle.BoldAndItalic;

        AddCurrency(top.rectTransform, "Coins", "MobileUI/UI/Coin Icon", out var coinsText, new Vector2(555f, -96f));
        CoinsText = coinsText;
        AddCurrency(top.rectTransform, "Pink Currency", "MobileUI/UI/Pink Cube Icon", out var pinkText, new Vector2(710f, -96f));
        PinkCurrencyText = pinkText;
        AddCurrency(top.rectTransform, "Blue Currency", "MobileUI/UI/Blue Cube Icon", out var blueText, new Vector2(865f, -96f));
        BlueCurrencyText = blueText;

        LevelText = AddText(shell, "Level Text", "LEVEL 1", 38, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(340f, 64f), new Vector2(0f, -128f));
        LevelText.fontStyle = FontStyle.Bold;

        HealthText = AddText(shell, "Health Text", "HP 20/20", 36, TextAnchor.MiddleLeft, new Color(0.55f, 1f, 0.45f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(300f, 64f), new Vector2(44f, -162f));
        HealthText.fontStyle = FontStyle.Bold;

        SetCoins(0);
        PinkCurrencyText.text = "0";
        BlueCurrencyText.text = "0";
    }

    private void AddCurrency(RectTransform parent, string name, string iconPath, out Text amountText, Vector2 position)
    {
        AddImage(parent, name + " Icon", iconPath, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(44f, 44f), position);
        amountText = AddText(parent, name + " Amount", "0", 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(82f, 42f), position + new Vector2(32f, 0f));
        amountText.fontStyle = FontStyle.Bold;
    }

    private void AddGameplayTint()
    {
        var playArea = NewRect("Gameplay Screen Space", shell, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, 0f), new Vector2(0f, -200f), new Vector2(0f, -710f));
        var image = playArea.gameObject.AddComponent<Image>();
        image.color = new Color(0.72f, 0.72f, 0.72f, 0.32f);
        image.raycastTarget = false;
    }

    private void BuildBottomShell()
    {
        var basePanel = AddImage(shell, "Bottom UI Base", "MobileUI/UI/Base For All Ui", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(1110f, 720f), new Vector2(0f, -12f));
        basePanel.raycastTarget = false;
    }

    private void BuildMainView()
    {
        mainView = NewView("Main Section");

        AddImage((RectTransform)mainView.transform, "Dice Editor Space", "MobileUI/UI/Main Section/Dice Editor Space", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(500f, 148f), new Vector2(170f, 500f));
        AddArtButton((RectTransform)mainView.transform, "Main Dice Editor Button", "Main Dice Editor", "MobileUI/UI/Main Section/Pink Button", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(330f, 112f), new Vector2(210f, 310f), null);

        BuildNavButtons((RectTransform)mainView.transform);
        RetryLevelButton = AddArtButton((RectTransform)mainView.transform, "Retry Last Level Button", "Retry", "MobileUI/UI/Upgrade Section UI/Red  Upgrade Button", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(240f, 88f), new Vector2(-48f, 565f), null);
        RetryLevelButton.gameObject.SetActive(false);
    }

    private void BuildUpgradeView()
    {
        upgradeView = NewView("Upgrade Section");
        AddBackButton((RectTransform)upgradeView.transform);

        var list = AddImage((RectTransform)upgradeView.transform, "Upgrade List Base", "MobileUI/UI/Upgrade Section UI/Red Base For Upgrades", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(900f, 390f), new Vector2(0f, 455f));
        list.raycastTarget = false;

        UpgradeDamageButton = AddUpgradeRow((RectTransform)upgradeView.transform, "Damage Upgrade", "MobileUI/Upgrades/damage upgrade 2", "Dice Damage", new Vector2(0f, 560f), true);
        AddUpgradeRow((RectTransform)upgradeView.transform, "Spell Damage Upgrade", "MobileUI/Upgrades/spell damage upgrade 4", "Spell Damage", new Vector2(0f, 440f), false);
        AddUpgradeRow((RectTransform)upgradeView.transform, "Spell Duration Upgrade", "MobileUI/Upgrades/spell Durationm upgrade 2", "Spell Duration", new Vector2(0f, 320f), false);

        BuildNavButtons((RectTransform)upgradeView.transform);
    }

    private Button AddUpgradeRow(RectTransform parent, string name, string iconPath, string title, Vector2 position, bool enabled)
    {
        AddImage(parent, name + " Row", "MobileUI/UI/Upgrade Section UI/Red Line For Upgrades", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(830f, 105f), position);
        AddImage(parent, name + " Icon", iconPath, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(82f, 82f), position + new Vector2(-320f, 0f));
        AddText(parent, name + " Label", title, 28, TextAnchor.MiddleLeft, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0.5f), new Vector2(280f, 64f), position + new Vector2(-220f, 0f));
        var button = AddArtButton(parent, name + " Button", enabled ? "Upgrade" : "0", "MobileUI/UI/Upgrade Section UI/Red  Upgrade Button", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(210f, 76f), position + new Vector2(285f, 0f), null);
        button.interactable = enabled;

        if (enabled)
        {
            UpgradeButtonText = button.GetComponentInChildren<Text>();
        }

        return button;
    }

    private void BuildSpellsView()
    {
        spellsView = NewView("Spells Section");
        AddBackButton((RectTransform)spellsView.transform);

        AddImage((RectTransform)spellsView.transform, "Spell Weight Card", "MobileUI/UI/Spells/Spells Profile", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(230f, 250f), new Vector2(52f, 460f));
        AddText((RectTransform)spellsView.transform, "Spell Weight Text", "Spells Weight\n1%\nRole\nTank", 24, TextAnchor.MiddleCenter, Color.white, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0.5f), new Vector2(210f, 170f), new Vector2(66f, 460f));

        AddImage((RectTransform)spellsView.transform, "Owned Spells Panel", "MobileUI/UI/Spells/Base For Own Spells", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0.5f), new Vector2(720f, 250f), new Vector2(-48f, 460f));
        AddText((RectTransform)spellsView.transform, "Owned Spells Label", "Spells List", 24, TextAnchor.MiddleCenter, Color.white, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(260f, 44f), new Vector2(190f, 334f));

        var start = new Vector2(-250f, 515f);
        for (var i = 0; i < 6; i++)
        {
            var x = start.x + ((i % 3) * 180f);
            var y = start.y - ((i / 3) * 105f);
            AddImage((RectTransform)spellsView.transform, "Owned Spell Slot " + i, "MobileUI/UI/Spells/Owned Spells Base", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(135f, 82f), new Vector2(x, y));
        }

        for (var i = 0; i < 8; i++)
        {
            var x = -330f + ((i % 4) * 220f);
            var y = 245f - ((i / 4) * 115f);
            AddImage((RectTransform)spellsView.transform, "Spell Slot " + i, "MobileUI/UI/Spells/Spells Owned Base", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(150f, 90f), new Vector2(x, y));
        }

        AddImage((RectTransform)spellsView.transform, "Fire Spell Icon", "MobileUI/Spells/Fire ball", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(86f, 86f), new Vector2(-330f, 245f));
        AddImage((RectTransform)spellsView.transform, "Ice Spell Icon", "MobileUI/Spells/Ice Icon", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(86f, 86f), new Vector2(-110f, 245f));

        BuildNavButtons((RectTransform)spellsView.transform);
    }

    private GameObject NewView(string name)
    {
        var view = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)view.transform;
        rect.SetParent(shell, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return view;
    }

    private void BuildNavButtons(RectTransform parent)
    {
        AddArtButton(parent, "Upgrade Section Button", "Upgrade\nSection", "MobileUI/UI/Upgrade Section UI/Yellow Button", new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(245f, 118f), new Vector2(42f, 42f), ShowUpgrade);
        AddArtButton(parent, "Summons Button", "Summons", "MobileUI/UI/Upgrade Section UI/Yellow Button", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(245f, 118f), new Vector2(0f, 42f), ShowSpells);
        AddArtButton(parent, "Shop Button", "Shop", "MobileUI/UI/Upgrade Section UI/Yellow Button", new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(245f, 118f), new Vector2(-42f, 42f), ShowSpells);
    }

    private void AddBackButton(RectTransform parent)
    {
        BackToGameplayButton = AddArtButton(parent, "Back To Gameplay Button", "Back To Gameplay", "MobileUI/UI/Back To Gamplay Button", new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(230f, 82f), new Vector2(32f, -230f), ShowMain);
    }

    public void ShowMain()
    {
        SetView(mainView);
    }

    public void ShowUpgrade()
    {
        SetView(upgradeView);
    }

    public void ShowSpells()
    {
        SetView(spellsView);
    }

    public void SetCoins(int coins)
    {
        if (CoinsText != null)
        {
            CoinsText.text = Mathf.Max(0, coins).ToString();
        }
    }

    public void SetUpgradeButtonLabel(string label)
    {
        if (UpgradeButtonText != null)
        {
            UpgradeButtonText.text = label;
        }
    }

    private void SetView(GameObject activeView)
    {
        if (mainView != null)
        {
            mainView.SetActive(mainView == activeView);
        }

        if (upgradeView != null)
        {
            upgradeView.SetActive(upgradeView == activeView);
        }

        if (spellsView != null)
        {
            spellsView.SetActive(spellsView == activeView);
        }
    }

    private Image AddImage(RectTransform parent, string name, string resourcePath, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = NewRect(name, parent, anchorMin, anchorMax, pivot, size, anchoredPosition);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = LoadSprite(resourcePath);
        image.preserveAspect = true;
        return image;
    }

    private Button AddArtButton(RectTransform parent, string name, string label, string resourcePath, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, UnityEngine.Events.UnityAction action)
    {
        var image = AddImage(parent, name, resourcePath, anchorMin, anchorMax, pivot, size, anchoredPosition);
        image.raycastTarget = true;
        var button = image.gameObject.AddComponent<Button>();
        if (action != null)
        {
            button.onClick.AddListener(action);
        }

        var text = AddText(image.rectTransform, "Label", label, 28, TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        text.rectTransform.offsetMin = new Vector2(18f, 12f);
        text.rectTransform.offsetMax = new Vector2(-18f, -12f);
        text.fontStyle = FontStyle.Bold;
        MobileLayoutUtility.ConfigureText(text, 30, 18);
        return button;
    }

    private Text AddText(RectTransform parent, string name, string value, int fontSize, TextAnchor alignment, Color color, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        var rect = NewRect(name, parent, anchorMin, anchorMax, pivot, size, anchoredPosition);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = color;
        text.text = value;
        text.raycastTarget = false;
        MobileLayoutUtility.ConfigureText(text, fontSize, Mathf.Max(12, fontSize - 16));

        var shadow = rect.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.15f, 0f, 0.04f, 0.75f);
        shadow.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return rect;
    }

    private static RectTransform NewRect(string name, RectTransform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 size, Vector2 anchoredPosition, Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = NewRect(name, parent, anchorMin, anchorMax, pivot, size, anchoredPosition);
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private Sprite LoadSprite(string resourcePath)
    {
        if (spriteCache.TryGetValue(resourcePath, out var cached))
        {
            return cached;
        }

        var texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            return null;
        }

        var sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[resourcePath] = sprite;
        return sprite;
    }
}
