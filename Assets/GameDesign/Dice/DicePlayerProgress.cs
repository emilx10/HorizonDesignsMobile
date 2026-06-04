using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
    [SerializeField] private TMP_Text coinsText;
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

        BindUiIfAvailable();
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

    private void BindUiIfAvailable()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi == null)
        {
            return;
        }

        if (coinsText == null)
        {
            coinsText = mobileUi.CoinsText;
        }

        if (upgradeButton == null)
        {
            upgradeButton = mobileUi.UpgradeDamageButton;
        }
    }

    private void RefreshUi()
    {
        if (coinsText != null)
        {
            coinsText.text = Coins.ToString();
        }

        if (upgradeButton != null)
        {
            upgradeButton.interactable = Coins >= CurrentUpgradeCost;
            var label = upgradeButton.GetComponentInChildren<TMP_Text>();
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
