using UnityEngine;

public enum DiceSpellRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum DiceSpellEffectType
{
    BonusDamage,
    FireBlast,
    FrostImpact
}

[CreateAssetMenu(menuName = "Horizon Designs/Dice Spell", fileName = "New Dice Spell")]
public class DiceSpellDefinition : ScriptableObject
{
    [SerializeField] private string spellId = "spell";
    [SerializeField] private string displayName = "Spell";
    [SerializeField] private DiceSpellRarity rarity = DiceSpellRarity.Common;
    [SerializeField, Min(0)] private int spellDefaultDamage = 1;
    [SerializeField, Min(0)] private int weight = 1;
    [SerializeField] private string role = "Damage";
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;
    [SerializeField] private Sprite acquiredImage;
    [SerializeField, Min(1)] private int maxLevel = 10;
    [SerializeField, Min(1)] private int duplicateUpgradeRequirement = 2;
    [Header("Battle Effect")]
    [SerializeField] private DiceSpellEffectType effectType = DiceSpellEffectType.BonusDamage;
    [SerializeField, Min(0f)] private float immediateDamageMultiplier = 1f;
    [Header("Burn")]
    [SerializeField, Min(1)] private int burnTurns = 5;
    [SerializeField, Min(1)] private int burnDamageDivisor = 5;
    [Header("Freeze")]
    [SerializeField, Min(1)] private int freezeStacksApplied = 1;
    [SerializeField, Min(1)] private int freezeThreshold = 10;
    [SerializeField, Min(1)] private int frozenTurns = 1;

    public string SpellId => spellId;
    public string DisplayName => displayName;
    public DiceSpellRarity Rarity => rarity;
    public int SpellDefaultDamage => spellDefaultDamage;
    public int Weight => weight;
    public string Role => role;
    public string Description => description;
    public Sprite Icon => icon;
    public Sprite AcquiredImage => acquiredImage != null ? acquiredImage : icon;
    public int MaxLevel => maxLevel;
    public int DuplicateUpgradeRequirement => duplicateUpgradeRequirement;
    public DiceSpellEffectType EffectType => effectType;
    public float ImmediateDamageMultiplier => immediateDamageMultiplier;
    public int BurnTurns => burnTurns;
    public int BurnDamageDivisor => burnDamageDivisor;
    public int FreezeStacksApplied => freezeStacksApplied;
    public int FreezeThreshold => freezeThreshold;
    public int FrozenTurns => frozenTurns;

    public static DiceSpellDefinition CreateRuntime(
        string id,
        string name,
        Sprite sprite,
        int defaultDamage = 1,
        DiceSpellEffectType runtimeEffectType = DiceSpellEffectType.BonusDamage)
    {
        var spell = CreateInstance<DiceSpellDefinition>();
        spell.spellId = string.IsNullOrWhiteSpace(id) ? "runtime_spell" : id;
        spell.displayName = string.IsNullOrWhiteSpace(name) ? spell.spellId : name;
        spell.spellDefaultDamage = Mathf.Max(0, defaultDamage);
        spell.icon = sprite;
        spell.acquiredImage = sprite;
        spell.effectType = runtimeEffectType;
        spell.ConfigureDefaultsForEffect();
        return spell;
    }

    public virtual int CalculateDamage(int dieDefaultDamage, int pip)
    {
        return Mathf.Max(0, dieDefaultDamage) * Mathf.Clamp(pip, 1, 6) * Mathf.Max(0, spellDefaultDamage);
    }

    public virtual int ResolveCast(DiceSpellCastContext context)
    {
        var spellTotalDamage = Mathf.Max(0, context.SpellDamage);

        switch (effectType)
        {
            case DiceSpellEffectType.FireBlast:
                ApplyBurn(context.Target, spellTotalDamage);
                return ScaleDamage(spellTotalDamage, immediateDamageMultiplier);

            case DiceSpellEffectType.FrostImpact:
                context.Target?.ApplyFreezeStacks(freezeStacksApplied, freezeThreshold, frozenTurns);
                return ScaleDamage(spellTotalDamage, immediateDamageMultiplier);

            default:
                return spellTotalDamage;
        }
    }

    public virtual void OnCast(DiceSpellCastContext context)
    {
    }

    private void ConfigureDefaultsForEffect()
    {
        switch (effectType)
        {
            case DiceSpellEffectType.FireBlast:
                immediateDamageMultiplier = 0.5f;
                burnTurns = 5;
                burnDamageDivisor = 5;
                break;

            case DiceSpellEffectType.FrostImpact:
                immediateDamageMultiplier = 1f;
                freezeStacksApplied = 1;
                freezeThreshold = 10;
                frozenTurns = 1;
                break;
        }
    }

    private void ApplyBurn(DiceEnemyAI target, int spellTotalDamage)
    {
        if (target == null)
        {
            return;
        }

        var tickDamage = Mathf.Max(1, Mathf.RoundToInt(spellTotalDamage / (float)Mathf.Max(1, burnDamageDivisor)));
        target.ApplyBurn(tickDamage, burnTurns);
    }

    private static int ScaleDamage(int damage, float multiplier)
    {
        return Mathf.Max(0, Mathf.RoundToInt(damage * Mathf.Max(0f, multiplier)));
    }
}
