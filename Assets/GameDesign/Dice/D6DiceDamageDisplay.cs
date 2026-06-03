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

        var rectTransform = damageText.rectTransform;
        var screenPosition = targetCamera.WorldToScreenPoint(transform.position + worldOffset);
        rectTransform.position = MobileLayoutUtility.ClampToScreen(screenPosition, rectTransform.sizeDelta);
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
        var uiRoot = MobileLayoutUtility.GetUiRoot();

        var textObject = new GameObject("Dice Damage Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(uiRoot, false);

        var text = textObject.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 42;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        MobileLayoutUtility.ConfigureText(text, 42, 22);

        var rectTransform = text.rectTransform;
        rectTransform.sizeDelta = new Vector2(260f, 72f);

        return text;
    }
}
