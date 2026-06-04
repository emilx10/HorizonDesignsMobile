using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DiceSpellProfileView : MonoBehaviour
{
    [SerializeField] private DiceSpellDefinition spell;
    [SerializeField] private DiceSpellInventory inventory;
    [SerializeField] private Image acquiredSpellImage;
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private TMP_Text duplicateText;
    [SerializeField] private GameObject lockedState;
    [SerializeField] private string levelFormat = "LVL {0}";
    [SerializeField] private string duplicateFormat = "{0}/{1}";

    private void Awake()
    {
        if (inventory == null)
        {
            inventory = FindFirstObjectByType<DiceSpellInventory>();
        }

        if (acquiredSpellImage == null)
        {
            acquiredSpellImage = GetComponentInChildren<Image>(true);
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (inventory != null)
        {
            inventory.SpellGranted += HandleSpellChanged;
            inventory.SpellUpgraded += HandleSpellUpgraded;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (inventory != null)
        {
            inventory.SpellGranted -= HandleSpellChanged;
            inventory.SpellUpgraded -= HandleSpellUpgraded;
        }
    }

    public void SetSpell(DiceSpellDefinition value)
    {
        spell = value;
        Refresh();
    }

    public void Refresh()
    {
        var ownership = inventory != null ? inventory.GetOwnership(spell) : null;
        var isOwned = ownership != null;

        if (acquiredSpellImage != null)
        {
            acquiredSpellImage.sprite = spell != null ? spell.AcquiredImage : null;
            acquiredSpellImage.enabled = isOwned && spell != null && spell.AcquiredImage != null;
        }

        if (lockedState != null)
        {
            lockedState.SetActive(!isOwned);
        }

        SetText(levelText, isOwned ? string.Format(levelFormat, ownership.Level) : string.Empty);
        SetText(duplicateText, isOwned && spell != null
            ? string.Format(duplicateFormat, ownership.Duplicates, spell.DuplicateUpgradeRequirement)
            : string.Empty);
    }

    private void HandleSpellChanged(DiceSpellDefinition changedSpell, DiceSpellInventory.SpellOwnership _, bool __)
    {
        if (changedSpell == spell)
        {
            Refresh();
        }
    }

    private void HandleSpellUpgraded(DiceSpellDefinition changedSpell, DiceSpellInventory.SpellOwnership _)
    {
        if (changedSpell == spell)
        {
            Refresh();
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
