using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DicePipSpellDropTarget : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [SerializeField, Range(1, 6)] private int pip = 1;
    [SerializeField] private DiceSpellLoadout loadout;
    [SerializeField] private Image equippedIconImage;
    [SerializeField] private bool unequipOnTap = true;

    private Sprite emptySlotSprite;
    private Color emptySlotColor;

    public int Pip => pip;

    public void Configure(int pipValue, DiceSpellLoadout spellLoadout, Image image)
    {
        pip = Mathf.Clamp(pipValue, 1, 6);
        loadout = spellLoadout != null ? spellLoadout : loadout;
        equippedIconImage = image != null ? image : equippedIconImage;

        if (equippedIconImage != null && emptySlotSprite == null)
        {
            emptySlotSprite = equippedIconImage.sprite;
            emptySlotColor = equippedIconImage.color;
        }

        Refresh();
    }

    private void Awake()
    {
        if (loadout == null)
        {
            loadout = FindFirstObjectByType<DiceSpellLoadout>();
        }

        if (equippedIconImage == null)
        {
            equippedIconImage = GetComponent<Image>();
        }

        if (equippedIconImage != null)
        {
            emptySlotSprite = equippedIconImage.sprite;
            emptySlotColor = equippedIconImage.color;
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (loadout != null)
        {
            loadout.SpellEquipped += HandleSpellEquipped;
            loadout.SpellUnequipped += HandleSpellUnequipped;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (loadout != null)
        {
            loadout.SpellEquipped -= HandleSpellEquipped;
            loadout.SpellUnequipped -= HandleSpellUnequipped;
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (loadout == null || eventData.pointerDrag == null)
        {
            return;
        }

        var dragItem = eventData.pointerDrag.GetComponent<DiceSpellDragItem>();
        if (dragItem == null || dragItem.Spell == null)
        {
            return;
        }

        loadout.EquipSpell(pip, dragItem.Spell);
        Refresh();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!unequipOnTap || loadout == null)
        {
            return;
        }

        loadout.UnequipSpell(pip);
        Refresh();
    }

    private void HandleSpellEquipped(int changedPip, DiceSpellDefinition _)
    {
        if (changedPip == pip)
        {
            Refresh();
        }
    }

    private void HandleSpellUnequipped(int changedPip)
    {
        if (changedPip == pip)
        {
            Refresh();
        }
    }

    private void Refresh()
    {
        if (equippedIconImage == null || loadout == null)
        {
            return;
        }

        var spell = loadout.GetSpellForPip(pip);
        equippedIconImage.sprite = spell != null && spell.Icon != null ? spell.Icon : emptySlotSprite;
        equippedIconImage.color = spell != null && spell.Icon != null ? Color.white : emptySlotColor;
        equippedIconImage.enabled = true;
    }
}
