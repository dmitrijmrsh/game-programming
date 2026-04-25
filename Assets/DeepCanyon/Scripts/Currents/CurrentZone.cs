using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class CurrentZone : MonoBehaviour
{
    public enum CurrentZoneRole
    {
        Neutral,
        Assist,
        Hazard
    }

    [SerializeField] Vector3 flowDirection = Vector3.right;
    [SerializeField] float strength = 10f;
    [SerializeField] float edgeFalloff = 2f;
    [SerializeField] float turbulence = 0.35f;
    [SerializeField] float visualIntensity = 1f;
    [SerializeField] Color currentTint = new(0.45f, 0.85f, 1f, 1f);
    [Header("Tactical Role")]
    [SerializeField] CurrentZoneRole zoneRole = CurrentZoneRole.Neutral;
    [SerializeField] float roleBonusMultiplier = 1.4f;
    [SerializeField] float armorerMitigation = 0.45f;

    BoxCollider zone;

    void Awake() => zone = GetComponent<BoxCollider>();

    void OnEnable()
    {
        if (CurrentField.Instance != null)
            CurrentField.Instance.RegisterZone(this);
    }

    void OnDisable()
    {
        if (CurrentField.Instance != null)
            CurrentField.Instance.UnregisterZone(this);
    }

    public Vector3 GetForceAt(Vector3 worldPos)
    {
        return GetForceAt(worldPos, false, false);
    }

    public Vector3 GetForceAt(Vector3 worldPos, bool hasSpeederBuff, bool hasArmorerBuff)
    {
        Vector3 local = transform.InverseTransformPoint(worldPos);
        Vector3 half = GetLocalHalfSize();

        if (Mathf.Abs(local.x) > half.x || Mathf.Abs(local.y) > half.y || Mathf.Abs(local.z) > half.z)
            return Vector3.zero;

        float dx = 1f - Mathf.Abs(local.x) / half.x;
        float dy = 1f - Mathf.Abs(local.y) / half.y;
        float dz = 1f - Mathf.Abs(local.z) / half.z;
        float fade = Mathf.Pow(Mathf.Min(dx, dy, dz), edgeFalloff);
        float tacticalMultiplier = 1f;

        if (zoneRole == CurrentZoneRole.Assist && hasSpeederBuff)
            tacticalMultiplier *= roleBonusMultiplier;
        else if (zoneRole == CurrentZoneRole.Hazard)
            tacticalMultiplier *= hasArmorerBuff ? armorerMitigation : roleBonusMultiplier;

        return transform.TransformDirection(flowDirection.normalized) * strength * fade * tacticalMultiplier;
    }

    public Vector3 GetVisualDirection()
    {
        return transform.TransformDirection(flowDirection.normalized);
    }

    public float GetDepthFactor(float maxDepth = 250f)
    {
        return Mathf.Clamp01(Mathf.Abs(transform.position.y) / maxDepth);
    }

    public Vector3 GetWorldCenter()
    {
        EnsureZone();
        return transform.TransformPoint(zone.center);
    }

    public Vector3 GetWorldSize()
    {
        EnsureZone();
        return Vector3.Scale(zone.size, transform.lossyScale);
    }

    public float GetVisualStrength()
    {
        float roleVisualBoost = zoneRole == CurrentZoneRole.Hazard ? roleBonusMultiplier : 1f;
        return strength * visualIntensity * roleVisualBoost;
    }

    public float GetTurbulence()
    {
        return turbulence;
    }

    public Color GetCurrentTint()
    {
        Color roleTint = zoneRole switch
        {
            CurrentZoneRole.Assist => Color.Lerp(currentTint, new Color(0.55f, 1f, 0.8f, 1f), 0.55f),
            CurrentZoneRole.Hazard => Color.Lerp(currentTint, new Color(1f, 0.4f, 0.35f, 1f), 0.7f),
            _ => currentTint
        };

        return Color.Lerp(roleTint, Color.white, GetDepthFactor() * 0.35f);
    }

    Vector3 GetLocalHalfSize()
    {
        EnsureZone();
        return zone.size * 0.5f;
    }

    void EnsureZone()
    {
        if (zone == null)
            zone = GetComponent<BoxCollider>();
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null) return;

        Gizmos.color = new Color(0f, 0.8f, 1f, 0.2f);
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);

        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(col.center, flowDirection.normalized * 3f);
    }
}
