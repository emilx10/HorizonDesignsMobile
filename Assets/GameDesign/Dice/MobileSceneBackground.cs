using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public sealed class MobileSceneBackground : MonoBehaviour
{
    [SerializeField] private string resourcePath = "MobileUI/UI/Scene Background";
    [SerializeField, Min(1f)] private float distanceFromCamera = 80f;
    [SerializeField, Min(1f)] private float coverScale = 1.08f;
    [SerializeField] private Vector2 viewportOffset = new(0f, -0.02f);
    [SerializeField] private int sortingOrder = -1000;

    private const string BackgroundObjectName = "Generated Scene Background";

    private Camera targetCamera;
    private SpriteRenderer spriteRenderer;
    private Sprite backgroundSprite;
    private int lastScreenWidth;
    private int lastScreenHeight;
    private float lastOrthographicSize;
    private float lastAspect;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateForScene()
    {
        var mainCamera = Camera.main;
        if (mainCamera == null || mainCamera.GetComponent<MobileSceneBackground>() != null)
        {
            return;
        }

        mainCamera.gameObject.AddComponent<MobileSceneBackground>();
    }

    private void Awake()
    {
        targetCamera = GetComponent<Camera>();
        EnsureBackground();
        FitToCamera();
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = GetComponent<Camera>();
        }

        if (Screen.width != lastScreenWidth
            || Screen.height != lastScreenHeight
            || !Mathf.Approximately(targetCamera.orthographicSize, lastOrthographicSize)
            || !Mathf.Approximately(targetCamera.aspect, lastAspect))
        {
            FitToCamera();
        }
    }

    private void EnsureBackground()
    {
        if (backgroundSprite == null)
        {
            backgroundSprite = Resources.Load<Sprite>(resourcePath);
        }

        var backgroundTransform = transform.Find(BackgroundObjectName);
        if (backgroundTransform == null)
        {
            var backgroundObject = new GameObject(BackgroundObjectName, typeof(SpriteRenderer));
            backgroundTransform = backgroundObject.transform;
            backgroundTransform.SetParent(transform, false);
        }

        spriteRenderer = backgroundTransform.GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            spriteRenderer = backgroundTransform.gameObject.AddComponent<SpriteRenderer>();
        }

        spriteRenderer.sprite = backgroundSprite;
        spriteRenderer.sortingOrder = sortingOrder;
        spriteRenderer.flipX = false;
        spriteRenderer.flipY = false;
    }

    private void FitToCamera()
    {
        EnsureBackground();

        if (targetCamera == null || spriteRenderer == null || backgroundSprite == null)
        {
            return;
        }

        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        lastOrthographicSize = targetCamera.orthographicSize;
        lastAspect = targetCamera.aspect;

        var backgroundTransform = spriteRenderer.transform;
        backgroundTransform.localRotation = Quaternion.identity;
        backgroundTransform.localPosition = new Vector3(
            viewportOffset.x * targetCamera.orthographicSize * targetCamera.aspect * 2f,
            viewportOffset.y * targetCamera.orthographicSize * 2f,
            distanceFromCamera);

        var spriteSize = backgroundSprite.bounds.size;
        if (spriteSize.x <= 0f || spriteSize.y <= 0f)
        {
            return;
        }

        var viewHeight = targetCamera.orthographic
            ? targetCamera.orthographicSize * 2f
            : 2f * distanceFromCamera * Mathf.Tan(targetCamera.fieldOfView * 0.5f * Mathf.Deg2Rad);
        var viewWidth = viewHeight * targetCamera.aspect;
        var scale = Mathf.Max(viewWidth / spriteSize.x, viewHeight / spriteSize.y) * coverScale;
        backgroundTransform.localScale = new Vector3(scale, scale, scale);
    }
}
