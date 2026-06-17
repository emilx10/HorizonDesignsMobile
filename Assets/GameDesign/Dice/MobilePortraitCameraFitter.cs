using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class MobilePortraitCameraFitter : MonoBehaviour
{
    [SerializeField] private Transform player;
    [SerializeField] private Transform enemy;
    [SerializeField] private Vector3 portraitPosition = new(0f, 3.25f, -7.5f);
    [SerializeField] private Vector3 portraitEulerAngles = new(20f, 0f, 0f);
    [SerializeField, Min(0.1f)] private float minVerticalSpan = 4.9f;
    [SerializeField, Min(0.1f)] private float minHorizontalSpan = 3.4f;
    [SerializeField, Min(0f)] private float worldPadding = 1.2f;

    private Camera targetCamera;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();

        if (!Application.isPlaying)
        {
            ApplyEditorPreview();
            return;
        }

        ApplyPortraitLock();
        FitNow();
    }

    private void OnEnable()
    {
        ApplyPortraitLock();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus)
        {
            ApplyPortraitLock();
            FitNow();
        }
    }

    private void OnApplicationPause(bool paused)
    {
        if (!paused)
        {
            ApplyPortraitLock();
            FitNow();
        }
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            ApplyEditorPreview();
            return;
        }

        MobileLayoutUtility.RefreshSafeArea();

        if (lastScreenWidth != Screen.width || lastScreenHeight != Screen.height)
        {
            FitNow();
        }
    }

    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            ApplyEditorPreview();
        }
    }

    public void SetTargets(Transform playerTarget, Transform enemyTarget)
    {
        player = playerTarget;
        enemy = enemyTarget;
        FitNow();
    }

    public void FitNow()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;

        transform.SetPositionAndRotation(portraitPosition, Quaternion.Euler(portraitEulerAngles));

        var bounds = BuildBounds();
        var center = bounds.center;
        center.z = 0f;

        transform.position = new Vector3(center.x, portraitPosition.y, portraitPosition.z);
        targetCamera.orthographic = true;

        var aspect = Mathf.Max(0.45f, targetCamera.aspect);
        var neededByHeight = Mathf.Max(minVerticalSpan, bounds.size.y + worldPadding) * 0.5f;
        var neededByWidth = Mathf.Max(minHorizontalSpan, bounds.size.x + worldPadding) / (2f * aspect);
        targetCamera.orthographicSize = Mathf.Max(neededByHeight, neededByWidth);
        ExpandUntilBoundsFit(bounds);
    }

    private Bounds BuildBounds()
    {
        var bounds = new Bounds(Vector3.zero, Vector3.zero);
        var hasBounds = false;

        IncludeTarget(player, ref bounds, ref hasBounds);
        IncludeTarget(enemy, ref bounds, ref hasBounds);

        if (!hasBounds)
        {
            bounds = new Bounds(Vector3.up * 1.25f, new Vector3(minHorizontalSpan, minVerticalSpan, 1f));
        }

        bounds.Encapsulate(new Vector3(0f, 0f, 0f));
        bounds.Encapsulate(new Vector3(0f, 2.5f, 2.5f));
        return bounds;
    }

    private static void IncludeTarget(Transform target, ref Bounds bounds, ref bool hasBounds)
    {
        if (target == null)
        {
            return;
        }

        var renderers = target.GetComponentsInChildren<Renderer>(true);
        foreach (var targetRenderer in renderers)
        {
            if (!hasBounds)
            {
                bounds = targetRenderer.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(targetRenderer.bounds);
            }
        }

        if (!hasBounds)
        {
            bounds = new Bounds(target.position, Vector3.one);
            hasBounds = true;
        }
    }

    private void ExpandUntilBoundsFit(Bounds bounds)
    {
        var viewportPadding = GetViewportPadding();

        for (var i = 0; i < 12; i++)
        {
            if (BoundsFitViewport(bounds, viewportPadding))
            {
                return;
            }

            targetCamera.orthographicSize *= 1.08f;
        }
    }

    private bool BoundsFitViewport(Bounds bounds, Vector4 padding)
    {
        var min = bounds.min - (Vector3.one * worldPadding);
        var max = bounds.max + (Vector3.one * worldPadding);

        for (var x = 0; x < 2; x++)
        {
            for (var y = 0; y < 2; y++)
            {
                for (var z = 0; z < 2; z++)
                {
                    var point = new Vector3(
                        x == 0 ? min.x : max.x,
                        y == 0 ? min.y : max.y,
                        z == 0 ? min.z : max.z);
                    var viewport = targetCamera.WorldToViewportPoint(point);

                    if (viewport.z <= 0f
                        || viewport.x < padding.x
                        || viewport.x > 1f - padding.z
                        || viewport.y < padding.y
                        || viewport.y > 1f - padding.w)
                    {
                        return false;
                    }
                }
            }
        }

        return true;
    }

    private static Vector4 GetViewportPadding()
    {
        const float baseHorizontalPadding = 0.05f;
        const float baseBottomPadding = 0.06f;
        const float baseTopPadding = 0.08f;

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            return new Vector4(baseHorizontalPadding, baseBottomPadding, baseHorizontalPadding, baseTopPadding);
        }

        var safeArea = Screen.safeArea;
        var leftInset = Mathf.Clamp01(safeArea.xMin / Screen.width);
        var rightInset = Mathf.Clamp01((Screen.width - safeArea.xMax) / Screen.width);
        var bottomInset = Mathf.Clamp01(safeArea.yMin / Screen.height);
        var topInset = Mathf.Clamp01((Screen.height - safeArea.yMax) / Screen.height);

        return new Vector4(
            Mathf.Min(0.25f, baseHorizontalPadding + leftInset),
            Mathf.Min(0.25f, baseBottomPadding + bottomInset),
            Mathf.Min(0.25f, baseHorizontalPadding + rightInset),
            Mathf.Min(0.3f, baseTopPadding + topInset));
    }

    private void ApplyEditorPreview()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (targetCamera == null)
        {
            return;
        }

        transform.SetPositionAndRotation(portraitPosition, Quaternion.Euler(portraitEulerAngles));
        targetCamera.orthographic = true;

#if UNITY_EDITOR
        if (!EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.SetDirty(this);
        }
#endif
    }

    private static void ApplyPortraitLock()
    {
        Screen.autorotateToPortrait = true;
        Screen.autorotateToPortraitUpsideDown = false;
        Screen.autorotateToLandscapeLeft = false;
        Screen.autorotateToLandscapeRight = false;
        Screen.orientation = ScreenOrientation.Portrait;
    }
}
