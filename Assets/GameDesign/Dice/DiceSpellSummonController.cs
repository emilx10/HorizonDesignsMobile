using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[DisallowMultipleComponent]
public sealed class DiceSpellSummonController : MonoBehaviour
{
    [Serializable]
    private sealed class SummonPoolEntry
    {
        [SerializeField] private DiceSpellDefinition spell;
        [SerializeField] private DiceSpellRarity rarity = DiceSpellRarity.Common;
        [SerializeField, Min(0f)] private float weight = 1f;

        public DiceSpellDefinition Spell => spell;
        public DiceSpellRarity Rarity => spell != null ? spell.Rarity : rarity;
        public float Weight => Mathf.Max(0f, weight);
    }

    [Header("References")]
    [SerializeField] private DiceCurrencyWallet wallet;
    [SerializeField] private DiceSpellInventory inventory;

    [Header("Costs")]
    [SerializeField] private DiceCurrencyKind summonCurrency = DiceCurrencyKind.PinkCubes;
    [SerializeField, Min(0)] private int singlePullCost = 1;
    [SerializeField, Min(0)] private int tenPullCost = 10;

    [Header("Pool")]
    [SerializeField] private List<SummonPoolEntry> summonPool = new();

    [Header("VFX Hooks")]
    [SerializeField, Min(0f)] private float summonVfxDuration = 1f;
    [SerializeField] private GameObject summonVfxRoot;
    [SerializeField] private ParticleSystem summonParticleSystem;
    [SerializeField] private Animator summonAnimator;
    [SerializeField] private string summonAnimatorTrigger = "Summon";
    [SerializeField] private UnityEvent summonVfxStarted;
    [SerializeField] private UnityEvent summonVfxFinished;

    public event Action<int> SummonStarted;
    public event Action<IReadOnlyList<DiceSpellInventory.SpellOwnership>> SummonCompleted;
    public event Action<DiceCurrencyKind, int> NotEnoughCurrency;

    public DiceCurrencyKind SummonCurrency => summonCurrency;
    public int SinglePullCost => singlePullCost;
    public int TenPullCost => tenPullCost;
    public bool IsSummoning { get; private set; }

    private void Awake()
    {
        if (wallet == null)
        {
            wallet = FindFirstObjectByType<DiceCurrencyWallet>();
        }

        if (inventory == null)
        {
            inventory = FindFirstObjectByType<DiceSpellInventory>();
        }

        if (summonVfxRoot != null)
        {
            summonVfxRoot.SetActive(false);
        }
    }

    public bool TrySinglePull()
    {
        return TrySummon(1);
    }

    public bool TryTenPull()
    {
        return TrySummon(10);
    }

    public bool TrySummon(int pullCount)
    {
        pullCount = Mathf.Max(1, pullCount);
        var cost = pullCount == 10 ? tenPullCost : singlePullCost * pullCount;

        if (IsSummoning || wallet == null || inventory == null || !wallet.TrySpend(summonCurrency, cost))
        {
            NotEnoughCurrency?.Invoke(summonCurrency, cost);
            return false;
        }

        StartCoroutine(SummonRoutine(pullCount));
        return true;
    }

    private IEnumerator SummonRoutine(int pullCount)
    {
        IsSummoning = true;
        SummonStarted?.Invoke(pullCount);
        PlaySummonVfx();

        if (summonVfxDuration > 0f)
        {
            yield return new WaitForSeconds(summonVfxDuration);
        }

        var results = new List<DiceSpellInventory.SpellOwnership>(pullCount);
        for (var i = 0; i < pullCount; i++)
        {
            var spell = RollSpell();
            var ownership = inventory.GrantSpell(spell);
            if (ownership != null)
            {
                results.Add(ownership);
            }
        }

        StopSummonVfx();
        IsSummoning = false;
        SummonCompleted?.Invoke(results);
    }

    private DiceSpellDefinition RollSpell()
    {
        var totalWeight = 0f;
        for (var i = 0; i < summonPool.Count; i++)
        {
            if (summonPool[i] != null && summonPool[i].Spell != null)
            {
                totalWeight += summonPool[i].Weight;
            }
        }

        if (totalWeight <= 0f)
        {
            return null;
        }

        var roll = UnityEngine.Random.Range(0f, totalWeight);
        for (var i = 0; i < summonPool.Count; i++)
        {
            var entry = summonPool[i];
            if (entry == null || entry.Spell == null)
            {
                continue;
            }

            roll -= entry.Weight;
            if (roll <= 0f)
            {
                return entry.Spell;
            }
        }

        return summonPool[^1].Spell;
    }

    private void PlaySummonVfx()
    {
        if (summonVfxRoot != null)
        {
            summonVfxRoot.SetActive(true);
        }

        if (summonParticleSystem != null)
        {
            summonParticleSystem.Play(true);
        }

        if (summonAnimator != null && !string.IsNullOrWhiteSpace(summonAnimatorTrigger))
        {
            summonAnimator.SetTrigger(summonAnimatorTrigger);
        }

        summonVfxStarted?.Invoke();
    }

    private void StopSummonVfx()
    {
        if (summonParticleSystem != null)
        {
            summonParticleSystem.Stop(true);
        }

        if (summonVfxRoot != null)
        {
            summonVfxRoot.SetActive(false);
        }

        summonVfxFinished?.Invoke();
    }
}
