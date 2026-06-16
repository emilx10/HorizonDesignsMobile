using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

[DisallowMultipleComponent]
[RequireComponent(typeof(Collider))]
public sealed class D6DiceRoller : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private LayerMask hittableLayers = ~0;
    [SerializeField] private bool inputEnabled = true;

    [Header("Roll")]
    [SerializeField, Min(0.2f)] private float rollDuration = 1.15f;
    [SerializeField, Min(0f)] private float swingHeight = 0.45f;
    [SerializeField, Min(1)] private int extraSpinTurns = 3;
    [SerializeField] private AnimationCurve rollEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public event Action<int> RollFinished;
    public event Action<int> ValueChanged;

    public int CurrentValue { get; private set; } = 1;
    public int FrontValue { get; private set; } = 1;
    public bool IsRolling { get; private set; }
    public bool InputEnabled => inputEnabled;

    private Coroutine activeRoll;
    private Vector3 homePosition;
    private Vector3 homeScale;
    private Quaternion homeRotation;
    private D6DiceVisual diceVisual;
    private readonly List<RaycastResult> uiRaycastResults = new();

    private static readonly Vector3[] NumberEulerAngles =
    {
        Vector3.zero, // 1 on top
        new Vector3(-90f, 0f, 0f), // 2 on top
        new Vector3(0f, 0f, 90f), // 3 on top
        new Vector3(0f, 0f, -90f), // 4 on top
        new Vector3(90f, 0f, 0f), // 5 on top
        new Vector3(180f, 0f, 0f), // 6 on top
    };

    private void Awake()
    {
        homePosition = transform.position;
        homeScale = transform.localScale;
        homeRotation = transform.rotation;
        diceVisual = GetComponent<D6DiceVisual>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (diceVisual != null && diceVisual.UsesFlatSpriteBody)
        {
            CurrentValue = UnityEngine.Random.Range(1, 7);
            FrontValue = GetRandomAdjacentValue(CurrentValue);
            diceVisual.SetVisiblePips(CurrentValue, FrontValue);
            ValueChanged?.Invoke(CurrentValue);
        }
    }

    private void Update()
    {
        if (!inputEnabled || IsRolling)
        {
            return;
        }

#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            TryRollAt(Mouse.current.position.ReadValue());
            if (IsRolling)
            {
                return;
            }
        }

        var touchscreen = Touchscreen.current;
        if (touchscreen == null)
        {
            return;
        }

        foreach (var touch in touchscreen.touches)
        {
            if (touch.press.wasPressedThisFrame)
            {
                TryRollAt(touch.position.ReadValue());
                break;
            }
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            TryRollAt(Input.mousePosition);
            if (IsRolling)
            {
                return;
            }
        }

        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
            {
                TryRollAt(touch.position);
                break;
            }
        }
