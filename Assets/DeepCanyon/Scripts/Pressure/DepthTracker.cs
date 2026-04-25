using UnityEngine;

public class DepthTracker : MonoBehaviour
{
    [SerializeField] float surfaceY = 0f;
    [SerializeField] float maxDepth = 250f;
    [SerializeField] float depthPressureRate = 0.008f;

    public float CurrentDepth { get; private set; }
    public float NormalizedPressure { get; private set; }
    public float PressureCoefficient { get; private set; }

    public float MaxDepth => maxDepth;
    public float SurfaceY => surfaceY;

    void Update()
    {
        CurrentDepth = Mathf.Min(0f, transform.position.y - surfaceY);
        float absDepth = Mathf.Abs(CurrentDepth);
        NormalizedPressure = Mathf.Clamp01(absDepth / maxDepth);
        PressureCoefficient = 1f + absDepth * depthPressureRate;
    }
}
