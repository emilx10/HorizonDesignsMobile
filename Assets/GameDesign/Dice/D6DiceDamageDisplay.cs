using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceDamage))]
public sealed class D6DiceDamageDisplay : MonoBehaviour
{
    [SerializeField] private D6DiceDamage damageSource;
    [SerializeField] private D6DiceRoller diceRoller;
    [SerializeField] private Camera targetCamera;
    [SerializeField] private Text damageText;
    [SerializeField] private Vector3 worldOffset = new(0f, 0.9f, 0f);
    [SerializeField] private string labelPrefix = "DMG";

    private void Awake()
    {
        if (damageSource == null)
        {
            damageSource = GetComponent<D6DiceDamage>();
        }

        if (diceRoller == null)
        {
            diceRoller = GetComponent<D6DiceRoller>();
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (damageText == null)
        {
            damageText = CreateDamageText();
        }

        Refresh();
    }

    private void OnEnable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished += HandleRollFinished;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (diceRoller != null)
        {
            diceRoller.RollFinished -= HandleRollFinished;
        }
    }

    private void LateUpdate()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (damageText == null || targetCamera == null)
        {
            return;
        }

        damageText.rectTransform.position = targetCamera.WorldToScreenPoint(transform.position + worldOffset);
    }

    public void Refresh()
    {
        if (damageText == null || damageSource == null)
        {
            return;
        }

        damageText.text = $"{labelPrefix} {damageSource.DiceDamage}";
    }

    private void HandleRollFinished(int _)
    {
        Refresh();
    }

    private static Text CreateDamageText()
    {
        var canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var canvasObject = new GameObject("Dice UI Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;
        }

        var textObject = new GameObject("Dice Damage Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(canvas.transform, false);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        var rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(260f, 72f);

        return text;
    }
}
