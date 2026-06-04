using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class DiceSpellDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private DiceSpellDefinition spell;
    [SerializeField] private Image iconImage;
    [SerializeField] private Canvas dragCanvas;
    [SerializeField] private CanvasGroup canvasGroup;

    private RectTransform rectTransform;
    private Transform originalParent;
    private Vector2 originalAnchoredPosition;

    public DiceSpellDefinition Spell => spell;

    private void Awake()
    {
        rectTransform = transform as RectTransform;

        if (iconImage == null)
        {
            iconImage = GetComponent<Image>();
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        if (dragCanvas == null)
        {
            dragCanvas = GetComponentInParent<Canvas>();
        }

        RefreshIcon();
    }

    public void SetSpell(DiceSpellDefinition value)
    {
        spell = value;
        RefreshIcon();
    }

    public void Configure(DiceSpellDefinition spellDefinition, Image image, Canvas canvas, CanvasGroup group)
    {
        spell = spellDefinition;
        iconImage = image != null ? image : iconImage;
        dragCanvas = canvas != null ? canvas : dragCanvas;
        canvasGroup = group != null ? group : canvasGroup;
        rectTransform = transform as RectTransform;
        RefreshIcon();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (spell == null || rectTransform == null)
        {
            return;
        }

        originalParent = transform.parent;
        originalAnchoredPosition = rectTransform.anchoredPosition;

        if (dragCanvas != null)
        {
            transform.SetParent(dragCanvas.transform, true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = false;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (spell == null || rectTransform == null)
        {
            return;
        }

        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (rectTransform == null)
        {
            return;
        }

        if (originalParent != null)
        {
            transform.SetParent(originalParent, true);
        }

        rectTransform.anchoredPosition = originalAnchoredPosition;

        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = true;
        }
    }

    private void RefreshIcon()
    {
        if (iconImage == null)
        {
            return;
        }

        iconImage.sprite = spell != null ? spell.Icon : null;
        iconImage.enabled = spell == null || spell.Icon != null;
    }
}
