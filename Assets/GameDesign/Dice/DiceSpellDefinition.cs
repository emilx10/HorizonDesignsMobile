using UnityEngine;

[CreateAssetMenu(menuName = "Horizon Designs/Dice Spell", fileName = "New Dice Spell")]
public class DiceSpellDefinition : ScriptableObject
{
    [SerializeField] private string spellId = "spell";
    [SerializeField] private string displayName = "Spell";
    [SerializeField, Min(0)] private int spellDefaultDamage = 1;
    [SerializeField, Min(0)] private int weight = 1;
    [SerializeField] private string role = "Damage";
    [SerializeField, TextArea] private string description;
    [SerializeField] private Sprite icon;

    public string SpellId => spellId;
    public string DisplayName => displayName;
    public int SpellDefaultDamage => spellDefaultDamage;
    public int Weight => weight;
    public string Role => role;
    public string Description => description;
    public Sprite Icon => icon;

    public virtual int CalculateDamage(int dieDefaultDamage, int pip)
    {
        return Mathf.Max(0, dieDefaultDamage) * Mathf.Clamp(pip, 1, 6) * Mathf.Max(0, spellDefaultDamage);
    }

    public virtual void OnCast(DiceSpellCastContext context)
    {
    }
}
