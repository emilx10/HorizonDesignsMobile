using System;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DiceSpellLoadout : MonoBehaviour
{
    [Serializable]
    private sealed class PipSpellSlot
    {
        [SerializeField, Range(1, 6)] private int pip = 1;
        [SerializeField] private DiceSpellDefinition spell;

        public int Pip => pip;
        public DiceSpellDefinition Spell
        {
            get => spell;
            set => spell = value;
        }

        public void SetPip(int value)
        {
            pip = Mathf.Clamp(value, 1, 6);
        }
    }

    [SerializeField] private PipSpellSlot[] equippedSpells =
    {
        new(), new(), new(), new(), new(), new()
    };

    public event Action<int, DiceSpellDefinition> SpellEquipped;
    public event Action<int> SpellUnequipped;
    public event Action<int, DiceSpellDefinition, int> SpellCast;

    private void OnValidate()
    {
        EnsureSlots();
    }

    private void Awake()
    {
        EnsureSlots();
    }

    public DiceSpellDefinition GetSpellForPip(int pip)
    {
        pip = Mathf.Clamp(pip, 1, 6);
        EnsureSlots();
        return equippedSpells[pip - 1].Spell;
    }

    public void EquipSpell(int pip, DiceSpellDefinition spell)
    {
        pip = Mathf.Clamp(pip, 1, 6);
        EnsureSlots();

        equippedSpells[pip - 1].Spell = spell;

        if (spell != null)
        {
            SpellEquipped?.Invoke(pip, spell);
        }
        else
        {
            SpellUnequipped?.Invoke(pip);
        }
    }

    public void UnequipSpell(int pip)
    {
        EquipSpell(pip, null);
    }

    public bool HasSpellForPip(int pip)
    {
        return GetSpellForPip(pip) != null;
    }

    public int CalculateSpellDamage(int pip, D6DiceDamage diceDamage)
    {
        var spell = GetSpellForPip(pip);
        if (spell == null || diceDamage == null)
        {
            return 0;
        }

        return spell.CalculateDamage(diceDamage.DefaultDamage, pip);
    }

    public int CastSpellForPip(int pip, D6DiceDamage diceDamage, DiceEnemyAI target)
    {
        pip = Mathf.Clamp(pip, 1, 6);

        var spell = GetSpellForPip(pip);
        if (spell == null || diceDamage == null)
        {
            return 0;
        }

        var rawSpellDamage = spell.CalculateDamage(diceDamage.DefaultDamage, pip);
        var baseDiceDamage = diceDamage.DefaultDamage * pip;
        var context = new DiceSpellCastContext(gameObject, target, diceDamage, pip, baseDiceDamage, rawSpellDamage);
        var resolvedSpellDamage = spell.ResolveCast(context);

        spell.OnCast(context);
        SpellCast?.Invoke(pip, spell, resolvedSpellDamage);

        return resolvedSpellDamage;
    }

    private void EnsureSlots()
    {
        if (equippedSpells == null || equippedSpells.Length != 6)
        {
            var oldSlots = equippedSpells;
            equippedSpells = new PipSpellSlot[6];

            for (var i = 0; i < equippedSpells.Length; i++)
            {
                equippedSpells[i] = oldSlots != null && i < oldSlots.Length && oldSlots[i] != null
                    ? oldSlots[i]
                    : new PipSpellSlot();
            }
        }

        for (var i = 0; i < equippedSpells.Length; i++)
        {
            equippedSpells[i] ??= new PipSpellSlot();
            equippedSpells[i].SetPip(i + 1);
        }
    }
}
