using System;
using UnityEngine;

public enum DiceCurrencyKind
{
    Coins,
    PinkCubes,
    BlueCubes
}

[DisallowMultipleComponent]
public sealed class DiceCurrencyWallet : MonoBehaviour
{
    [SerializeField, Min(0)] private int startingCoins;
    [SerializeField, Min(0)] private int startingPinkCubes;
    [SerializeField, Min(0)] private int startingBlueCubes;

    public event Action<DiceCurrencyKind, int> CurrencyChanged;

    public int Coins { get; private set; }
    public int PinkCubes { get; private set; }
    public int BlueCubes { get; private set; }

    private void Awake()
    {
        Coins = startingCoins;
        PinkCubes = startingPinkCubes;
        BlueCubes = startingBlueCubes;
        RefreshUi();
    }

    public int GetAmount(DiceCurrencyKind kind)
    {
        return kind switch
        {
            DiceCurrencyKind.PinkCubes => PinkCubes,
            DiceCurrencyKind.BlueCubes => BlueCubes,
            _ => Coins,
        };
    }

    public bool HasEnough(DiceCurrencyKind kind, int amount)
    {
        return GetAmount(kind) >= Mathf.Max(0, amount);
    }

    public void Add(DiceCurrencyKind kind, int amount)
    {
        amount = Mathf.Max(0, amount);
        SetAmount(kind, GetAmount(kind) + amount);
    }

    public bool TrySpend(DiceCurrencyKind kind, int amount)
    {
        amount = Mathf.Max(0, amount);
        if (!HasEnough(kind, amount))
        {
            return false;
        }

        SetAmount(kind, GetAmount(kind) - amount);
        return true;
    }

    public void SetAmount(DiceCurrencyKind kind, int amount)
    {
        amount = Mathf.Max(0, amount);

        switch (kind)
        {
            case DiceCurrencyKind.PinkCubes:
                PinkCubes = amount;
                break;
            case DiceCurrencyKind.BlueCubes:
                BlueCubes = amount;
                break;
            default:
                Coins = amount;
                break;
        }

        RefreshUi();
        CurrencyChanged?.Invoke(kind, amount);
    }

    private void RefreshUi()
    {
        var mobileUi = MobileGameUiController.FindExisting();
        if (mobileUi == null)
        {
            return;
        }

        mobileUi.SetCoins(Coins);
        mobileUi.SetPinkCurrency(PinkCubes);
        mobileUi.SetBlueCurrency(BlueCubes);
    }
}
