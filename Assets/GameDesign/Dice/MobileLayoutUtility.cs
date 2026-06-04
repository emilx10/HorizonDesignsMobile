using UnityEngine;
using UnityEngine.UI;

public static class MobileLayoutUtility
{
    private const string CanvasName = "Dice UI Canvas";
    private const string SafeAreaName = "Safe Area";

    private static Rect lastSafeArea;
    private static Vector2Int lastScreenSize;

    public static Canvas EnsureCanvas()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject(CanvasName, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler == null)
        {
            scaler = canvas.gameObject.AddComponent<CanvasScaler>();
        }

        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0f;

        if (canvas.GetComponent<GraphicRaycaster>() == null)
        {
            canvas.gameObject.AddComponent<GraphicRaycaster>();
        }

        EnsureSafeAreaRoot(canvas);
        return canvas;
    }

    public static RectTransform EnsureSafeAreaRoot(Canvas canvas)
    {
        var safeArea = canvas.transform.Find(SafeAreaName) as RectTransform;
        if (safeArea == null)
        {
            var safeAreaObject = new GameObject(SafeAreaName, typeof(RectTransform));
            safeArea = safeAreaObject.GetComponent<RectTransform>();
            safeArea.SetParent(canvas.transform, false);
        }

        ApplySafeArea(safeArea);
        return safeArea;
    }

    public static RectTransform GetUiRoot()
    {
        return EnsureSafeAreaRoot(EnsureCanvas());
    }

    public static void RefreshSafeArea()
    {
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            ApplySafeArea(EnsureSafeAreaRoot(canvas));
        }
    }

    public static void ConfigureText(Text text, int maxFontSize, int minFontSize = 18)
    {
        text.resizeTextForBestFit = true;
        text.resizeTextMinSize = minFontSize;
        text.resizeTextMaxSize = maxFontSize;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
    }

    public static Vector3 ClampToScreen(Vector3 screenPosition, Vector2 sizeDelta, float padding = 8f)
    {
        var halfWidth = Mathf.Max(0f, sizeDelta.x * 0.5f);
        var halfHeight = Mathf.Max(0f, sizeDelta.y * 0.5f);
        var safeArea = Screen.safeArea;

        if (safeArea.width <= 0f || safeArea.height <= 0f)
        {
            safeArea = new Rect(0f, 0f, Screen.width, Screen.height);
        }

        screenPosition.x = Mathf.Clamp(screenPosition.x, safeArea.xMin + halfWidth + padding, safeArea.xMax - halfWidth - padding);
        screenPosition.y = Mathf.Clamp(screenPosition.y, safeArea.yMin + halfHeight + padding, safeArea.yMax - halfHeight - padding);
        return screenPosition;
    }

    private static void ApplySafeArea(RectTransform safeArea)
    {
        var currentSafeArea = Screen.safeArea;
        var currentScreenSize = new Vector2Int(Screen.width, Screen.height);

        lastSafeArea = currentSafeArea;
        lastScreenSize = currentScreenSize;

        if (Screen.width <= 0 || Screen.height <= 0)
        {
            safeArea.anchorMin = Vector2.zero;
            safeArea.anchorMax = Vector2.one;
            safeArea.offsetMin = Vector2.zero;
            safeArea.offsetMax = Vector2.zero;
            return;
        }

        var anchorMin = currentSafeArea.position;
        var anchorMax = currentSafeArea.position + currentSafeArea.size;
        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        safeArea.anchorMin = anchorMin;
        safeArea.anchorMax = anchorMax;
        safeArea.offsetMin = Vector2.zero;
        safeArea.offsetMax = Vector2.zero;
    }
}
