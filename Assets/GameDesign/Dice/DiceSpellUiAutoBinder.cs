using UnityEngine;
using UnityEngine.UI;

public sealed class DiceSpellUiAutoBinder : MonoBehaviour
{
    [SerializeField] private bool configureOnStart = true;
    [SerializeField] private DiceSpellLoadout loadout;
    [SerializeField] private Canvas dragCanvas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        if (FindObjectByName("DiceSpellEditor") == null)
        {
            return;
        }

        var existing = FindFirstObjectByType<DiceSpellUiAutoBinder>();
        if (existing != null)
        {
            return;
        }

        var binderObject = new GameObject("Dice Spell UI Auto Binder");
        binderObject.AddComponent<DiceSpellUiAutoBinder>();
    }

    private void Start()
    {
        if (configureOnStart)
        {
            Configure();
        }
    }

    [ContextMenu("Configure Spell Drag Drop")]
    public void Configure()
    {
        if (loadout == null)
        {
            loadout = FindPlayerSpellLoadout();
        }

        if (dragCanvas == null)
        {
            dragCanvas = FindFirstObjectByType<Canvas>();
        }

        ConfigureSpellProfile(1, "Fire Ball", "MobileUI/Spells/Fire ball", DiceSpellEffectType.FireBlast);
        ConfigureSpellProfile(2, "Ice", "MobileUI/Spells/Ice Icon", DiceSpellEffectType.FrostImpact);

        for (var pip = 1; pip <= 6; pip++)
        {
            ConfigurePipSlot(pip);
        }
    }

    private void ConfigureSpellProfile(
        int profileNumber,
        string spellName,
        string resourcePath,
        DiceSpellEffectType effectType)
    {
        var profile = FindObjectByName($"SpellProfile{profileNumber}");
        if (profile == null)
        {
            return;
        }

        var image = GetBestImage(profile);
        var sprite = image != null && image.sprite != null ? image.sprite : Resources.Load<Sprite>(resourcePath);
        if (sprite == null)
        {
            return;
        }

        if (image != null)
        {
            image.sprite = sprite;
            image.enabled = true;
            image.raycastTarget = true;
        }

        var canvasGroup = profile.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = profile.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = true;
        canvasGroup.interactable = true;

        var dragItem = profile.GetComponent<DiceSpellDragItem>();
        if (dragItem == null)
        {
            dragItem = profile.gameObject.AddComponent<DiceSpellDragItem>();
        }

        var spell = DiceSpellDefinition.CreateRuntime(
            $"runtime_{spellName.ToLowerInvariant().Replace(" ", "_")}",
            spellName,
            sprite,
            1,
            effectType);

        dragItem.Configure(spell, image, dragCanvas, canvasGroup);
    }

    private void ConfigurePipSlot(int pip)
    {
        var slot = FindObjectByName($"OwnedSpellsPip{pip}");
        if (slot == null)
        {
            return;
        }

        var image = slot.GetComponent<Image>();
        if (image == null)
        {
            image = slot.GetComponentInChildren<Image>(true);
        }

        if (image != null)
        {
            image.raycastTarget = true;
        }

        var dropTarget = slot.GetComponent<DicePipSpellDropTarget>();
        if (dropTarget == null)
        {
            dropTarget = slot.gameObject.AddComponent<DicePipSpellDropTarget>();
        }

        dropTarget.Configure(pip, loadout, image);
    }

    private static Image GetBestImage(Transform root)
    {
        var childImages = root.GetComponentsInChildren<Image>(true);
        for (var i = 0; i < childImages.Length; i++)
        {
            if (childImages[i] != null && childImages[i].sprite != null && childImages[i].transform != root)
            {
                return childImages[i];
            }
        }

        return root.GetComponent<Image>();
    }

    private static Transform FindObjectByName(string objectName)
    {
        var transforms = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (var i = 0; i < transforms.Length; i++)
        {
            if (transforms[i].name == objectName)
            {
                return transforms[i];
            }
        }

        return null;
    }

    private static DiceSpellLoadout FindPlayerSpellLoadout()
    {
        var battleController = FindFirstObjectByType<DiceBattleController>();
        if (battleController != null && battleController.TryGetComponent(out DiceSpellLoadout playerLoadout))
        {
            return playerLoadout;
        }

        var diceDamage = FindFirstObjectByType<D6DiceDamage>();
        if (diceDamage != null && diceDamage.TryGetComponent(out playerLoadout))
        {
            return playerLoadout;
        }

        return FindFirstObjectByType<DiceSpellLoadout>();
    }
}
