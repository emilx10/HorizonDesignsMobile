using UnityEngine;
using UnityEngine.UI;
using TMPro;

[DisallowMultipleComponent]
public sealed class MobileGameUiController : MonoBehaviour
{
    public static MobileGameUiController Instance { get; private set; }

    [Header("Text")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text coinsText;
    [SerializeField] private TMP_Text pinkCurrencyText;
    [SerializeField] private TMP_Text blueCurrencyText;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text upgradeButtonText;
    [SerializeField] private TMP_Text equipSpellButtonText;

    [Header("Buttons")]
    [SerializeField] private Button upgradeDamageButton;
    [SerializeField] private Button retryLevelButton;
    [SerializeField] private Button upgradeSectionButton;
    [SerializeField] private Button summonsButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button infoButton;
    [SerializeField] private Button backToGameplayButton;
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

    public TMP_Text LevelText => levelText;
    public TMP_Text CoinsText => coinsText;
    public TMP_Text PinkCurrencyText => pinkCurrencyText;
    public TMP_Text BlueCurrencyText => blueCurrencyText;
    public TMP_Text HealthText => healthText;
    public TMP_Text UpgradeButtonText => upgradeButtonText;
    public Button UpgradeDamageButton => upgradeDamageButton;
    public Button RetryLevelButton => retryLevelButton;
    public Button BackToGameplayButton => backToGameplayButton;
    public Button SettingsOkButton => settingsOkButton;
    public Button EquipSpellButton => equipSpellButton;
    public Button CloseInfoButton => closeInfoButton;

    private bool spellEquipped;

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
    }

    private void OnEnable()
    {
        AddButtonListeners();
    }

    private void Start()
    {
        ShowMain();
    }

    private void OnDisable()
    {
        RemoveButtonListeners();
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
        SetView(settingsView, false);
    }

    public void ShowInfo()
    {
        SetView(infoView, false);
    }

    public void SetCoins(int coins)
    {
        if (coinsText != null)
        {
            coinsText.text = Mathf.Max(0, coins).ToString();
        }
    }

    public void SetPinkCurrency(int amount)
    {
        if (pinkCurrencyText != null)
        {
            pinkCurrencyText.text = Mathf.Max(0, amount).ToString();
        }
    }

    public void SetBlueCurrency(int amount)
    {
        if (blueCurrencyText != null)
        {
            blueCurrencyText.text = Mathf.Max(0, amount).ToString();
        }
    }

    public void SetLevel(int level)
    {
        if (levelText != null)
        {
            levelText.text = $"LEVEL {Mathf.Max(1, level)}";
        }
    }

    public void SetHealth(int current, int max)
    {
        if (healthText != null)
        {
            healthText.color = Color.white;
            healthText.text = $"{Mathf.Max(0, current)}/{Mathf.Max(1, max)}";
        }
    }

    public void SetUpgradeButtonLabel(string label)
    {
        if (upgradeButtonText != null)
        {
            upgradeButtonText.text = label;
            return;
        }

        var labelText = upgradeDamageButton != null ? upgradeDamageButton.GetComponentInChildren<TMP_Text>() : null;
        if (labelText != null)
        {
            labelText.text = label;
        }
    }

    public void ToggleSpellEquipped()
    {
        spellEquipped = !spellEquipped;
        var label = equipSpellButtonText != null ? equipSpellButtonText : equipSpellButton != null ? equipSpellButton.GetComponentInChildren<TMP_Text>() : null;
        if (label != null)
        {
            label.text = spellEquipped ? "Unequip" : "Equip";
        }
    }

    private void SetView(GameObject activeView, bool showGameplayChrome)
    {
        SetActiveIfAssigned(gameplayChrome, showGameplayChrome);
        SetActiveIfAssigned(mainView, mainView == activeView);
        SetActiveIfAssigned(upgradeView, upgradeView == activeView);
        SetActiveIfAssigned(spellsView, spellsView == activeView);
        SetActiveIfAssigned(settingsView, settingsView == activeView);
        SetActiveIfAssigned(infoView, infoView == activeView);
    }

    private void AddButtonListeners()
    {
        AddListener(upgradeSectionButton, ShowUpgrade);
        AddListener(summonsButton, ShowSpells);
        AddListener(settingsButton, ShowSettings);
        AddListener(infoButton, ShowInfo);
        AddListener(backToGameplayButton, ShowMain);
        AddListener(settingsOkButton, ShowMain);
        AddListener(closeInfoButton, ShowMain);
        AddListener(equipSpellButton, ToggleSpellEquipped);
    }

    private void RemoveButtonListeners()
    {
        RemoveListener(upgradeSectionButton, ShowUpgrade);
        RemoveListener(summonsButton, ShowSpells);
        RemoveListener(settingsButton, ShowSettings);
        RemoveListener(infoButton, ShowInfo);
        RemoveListener(backToGameplayButton, ShowMain);
        RemoveListener(settingsOkButton, ShowMain);
        RemoveListener(closeInfoButton, ShowMain);
        RemoveListener(equipSpellButton, ToggleSpellEquipped);
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.RemoveListener(action);
        }
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }
}
