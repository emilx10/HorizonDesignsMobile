using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DiceSummonPopupController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private DiceSpellSummonController summonController;
    [SerializeField] private Button singlePullButton;
    [SerializeField] private Button tenPullButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text singlePullText;
    [SerializeField] private TMP_Text tenPullText;
    [SerializeField] private TMP_Text feedbackText;
    [SerializeField] private Image singlePullCurrencyImage;
    [SerializeField] private Image tenPullCurrencyImage;
    [SerializeField] private Sprite currencySprite;
    [SerializeField] private string singlePullFormat = "x1 Pull\n{0}";
    [SerializeField] private string tenPullFormat = "x10 Pull\n{0}";
    [SerializeField] private string notEnoughCurrencyFormat = "Need {0}";

    private void Awake()
    {
        if (popupRoot == null)
        {
            popupRoot = gameObject;
        }

        if (summonController == null)
        {
            summonController = FindFirstObjectByType<DiceSpellSummonController>();
        }

        SetOpen(false);
        Refresh();
    }

    private void OnEnable()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.AddListener(DoSinglePull);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.AddListener(DoTenPull);
        }

        if (closeButton != null)
        {
            closeButton.onClick.AddListener(Close);
        }

        if (summonController != null)
        {
            summonController.NotEnoughCurrency += HandleNotEnoughCurrency;
            summonController.SummonStarted += HandleSummonStarted;
            summonController.SummonCompleted += HandleSummonCompleted;
        }
    }

    private void OnDisable()
    {
        if (singlePullButton != null)
        {
            singlePullButton.onClick.RemoveListener(DoSinglePull);
        }

        if (tenPullButton != null)
        {
            tenPullButton.onClick.RemoveListener(DoTenPull);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
        }

        if (summonController != null)
        {
            summonController.NotEnoughCurrency -= HandleNotEnoughCurrency;
            summonController.SummonStarted -= HandleSummonStarted;
            summonController.SummonCompleted -= HandleSummonCompleted;
        }
    }

    public void Open()
    {
        Refresh();
        SetOpen(true);
    }

    public void Close()
    {
        SetOpen(false);
    }

    private void DoSinglePull()
    {
        if (summonController != null)
        {
            summonController.TrySinglePull();
        }
    }

    private void DoTenPull()
    {
        if (summonController != null)
        {
            summonController.TryTenPull();
        }
    }

    private void Refresh()
    {
        if (summonController != null)
        {
            SetText(singlePullText, string.Format(singlePullFormat, summonController.SinglePullCost));
            SetText(tenPullText, string.Format(tenPullFormat, summonController.TenPullCost));
        }

        if (singlePullCurrencyImage != null)
        {
            singlePullCurrencyImage.sprite = currencySprite;
        }

        if (tenPullCurrencyImage != null)
        {
            tenPullCurrencyImage.sprite = currencySprite;
        }

        SetText(feedbackText, string.Empty);
    }

    private void HandleNotEnoughCurrency(DiceCurrencyKind _, int cost)
    {
        SetText(feedbackText, string.Format(notEnoughCurrencyFormat, cost));
    }

    private void HandleSummonStarted(int _)
    {
        SetButtonsInteractable(false);
        SetText(feedbackText, string.Empty);
    }

    private void HandleSummonCompleted(System.Collections.Generic.IReadOnlyList<DiceSpellInventory.SpellOwnership> _)
    {
        SetButtonsInteractable(true);
        Refresh();
    }

    private void SetOpen(bool open)
    {
        if (popupRoot != null)
        {
            popupRoot.SetActive(open);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (singlePullButton != null)
        {
            singlePullButton.interactable = interactable;
        }

        if (tenPullButton != null)
        {
            tenPullButton.interactable = interactable;
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }
}
