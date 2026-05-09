using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(D6DiceRoller))]
public sealed class D6DiceDamage : MonoBehaviour
{
    [SerializeField, Min(0)] private int defaultDamage = 1;
    [SerializeField] private Camera targetCamera;

    private D6DiceRoller diceRoller;

    public int DefaultDamage => defaultDamage;
    public int DiceValue => GetFrontFacingValue();
    public int DiceDamage => DiceValue * defaultDamage;

    private void Awake()
    {
        diceRoller = GetComponent<D6DiceRoller>();

        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }
    }

    public int CalculateDamage()
    {
        return DiceDamage;
    }

    public void AddDefaultDamage(int amount)
    {
        defaultDamage = Mathf.Max(0, defaultDamage + amount);
    }

    private int GetFrontFacingValue()
    {
        if (targetCamera == null)
        {
            targetCamera = Camera.main;
        }

        if (targetCamera == null)
        {
            return diceRoller != null ? diceRoller.CurrentValue : 1;
        }

        var directionToCamera = (targetCamera.transform.position - transform.position).normalized;

        var bestValue = 1;
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
