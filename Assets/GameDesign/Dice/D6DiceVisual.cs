using UnityEngine;

[DisallowMultipleComponent]
public sealed class D6DiceVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float size = 1f;
    [SerializeField, Min(0.01f)] private float pipRadius = 0.075f;
    [SerializeField, Min(0.01f)] private float pipDepth = 0.018f;
    [SerializeField, Min(0.05f)] private float spellIconSize = 0.42f;
    [SerializeField, Min(0f)] private float spellIconFaceOffset = 0.003f;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material pipMaterial;
    [SerializeField] private DiceSpellLoadout spellLoadout;

    private const string GeneratedRootName = "Generated Pips";
    private const string GeneratedSpellRootName = "Generated Spell Faces";

    private static readonly Vector2[] One = { Vector2.zero };
    private static readonly Vector2[] Two = { new(-1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Three = { new(-1f, -1f), Vector2.zero, new(1f, 1f) };
    private static readonly Vector2[] Four = { new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Five = { new(-1f, -1f), new(-1f, 1f), Vector2.zero, new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Six = { new(-1f, -1f), new(-1f, 0f), new(-1f, 1f), new(1f, -1f), new(1f, 0f), new(1f, 1f) };

    private void Awake()
    {
        if (spellLoadout == null)
        {
            spellLoadout = GetComponent<DiceSpellLoadout>();
        }

        Build();
    }

    private void OnEnable()
    {
        if (spellLoadout == null)
        {
            spellLoadout = GetComponent<DiceSpellLoadout>();
        }

        SubscribeToLoadout();
    }

    private void OnDisable()
    {
        UnsubscribeFromLoadout();
    }

    public void SetSpellLoadout(DiceSpellLoadout loadout)
    {
        if (spellLoadout == loadout)
        {
            Build();
            return;
        }

        UnsubscribeFromLoadout();
        spellLoadout = loadout;
        SubscribeToLoadout();
        Build();
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Build();
    }

    public void Build()
    {
        ApplyBody();
        ClearGeneratedPips();
        ClearGeneratedSpellFaces();

        var root = new GameObject(GeneratedRootName).transform;
        root.SetParent(transform, false);

        var spellRoot = new GameObject(GeneratedSpellRootName).transform;
        spellRoot.SetParent(transform, false);

        AddFace(root, spellRoot, 1, Vector3.up, Vector3.forward, Vector3.right, One);
        AddFace(root, spellRoot, 6, Vector3.down, Vector3.forward, Vector3.right, Six);
        AddFace(root, spellRoot, 2, Vector3.forward, Vector3.up, Vector3.right, Two);
        AddFace(root, spellRoot, 5, Vector3.back, Vector3.up, Vector3.right, Five);
        AddFace(root, spellRoot, 3, Vector3.right, Vector3.up, Vector3.forward, Three);
        AddFace(root, spellRoot, 4, Vector3.left, Vector3.up, Vector3.forward, Four);
    }

    private void ApplyBody()
    {
        transform.localScale = Vector3.one * size;

        if (bodyMaterial != null && TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.sharedMaterial = bodyMaterial;
        }
    }

    private void AddFace(Transform pipRoot, Transform spellRoot, int value, Vector3 normal, Vector3 upAxis, Vector3 rightAxis, Vector2[] layout)
    {
        AddSpellFace(spellRoot, value, normal, upAxis, rightAxis);

        var spacing = size * 0.24f;
        var faceOffset = (size * 0.5f) + (pipDepth * 0.5f);

        foreach (var pip in layout)
        {
            var pipObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipObject.name = "Pip";
            pipObject.transform.SetParent(pipRoot, false);
            pipObject.transform.localPosition = (normal * faceOffset) + (rightAxis * pip.x * spacing) + (upAxis * pip.y * spacing);
            pipObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            pipObject.transform.localScale = new Vector3(pipRadius * 2f, pipDepth * 0.5f, pipRadius * 2f);

            if (pipObject.TryGetComponent(out Collider pipCollider))
            {
                pipCollider.enabled = false;
                Destroy(pipCollider);
            }

            if (pipMaterial != null && pipObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = pipMaterial;
            }
        }
    }

    private void AddSpellFace(Transform root, int value, Vector3 normal, Vector3 upAxis, Vector3 rightAxis)
    {
        if (spellLoadout == null)
        {
            return;
        }

        var spell = spellLoadout.GetSpellForPip(value);
        if (spell == null || spell.Icon == null)
        {
            return;
        }

        var iconObject = new GameObject($"Spell Face {value}", typeof(SpriteRenderer));
        iconObject.transform.SetParent(root, false);
        iconObject.transform.localPosition = normal * ((size * 0.5f) + spellIconFaceOffset);
        iconObject.transform.localRotation = Quaternion.LookRotation(normal, upAxis);

        var sprite = spell.Icon;
        var spriteSize = sprite.bounds.size;
        var largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        var iconScale = largestSide > 0f ? spellIconSize / largestSide : spellIconSize;
        iconObject.transform.localScale = new Vector3(iconScale, iconScale, iconScale);

        var spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 1;
    }

    private void ClearGeneratedPips()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == GeneratedRootName)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void ClearGeneratedSpellFaces()
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == GeneratedSpellRootName)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void HandleSpellChanged(int _, DiceSpellDefinition __)
    {
        Build();
    }

    private void HandleSpellChanged(int _)
    {
        Build();
    }

    private void SubscribeToLoadout()
    {
        if (spellLoadout == null)
        {
            return;
        }

        spellLoadout.SpellEquipped -= HandleSpellChanged;
        spellLoadout.SpellUnequipped -= HandleSpellChanged;
        spellLoadout.SpellEquipped += HandleSpellChanged;
        spellLoadout.SpellUnequipped += HandleSpellChanged;
    }

    private void UnsubscribeFromLoadout()
    {
        if (spellLoadout == null)
        {
            return;
        }

        spellLoadout.SpellEquipped -= HandleSpellChanged;
        spellLoadout.SpellUnequipped -= HandleSpellChanged;
    }
}
