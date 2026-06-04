using UnityEngine;

public readonly struct DiceSpellCastContext
{
    public DiceSpellCastContext(
        GameObject caster,
        DiceEnemyAI target,
        D6DiceDamage diceDamage,
        int pip,
        int baseDiceDamage,
        int spellDamage)
    {
        Caster = caster;
        Target = target;
        DiceDamage = diceDamage;
        Pip = pip;
        BaseDiceDamage = baseDiceDamage;
        SpellDamage = spellDamage;
    }

    public GameObject Caster { get; }
    public DiceEnemyAI Target { get; }
    public D6DiceDamage DiceDamage { get; }
    public int Pip { get; }
    public int BaseDiceDamage { get; }
    public int SpellDamage { get; }
}
