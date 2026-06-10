using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public sealed class DiceSpellVfxController : MonoBehaviour
{
    [Header("Fire Blast")]
    [SerializeField, Min(0.05f)] private float fireballDuration = 0.45f;
    [SerializeField, Min(0.05f)] private float fireballSize = 0.45f;
    [SerializeField, Min(0f)] private float fireballArcHeight = 0.7f;
    [SerializeField, Min(0f)] private float impactDuration = 0.18f;
    [SerializeField, Min(0f)] private float impactSize = 0.65f;
    [SerializeField] private Vector3 castOffset = new(0f, 0.65f, 0f);
    [SerializeField] private Vector3 impactOffset = new(0f, 0.35f, 0f);
    [SerializeField] private Color fireballColor = new(1f, 0.38f, 0.04f, 1f);
    [SerializeField] private Color impactColor = new(1f, 0.78f, 0.1f, 0.75f);
    [SerializeField] private Camera targetCamera;

    public IEnumerator PlayCast(DiceSpellDefinition spell, Transform caster, DiceEnemyAI target)
    {
        if (target == null)
        {
            yield break;
        }

        yield return PlayCastToTransform(spell, caster, target.transform);
    }

    public IEnumerator PlayCastToTransform(DiceSpellDefinition spell, Transform caster, Transform target)
    {
        if (spell == null || caster == null || target == null || spell.EffectType != DiceSpellEffectType.FireBlast)
        {
            yield break;
        }

        var startPosition = caster.position + castOffset;
        var targetPosition = target.position + impactOffset;
        var projectile = CreateFireball(spell, startPosition);

        var time = 0f;
        while (time < fireballDuration && projectile != null)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / fireballDuration);
            var arc = Vector3.up * (Mathf.Sin(t * Mathf.PI) * fireballArcHeight);

            projectile.transform.position = Vector3.Lerp(startPosition, targetPosition, t) + arc;
            FaceCamera(projectile.transform, -720f * time);
            yield return null;
        }

        if (projectile != null)
        {
            Destroy(projectile);
        }

        yield return PlayImpact(targetPosition);
    }

    private GameObject CreateFireball(DiceSpellDefinition spell, Vector3 position)
    {
        if (spell.Icon != null)
        {
            var iconObject = new GameObject("Fire Blast Projectile", typeof(SpriteRenderer));
            iconObject.transform.position = position;

            var spriteRenderer = iconObject.GetComponent<SpriteRenderer>();
            spriteRenderer.sprite = spell.Icon;
            spriteRenderer.color = Color.white;
            spriteRenderer.sortingOrder = 20;

            var spriteSize = spell.Icon.bounds.size;
            var largestSide = Mathf.Max(spriteSize.x, spriteSize.y);
            var scale = largestSide > 0f ? fireballSize / largestSide : fireballSize;
            iconObject.transform.localScale = Vector3.one * scale;
            return iconObject;
        }

        var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Fire Blast Projectile";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * fireballSize;

        if (sphere.TryGetComponent(out Collider sphereCollider))
        {
            sphereCollider.enabled = false;
            Destroy(sphereCollider);
        }

        if (sphere.TryGetComponent(out MeshRenderer renderer))
        {
            renderer.material = CreateUnlitMaterial(fireballColor);
        }

        return sphere;
    }

    private IEnumerator PlayImpact(Vector3 position)
    {
        if (impactDuration <= 0f)
        {
            yield break;
        }

        var impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        impact.name = "Fire Blast Impact";
        impact.transform.position = position;

        if (impact.TryGetComponent(out Collider impactCollider))
        {
            impactCollider.enabled = false;
            Destroy(impactCollider);
        }

        MeshRenderer impactRenderer = null;
        if (impact.TryGetComponent(out MeshRenderer renderer))
        {
            impactRenderer = renderer;
            impactRenderer.material = CreateUnlitMaterial(impactColor);
        }

        var time = 0f;
        while (time < impactDuration && impact != null)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / impactDuration);
            impact.transform.localScale = Vector3.one * Mathf.Lerp(0.05f, impactSize, t);
            FaceCamera(impact.transform, 0f);

            if (impactRenderer != null)
            {
                var color = impactColor;
                color.a = Mathf.Lerp(impactColor.a, 0f, t);
                impactRenderer.material.color = color;
            }

            yield return null;
        }

        if (impact != null)
        {
            Destroy(impact);
        }
    }

    private static Material CreateUnlitMaterial(Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Unlit")
                     ?? Shader.Find("Sprites/Default")
                     ?? Shader.Find("Unlit/Color")
                     ?? Shader.Find("Standard");
        var material = new Material(shader);
        material.color = color;
        return material;
    }

    private void FaceCamera(Transform visual, float rollDegrees)
    {
        if (visual == null)
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            visual.Rotate(0f, 0f, rollDegrees * Time.deltaTime, Space.Self);
            return;
        }

        visual.rotation = targetCamera.transform.rotation * Quaternion.Euler(0f, 0f, rollDegrees);
    }
}
