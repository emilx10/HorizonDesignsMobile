using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DisallowMultipleComponent]
public sealed class D6DiceVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float size = 1f;
    [SerializeField, Min(0.01f)] private float pipRadius = 0.075f;
    [SerializeField, Min(0.01f)] private float pipDepth = 0.018f;
    [SerializeField] private Sprite bodySprite;
    [SerializeField, Min(0.05f)] private float bodySpriteSize = 1.08f;
    [SerializeField] private Sprite shadowSprite;
    [SerializeField] private Vector2 shadowOffset = new(0f, -0.47f);
    [SerializeField] private Vector2 shadowScale = new(1.12f, 0.42f);
    [SerializeField] private Color shadowColor = new(1f, 1f, 1f, 0.82f);

    [Header("Top Face Pips")]
    [SerializeField, Min(0.01f)] private float topPipSize = 0.035f;
    [SerializeField] private Vector2 topFaceCenter = new(-0.02f, 0.405f);
    [SerializeField] private Vector2 topFacePositionOffset;
    [SerializeField] private Vector2 topFaceSpacing = new(0.11f, 0.038f);
    [SerializeField] private Vector2 topFaceSafeHalfSize = new(0.22f, 0.06f);

    [Header("Front Face Pips")]
    [SerializeField, Min(0.01f)] private float frontPipSize = 0.082f;
    [SerializeField] private Vector2 frontFaceCenter = new(0f, -0.14f);
    [SerializeField] private Vector2 frontFaceSpacing = new(0.19f, 0.15f);
    [SerializeField] private Vector2 frontFaceSafeHalfSize = new(0.42f, 0.29f);

    [Header("Flat Pip Style")]
    [SerializeField, Min(0.05f)] private float flatSpellIconSize = 0.34f;
    [SerializeField] private Color flatPipColor = new(0.08f, 0.08f, 0.08f, 1f);
    [SerializeField] private int bodySpriteSortingOrder;
    [SerializeField, Min(0.05f)] private float spellIconSize = 0.42f;
    [SerializeField, Min(0f)] private float spellIconFaceOffset = 0.003f;
    [SerializeField] private float spellIconRotationDegrees = -90f;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material pipMaterial;
    [SerializeField] private DiceSpellLoadout spellLoadout;
    [Header("Editor Preview")]
    [SerializeField, Range(1, 6)] private int previewTopPip = 6;
    [SerializeField, Range(1, 6)] private int previewFrontPip = 2;

    private const string GeneratedRootName = "Generated Pips";
    private const string GeneratedFlatRootName = "Generated Flat Dice";
    private const string GeneratedSpellRootName = "Generated Spell Faces";

    private static readonly Vector2[] One = { Vector2.zero };
    private static readonly Vector2[] Two = { new(-1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Three = { new(-1f, -1f), Vector2.zero, new(1f, 1f) };
    private static readonly Vector2[] Four = { new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Five = { new(-1f, -1f), new(-1f, 1f), Vector2.zero, new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Six = { new(-1f, -1f), new(-1f, 0f), new(-1f, 1f), new(1f, -1f), new(1f, 0f), new(1f, 1f) };

    private int visibleTopPip = 1;
    private int visibleFrontPip = 1;
    private static Sprite flatPipSprite;

    public bool UsesFlatSpriteBody => bodySprite != null;

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

    public void SetVisiblePips(int topPip, int frontPip)
    {
        visibleTopPip = Mathf.Clamp(topPip, 1, 6);
        visibleFrontPip = Mathf.Clamp(frontPip, 1, 6);

        if (UsesFlatSpriteBody)
        {
            BuildFlatDice();
        }
    }

    public void RandomizeVisiblePips()
    {
        var top = Random.Range(1, 7);
        SetVisiblePips(top, GetRandomAdjacentValue(top));
    }

    private static int GetRandomAdjacentValue(int value)
    {
        value = Mathf.Clamp(value, 1, 6);
        var opposite = GetOppositeValue(value);
        var choice = Random.Range(1, 5);

        for (var candidate = 1; candidate <= 6; candidate++)
        {
            if (candidate == value || candidate == opposite)
            {
                continue;
            }

            choice--;
            if (choice == 0)
            {
                return candidate;
            }
        }

        return value == 1 ? 2 : 1;
    }

    private static int GetOppositeValue(int value)
    {
        return Mathf.Clamp(value, 1, 6) switch
        {
            1 => 6,
            2 => 5,
            3 => 4,
            4 => 3,
            5 => 2,
            _ => 1
        };
    }

    private void OnValidate()
    {
        visibleTopPip = Mathf.Clamp(previewTopPip, 1, 6);
        visibleFrontPip = Mathf.Clamp(previewFrontPip, 1, 6);

    }

    [ContextMenu("Refresh Dice Preview")]
    private void RefreshDicePreview()
    {
#if UNITY_EDITOR
        if (IsPersistentPrefabAsset())
        {
            return;
        }
#endif

        visibleTopPip = Mathf.Clamp(previewTopPip, 1, 6);
        visibleFrontPip = Mathf.Clamp(previewFrontPip, 1, 6);
        Build();
    }

    [ContextMenu("Clear Generated Dice Preview")]
    private void ClearGeneratedVisuals()
    {
        ClearGeneratedPips();
        ClearGeneratedFlatDice();
        ClearGeneratedSpellFaces();
    }

    public void Build()
    {
#if UNITY_EDITOR
        if (IsPersistentPrefabAsset())
        {
            return;
        }
#endif

        ApplyBody();

        if (UsesFlatSpriteBody)
        {
            ClearGeneratedPips();
            ClearGeneratedSpellFaces();
            BuildFlatDice();
            return;
        }

        ClearGeneratedFlatDice();
        ClearGeneratedPips();
        ClearGeneratedSpellFaces();

        var root = new GameObject(GeneratedRootName).transform;
        MarkGenerated(root.gameObject);
        root.SetParent(transform, false);

        var spellRoot = new GameObject(GeneratedSpellRootName).transform;
        MarkGenerated(spellRoot.gameObject);
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

        if (!TryGetComponent(out MeshRenderer meshRenderer))
        {
            return;
        }

        meshRenderer.enabled = !UsesFlatSpriteBody;

        if (!UsesFlatSpriteBody && bodyMaterial != null)
        {
            meshRenderer.sharedMaterial = bodyMaterial;
        }
    }

    public bool ShouldRendererStayHidden(Renderer targetRenderer)
    {
        return UsesFlatSpriteBody
               && targetRenderer != null
               && targetRenderer.gameObject == gameObject
               && targetRenderer is MeshRenderer;
    }

    private void BuildFlatDice()
    {
        ClearGeneratedFlatDice();

        var root = new GameObject(GeneratedFlatRootName).transform;
        MarkGenerated(root.gameObject);
        root.SetParent(transform, false);
        root.localRotation = Quaternion.identity;
        root.localPosition = Vector3.zero;

        AddShadowSprite(root);
        AddBodySprite(root);
        AddFlatFace(root, visibleTopPip, topFaceCenter + topFacePositionOffset, topFaceSpacing, topFaceSafeHalfSize, topPipSize, bodySpriteSortingOrder + 1);
        AddFlatFace(root, visibleFrontPip, frontFaceCenter, frontFaceSpacing, frontFaceSafeHalfSize, frontPipSize, bodySpriteSortingOrder + 1);
        AddFlatSpellIcon(root, visibleTopPip, topFaceCenter + topFacePositionOffset, bodySpriteSortingOrder + 2);
        AddFlatSpellIcon(root, visibleFrontPip, frontFaceCenter, bodySpriteSortingOrder + 2);
    }

    private void AddBodySprite(Transform root)
    {
        var bodyObject = new GameObject("Sprite Body", typeof(SpriteRenderer));
        MarkGenerated(bodyObject);
        bodyObject.transform.SetParent(root, false);

        var spriteRenderer = bodyObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = bodySprite;
        spriteRenderer.sortingOrder = bodySpriteSortingOrder;

        var spriteSize = bodySprite.bounds.size;
        var largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        var spriteScale = largestSide > 0f ? bodySpriteSize / largestSide : bodySpriteSize;
        bodyObject.transform.localScale = Vector3.one * spriteScale;
    }

    private void AddShadowSprite(Transform root)
    {
        if (shadowSprite == null)
        {
            return;
        }

        var shadowObject = new GameObject("Dice Shadow", typeof(SpriteRenderer));
        MarkGenerated(shadowObject);
        shadowObject.transform.SetParent(root, false);
        shadowObject.transform.localPosition = new Vector3(shadowOffset.x, shadowOffset.y, 0.02f);

        var spriteRenderer = shadowObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = shadowSprite;
        spriteRenderer.color = shadowColor;
        spriteRenderer.sortingOrder = bodySpriteSortingOrder - 1;

        var spriteSize = shadowSprite.bounds.size;
        var shadowSize = new Vector2(bodySpriteSize * shadowScale.x, bodySpriteSize * shadowScale.y);
        var scaleX = spriteSize.x > 0f ? shadowSize.x / spriteSize.x : shadowScale.x;
        var scaleY = spriteSize.y > 0f ? shadowSize.y / spriteSize.y : shadowScale.y;
        shadowObject.transform.localScale = new Vector3(scaleX, scaleY, 1f);
    }

    private void AddFlatFace(Transform root, int value, Vector2 center, Vector2 spacing, Vector2 safeHalfSize, float pipSize, int sortingOrder)
    {
        foreach (var pip in GetLayout(value))
        {
            var pipObject = new GameObject("Flat Pip", typeof(SpriteRenderer));
            MarkGenerated(pipObject);
            pipObject.name = "Flat Pip";
            pipObject.transform.SetParent(root, false);
            var pipRadius = pipSize * 0.5f;
            var maxX = Mathf.Max(0f, safeHalfSize.x - pipRadius);
            var maxY = Mathf.Max(0f, safeHalfSize.y - pipRadius);
            var localX = Mathf.Clamp(pip.x * spacing.x, -maxX, maxX);
            var localY = Mathf.Clamp(pip.y * spacing.y, -maxY, maxY);
            pipObject.transform.localPosition = new Vector3(center.x + localX, center.y + localY, -0.01f);
            pipObject.transform.localScale = Vector3.one * pipSize;

            var spriteRenderer = pipObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = GetFlatPipSprite();
            spriteRenderer.color = flatPipColor;
            spriteRenderer.sortingOrder = sortingOrder;
        }
    }

    private void AddFlatSpellIcon(Transform root, int pip, Vector2 center, int sortingOrder)
    {
        if (spellLoadout == null)
        {
            return;
        }

        var spell = spellLoadout.GetSpellForPip(pip);
        if (spell == null || spell.Icon == null)
        {
            return;
        }

        var iconObject = new GameObject($"Flat Spell Pip {pip}", typeof(SpriteRenderer));
        MarkGenerated(iconObject);
        iconObject.transform.SetParent(root, false);
        iconObject.transform.localPosition = new Vector3(center.x, center.y, -0.02f);

        var sprite = spell.Icon;
        var spriteSize = sprite.bounds.size;
        var largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        var iconScale = largestSide > 0f ? flatSpellIconSize / largestSide : flatSpellIconSize;
        iconObject.transform.localScale = Vector3.one * iconScale;

        var spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = sortingOrder;
    }

    private static Sprite GetFlatPipSprite()
    {
        if (flatPipSprite != null)
        {
            return flatPipSprite;
        }

        const int textureSize = 32;
        var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        var center = new Vector2((textureSize - 1) * 0.5f, (textureSize - 1) * 0.5f);
        var radius = textureSize * 0.42f;

        for (var y = 0; y < textureSize; y++)
        {
            for (var x = 0; x < textureSize; x++)
            {
                var distance = Vector2.Distance(new Vector2(x, y), center);
                texture.SetPixel(x, y, distance <= radius ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        flatPipSprite = Sprite.Create(texture, new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), textureSize);
        return flatPipSprite;
    }

    private void AddFace(Transform pipRoot, Transform spellRoot, int value, Vector3 normal, Vector3 upAxis, Vector3 rightAxis, Vector2[] layout)
    {
        AddSpellFace(spellRoot, value, normal, upAxis);

        var spacing = size * 0.24f;
        var faceOffset = (size * 0.5f) + (pipDepth * 0.5f);

        foreach (var pip in layout)
        {
            var pipObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            MarkGenerated(pipObject);
            pipObject.name = "Pip";
            pipObject.transform.SetParent(pipRoot, false);
            pipObject.transform.localPosition = (normal * faceOffset) + (rightAxis * pip.x * spacing) + (upAxis * pip.y * spacing);
            pipObject.transform.localRotation = Quaternion.FromToRotation(Vector3.up, normal);
            pipObject.transform.localScale = new Vector3(pipRadius * 2f, pipDepth * 0.5f, pipRadius * 2f);

            if (pipObject.TryGetComponent(out Collider pipCollider))
            {
                DestroyGenerated(pipCollider);
            }

            if (pipMaterial != null && pipObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                meshRenderer.sharedMaterial = pipMaterial;
            }
        }
    }

    private void AddSpellFace(Transform root, int value, Vector3 normal, Vector3 upAxis)
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
        MarkGenerated(iconObject);
        iconObject.transform.SetParent(root, false);
        iconObject.transform.localPosition = normal * ((size * 0.5f) + spellIconFaceOffset);
        iconObject.transform.localRotation = Quaternion.LookRotation(normal, upAxis)
                                             * Quaternion.Euler(0f, 0f, spellIconRotationDegrees);

        var sprite = spell.Icon;
        var spriteSize = sprite.bounds.size;
        var largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
        var iconScale = largestSide > 0f ? spellIconSize / largestSide : spellIconSize;
        iconObject.transform.localScale = Vector3.one * iconScale;

        var spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = sprite;
        spriteRenderer.sortingOrder = 1;
    }

    private static IEnumerable<Vector2> GetLayout(int value)
    {
        return Mathf.Clamp(value, 1, 6) switch
        {
            1 => One,
            2 => Two,
            3 => Three,
            4 => Four,
            5 => Five,
            _ => Six
        };
    }

    private void ClearGeneratedPips()
    {
        ClearGeneratedRoot(GeneratedRootName);
    }

    private void ClearGeneratedFlatDice()
    {
        ClearGeneratedRoot(GeneratedFlatRootName);
    }

    private void ClearGeneratedSpellFaces()
    {
        ClearGeneratedRoot(GeneratedSpellRootName);
    }

    private void ClearGeneratedRoot(string rootName)
    {
        for (var i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (child.name == rootName)
            {
                DestroyGenerated(child.gameObject);
            }
        }
    }

    private static void MarkGenerated(GameObject target)
    {
        if (target != null && !Application.isPlaying)
        {
            target.hideFlags = HideFlags.DontSaveInEditor | HideFlags.DontSaveInBuild;
        }
    }

    private static void DestroyGenerated(Object target)
    {
        if (target == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }

#if UNITY_EDITOR
    [InitializeOnLoadMethod]
    private static void ClearGeneratedDicePreviewsAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (!Application.isPlaying)
            {
                ClearAllGeneratedDicePreviews();
            }
        };
    }

    private bool IsPersistentPrefabAsset()
    {
        return !Application.isPlaying && EditorUtility.IsPersistent(gameObject);
    }

    [MenuItem("Tools/Horizon Designs/Dice/Clear Generated Dice Previews")]
    private static void ClearAllGeneratedDicePreviews()
    {
        foreach (var visual in Resources.FindObjectsOfTypeAll<D6DiceVisual>())
        {
            if (visual == null || EditorUtility.IsPersistent(visual.gameObject))
            {
                continue;
            }

            visual.ClearGeneratedVisuals();
        }

        foreach (var transform in Resources.FindObjectsOfTypeAll<Transform>())
        {
            if (transform == null || EditorUtility.IsPersistent(transform.gameObject))
            {
                continue;
            }

            if (IsGeneratedPreviewTransformName(transform.name))
            {
                DestroyImmediate(transform.gameObject);
            }
        }
    }

    private static bool IsGeneratedPreviewTransformName(string transformName)
    {
        return transformName is GeneratedRootName
            or GeneratedFlatRootName
            or GeneratedSpellRootName
            or "Sprite Body"
            or "Flat Pip"
            or "Dice Shadow"
            || transformName.StartsWith("Flat Spell Pip")
            || transformName.StartsWith("Spell Face");
    }
#endif

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
