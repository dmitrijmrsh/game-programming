using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class CurrentZoneVFXBinder : MonoBehaviour
{
    [System.Serializable]
    public struct CurrentZoneVFXProfile
    {
        public Vector3 FlowDirection;
        public float FlowStrength;
        public Vector3 ZoneCenter;
        public Vector3 ZoneSize;
        public float DepthFactor;
        public float Turbulence;
        public Vector4 Tint;
    }

    [Header("Exposed Properties")]
    [SerializeField] string flowDirectionProperty = "FlowDirection";
    [SerializeField] string flowStrengthProperty = "FlowStrength";
    [SerializeField] string zoneCenterProperty = "ZoneCenter";
    [SerializeField] string zoneSizeProperty = "ZoneSize";
    [SerializeField] string depthFactorProperty = "DepthFactor";
    [SerializeField] string turbulenceProperty = "Turbulence";
    [SerializeField] string tintProperty = "CurrentTint";

    [Header("Lane Placement")]
    [SerializeField] Vector3 laneOffsetNormalized = Vector3.zero;
    [SerializeField] Vector3 laneScaleMultiplier = Vector3.one;
    [SerializeField] float playRateMultiplier = 1f;
    [SerializeField] float transformScaleFactor = 0.12f;
    [SerializeField] float depthScaleBoost = 0.35f;

    CurrentZone zone;
    VisualEffect visualEffect;

    void Awake()
    {
        zone = GetComponentInParent<CurrentZone>();
        visualEffect = GetComponent<VisualEffect>();

        if (visualEffect != null)
        {
            visualEffect.resetSeedOnPlay = false;
            visualEffect.startSeed = (uint)Mathf.Abs($"{BuildHierarchyPath(transform)}:{name}".GetHashCode());
        }
    }

    void OnEnable()
    {
        if (visualEffect == null)
            visualEffect = GetComponent<VisualEffect>();

        visualEffect?.Play();
    }

    void Update()
    {
        if (zone == null || visualEffect == null)
            return;

        var profile = BuildProfile(zone, Time.time);
        UpdateLaneTransform(profile);
        ApplyProfile(visualEffect, profile);
    }

    public static CurrentZoneVFXProfile BuildProfile(CurrentZone zone)
    {
        return BuildProfile(zone, Time.time);
    }

    public static CurrentZoneVFXProfile BuildProfile(CurrentZone zone, float sampleTime)
    {
        Color tint = zone.GetCurrentTint();
        Vector3 direction = zone.GetVisualDirection();
        float strength = zone.GetVisualStrength();

        if (CurrentField.Instance != null)
        {
            Vector3 runtimeCurrent = CurrentField.Instance.SampleCurrent(zone.GetWorldCenter(), sampleTime);
            float runtimeMagnitude = runtimeCurrent.magnitude;
            if (runtimeMagnitude > 0.01f)
            {
                direction = runtimeCurrent / runtimeMagnitude;
                strength = runtimeMagnitude;
            }
        }

        return new CurrentZoneVFXProfile
        {
            FlowDirection = direction,
            FlowStrength = strength,
            ZoneCenter = zone.GetWorldCenter(),
            ZoneSize = zone.GetWorldSize(),
            DepthFactor = zone.GetDepthFactor(),
            Turbulence = zone.GetTurbulence(),
            Tint = new Vector4(tint.r, tint.g, tint.b, tint.a)
        };
    }

    void ApplyProfile(VisualEffect target, CurrentZoneVFXProfile profile)
    {
        target.playRate = Mathf.Clamp((0.65f + profile.FlowStrength * 0.08f) * playRateMultiplier, 0.55f, 3.5f);

        if (target.HasVector3(flowDirectionProperty))
            target.SetVector3(flowDirectionProperty, profile.FlowDirection);

        if (target.HasFloat(flowStrengthProperty))
            target.SetFloat(flowStrengthProperty, profile.FlowStrength);

        if (target.HasVector3(zoneCenterProperty))
            target.SetVector3(zoneCenterProperty, profile.ZoneCenter);

        if (target.HasVector3(zoneSizeProperty))
            target.SetVector3(zoneSizeProperty, profile.ZoneSize);

        if (target.HasFloat(depthFactorProperty))
            target.SetFloat(depthFactorProperty, profile.DepthFactor);

        if (target.HasFloat(turbulenceProperty))
            target.SetFloat(turbulenceProperty, profile.Turbulence);

        if (target.HasVector4(tintProperty))
            target.SetVector4(tintProperty, profile.Tint);
    }

    void UpdateLaneTransform(CurrentZoneVFXProfile profile)
    {
        if (zone == null)
            return;

        transform.localPosition = Vector3.Scale(profile.ZoneSize * 0.5f, laneOffsetNormalized);

        Vector3 direction = profile.FlowDirection.sqrMagnitude > 0.0001f
            ? profile.FlowDirection.normalized
            : Vector3.forward;

        Vector3 up = Mathf.Abs(Vector3.Dot(direction, Vector3.up)) > 0.92f
            ? zone.transform.right
            : Vector3.up;

        transform.rotation = Quaternion.LookRotation(direction, up);

        float depthBoost = Mathf.Lerp(1f, 1f + depthScaleBoost, profile.DepthFactor);
        Vector3 scaledSize = Vector3.Scale(profile.ZoneSize, laneScaleMultiplier) * (transformScaleFactor * depthBoost);
        scaledSize.x = Mathf.Max(scaledSize.x, 0.9f);
        scaledSize.y = Mathf.Max(scaledSize.y, 0.9f);
        scaledSize.z = Mathf.Max(scaledSize.z, 1.25f);
        transform.localScale = scaledSize;
    }

    static string BuildHierarchyPath(Transform current)
    {
        string path = current.name;
        while (current.parent != null)
        {
            current = current.parent;
            path = $"{current.name}/{path}";
        }

        return path;
    }
}
