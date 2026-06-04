using System;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DiceSpellInventory : MonoBehaviour
{
    [Serializable]
    public sealed class SpellOwnership
    {
        [SerializeField] private DiceSpellDefinition spell;
        [SerializeField, Min(1)] private int level = 1;
        [SerializeField, Min(0)] private int duplicates;

        public DiceSpellDefinition Spell => spell;
        public int Level => level;
        public int Duplicates => duplicates;
        public bool IsOwned => spell != null;

        public SpellOwnership(DiceSpellDefinition spell)
        {
            this.spell = spell;
        }

        public bool AddDuplicate()
        {
            if (spell == null || level >= spell.MaxLevel)
            {
                duplicates++;
                return false;
            }

            duplicates++;
            var upgraded = false;
            while (level < spell.MaxLevel && duplicates >= spell.DuplicateUpgradeRequirement)
            {
                duplicates -= spell.DuplicateUpgradeRequirement;
                level++;
                upgraded = true;
            }

            return upgraded;
        }
    }

    [SerializeField] private List<SpellOwnership> ownedSpells = new();

    public event Action<DiceSpellDefinition, SpellOwnership, bool> SpellGranted;
    public event Action<DiceSpellDefinition, SpellOwnership> SpellUpgraded;

    public IReadOnlyList<SpellOwnership> OwnedSpells => ownedSpells;

    public bool IsOwned(DiceSpellDefinition spell)
    {
        return GetOwnership(spell) != null;
    }

    public SpellOwnership GetOwnership(DiceSpellDefinition spell)
    {
        if (spell == null)
        {
            return null;
        }

        for (var i = 0; i < ownedSpells.Count; i++)
        {
            var entry = ownedSpells[i];
            if (entry != null && entry.Spell == spell)
            {
                return entry;
            }
        }

        return null;
    }

    public SpellOwnership GrantSpell(DiceSpellDefinition spell)
    {
        if (spell == null)
        {
            return null;
        }

        var ownership = GetOwnership(spell);
        if (ownership == null)
        {
            ownership = new SpellOwnership(spell);
            ownedSpells.Add(ownership);
            SpellGranted?.Invoke(spell, ownership, false);
            return ownership;
        }

        var upgraded = ownership.AddDuplicate();
        SpellGranted?.Invoke(spell, ownership, true);

        if (upgraded)
        {
            SpellUpgraded?.Invoke(spell, ownership);
        }

        return ownership;
    }
}
