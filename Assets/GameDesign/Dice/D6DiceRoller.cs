using System;
using System.Collections;
using UnityEngine;
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

    [Header("Roll")]
    [SerializeField, Min(0.2f)] private float rollDuration = 1.15f;
    [SerializeField, Min(0f)] private float swingHeight = 0.45f;
    [SerializeField, Min(1)] private int extraSpinTurns = 3;
    [SerializeField] private AnimationCurve rollEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public event Action<int> RollFinished;

    public int CurrentValue { get; private set; } = 1;
    public bool IsRolling { get; private set; }

    private Coroutine activeRoll;
    private Vector3 homePosition;

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

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (IsRolling)
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
        var result = UnityEngine.Random.Range(1, 7);
        RollTo(result);
    }

    public void RollTo(int value)
    {
        value = Mathf.Clamp(value, 1, 6);

        if (activeRoll != null)
        {
            StopCoroutine(activeRoll);
        }

        activeRoll = StartCoroutine(RollRoutine(value));
    }

    private void TryRollAt(Vector2 screenPosition)
    {
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

    private IEnumerator RollRoutine(int targetValue)
    {
        IsRolling = true;

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
}