#endif
    }

    public void Roll()
    {
        if (!inputEnabled || IsRolling)
        {
            return;
        }

        var result = UnityEngine.Random.Range(1, 7);
        RollTo(result);
    }

    public void RollTo(int value)
    {
        if (!inputEnabled)
        {
            return;
        }

        value = Mathf.Clamp(value, 1, 6);

        if (activeRoll != null)
        {
            StopCoroutine(activeRoll);
        }

        activeRoll = StartCoroutine(RollRoutine(value));
    }

    public void SetInputEnabled(bool enabled)
    {
        inputEnabled = enabled;
    }

    private void TryRollAt(Vector2 screenPosition)
    {
        if (IsScreenPositionOverUi(screenPosition))
        {
            return;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return;
        }

        var ray = targetCamera.ScreenPointToRay(screenPosition);
        if (Physics.Raycast(ray, out var hit, 100f, hittableLayers, QueryTriggerInteraction.Ignore)
            && hit.transform.IsChildOf(transform))
        {
            Roll();
        }
    }

    private bool IsScreenPositionOverUi(Vector2 screenPosition)
    {
        if (EventSystem.current == null)
        {
            return false;
        }

        var eventData = new PointerEventData(EventSystem.current)
        {
            position = screenPosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, uiRaycastResults);
        return uiRaycastResults.Count > 0;
    }

    private IEnumerator RollRoutine(int targetValue)
    {
        IsRolling = true;

        if (diceVisual != null && diceVisual.UsesFlatSpriteBody)
        {
            yield return FlatRollRoutine(targetValue);
            yield break;
        }

        var startEuler = transform.eulerAngles;
        var targetEuler = NumberEulerAngles[targetValue - 1];
        targetEuler.y = GetFinalYaw(targetValue);
        targetEuler = AlignFinalOrientation(targetValue, targetEuler);

        var animatedEndEuler = targetEuler + new Vector3(
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f,
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f,
            UnityEngine.Random.Range(extraSpinTurns, extraSpinTurns + 3) * 360f);

        var startPosition = transform.position;
        var time = 0f;

        while (time < rollDuration)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / rollDuration);
            var eased = rollEase.Evaluate(t);

            transform.position = Vector3.Lerp(startPosition, homePosition, eased)
                                 + Vector3.up * (Mathf.Sin(t * Mathf.PI) * swingHeight);
            transform.rotation = Quaternion.Euler(Vector3.LerpUnclamped(startEuler, animatedEndEuler, eased));

            yield return null;
        }

        transform.position = homePosition;
        transform.rotation = Quaternion.Euler(targetEuler);
        CurrentValue = targetValue;
        FrontValue = GetFrontFacingValue(CurrentValue);
        ValueChanged?.Invoke(CurrentValue);
        IsRolling = false;
        activeRoll = null;
        RollFinished?.Invoke(CurrentValue);
    }

    private IEnumerator FlatRollRoutine(int targetTopValue)
    {
        var startPosition = transform.position;
        var startScale = transform.localScale;
        var time = 0f;

        while (time < rollDuration)
        {
            time += Time.deltaTime;
            var t = Mathf.Clamp01(time / rollDuration);
            var eased = rollEase.Evaluate(t);
            var wobble = Mathf.Sin(t * Mathf.PI * Mathf.Max(1, extraSpinTurns) * 2f);

            transform.position = Vector3.Lerp(startPosition, homePosition, eased)
                                 + Vector3.up * (Mathf.Sin(t * Mathf.PI) * swingHeight);
            transform.localScale = startScale * (1f + (Mathf.Sin(t * Mathf.PI) * 0.12f));
            transform.rotation = homeRotation * Quaternion.Euler(0f, 0f, wobble * 14f);

            yield return null;
        }

        CurrentValue = Mathf.Clamp(targetTopValue, 1, 6);
        FrontValue = GetRandomAdjacentValue(CurrentValue);
        transform.position = homePosition;
        transform.localScale = homeScale;
        transform.rotation = homeRotation;
        diceVisual.SetVisiblePips(CurrentValue, FrontValue);
        ValueChanged?.Invoke(CurrentValue);
        IsRolling = false;
        activeRoll = null;
        RollFinished?.Invoke(CurrentValue);
    }

    private Vector3 AlignFinalOrientation(int targetValue, Vector3 targetEuler)
    {
        if (targetValue != 6)
        {
            return targetEuler;
        }

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return targetEuler;
        }

        var bestEuler = targetEuler;
        var bestVerticalScore = float.NegativeInfinity;

        for (var yaw = 0; yaw < 360; yaw += 90)
        {
            var candidateEuler = targetEuler;
            candidateEuler.y = yaw;

            var candidateRotation = Quaternion.Euler(candidateEuler);
            var center = transform.position;
            var sixColumnDirection = candidateRotation * Vector3.forward;
            var screenCenter = targetCamera.WorldToScreenPoint(center);
            var screenOffset = targetCamera.WorldToScreenPoint(center + sixColumnDirection) - screenCenter;
            var verticalScore = Mathf.Abs(screenOffset.y) - Mathf.Abs(screenOffset.x);

            if (verticalScore > bestVerticalScore)
            {
                bestVerticalScore = verticalScore;
                bestEuler = candidateEuler;
            }
        }

        return bestEuler;
    }

    private static float GetFinalYaw(int targetValue)
    {
        if (targetValue == 6)
        {
            return 0f;
        }

        return UnityEngine.Random.Range(0, 4) * 90f;
    }

    private static int GetRandomAdjacentValue(int value)
    {
        value = Mathf.Clamp(value, 1, 6);
        var opposite = GetOppositeValue(value);
        var choice = UnityEngine.Random.Range(1, 5);

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

    private int GetFrontFacingValue(int fallback)
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return fallback;
        }

        var directionToCamera = (targetCamera.transform.position - transform.position).normalized;
        var bestValue = fallback;
        var bestDot = float.NegativeInfinity;

        CheckFace(Vector3.up, 1, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.down, 6, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.forward, 2, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.back, 5, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.right, 3, directionToCamera, ref bestValue, ref bestDot);
        CheckFace(Vector3.left, 4, directionToCamera, ref bestValue, ref bestDot);

        return bestValue;
    }

    private void CheckFace(Vector3 localNormal, int value, Vector3 directionToCamera, ref int bestValue, ref float bestDot)
    {
        var worldNormal = transform.TransformDirection(localNormal);
        var dot = Vector3.Dot(worldNormal, directionToCamera);

        if (dot > bestDot)
        {
            bestDot = dot;
            bestValue = value;
        }
    }
}
