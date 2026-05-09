using UnityEngine;

[DisallowMultipleComponent]
public sealed class D6DiceVisual : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float size = 1f;
    [SerializeField, Min(0.01f)] private float pipRadius = 0.075f;
    [SerializeField, Min(0.01f)] private float pipDepth = 0.018f;
    [SerializeField] private Material bodyMaterial;
    [SerializeField] private Material pipMaterial;

    private const string GeneratedRootName = "Generated Pips";

    private static readonly Vector2[] One = { Vector2.zero };
    private static readonly Vector2[] Two = { new(-1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Three = { new(-1f, -1f), Vector2.zero, new(1f, 1f) };
    private static readonly Vector2[] Four = { new(-1f, -1f), new(-1f, 1f), new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Five = { new(-1f, -1f), new(-1f, 1f), Vector2.zero, new(1f, -1f), new(1f, 1f) };
    private static readonly Vector2[] Six = { new(-1f, -1f), new(-1f, 0f), new(-1f, 1f), new(1f, -1f), new(1f, 0f), new(1f, 1f) };

    private void Awake()
    {
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

        var root = new GameObject(GeneratedRootName).transform;
        root.SetParent(transform, false);

        AddFace(root, Vector3.up, Vector3.forward, Vector3.right, One);
        AddFace(root, Vector3.down, Vector3.forward, Vector3.right, Six);
        AddFace(root, Vector3.forward, Vector3.up, Vector3.right, Two);
        AddFace(root, Vector3.back, Vector3.up, Vector3.right, Five);
        AddFace(root, Vector3.right, Vector3.up, Vector3.forward, Three);
        AddFace(root, Vector3.left, Vector3.up, Vector3.forward, Four);
    }

    private void ApplyBody()
    {
        transform.localScale = Vector3.one * size;

        if (bodyMaterial != null && TryGetComponent(out MeshRenderer meshRenderer))
        {
            meshRenderer.sharedMaterial = bodyMaterial;
        }
    }

    private void AddFace(Transform root, Vector3 normal, Vector3 upAxis, Vector3 rightAxis, Vector2[] layout)
    {
        var spacing = size * 0.24f;
        var faceOffset = (size * 0.5f) + (pipDepth * 0.5f);

        foreach (var pip in layout)
        {
            var pipObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipObject.name = "Pip";
            pipObject.transform.SetParent(root, false);
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
}
