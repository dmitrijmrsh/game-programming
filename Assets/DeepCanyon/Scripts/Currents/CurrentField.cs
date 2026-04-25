using UnityEngine;
using System.Collections.Generic;

public class CurrentField : MonoBehaviour
{
    [Header("Noise Settings")]
    [SerializeField] float frequency = 0.02f;
    [SerializeField] float drift = 0.3f;
    [SerializeField] float baseStrength = 2f;

    [Header("Depth Influence")]
    [SerializeField] float depthStrengthMultiplier = 1.5f;
    [SerializeField] float maxDepthForStrength = 250f;

    static CurrentField instance;
    public static CurrentField Instance => instance;

    readonly List<CurrentZone> zones = new();

    void Awake()
    {
        instance = this;
        RebuildZoneRegistry();
    }

    void OnEnable()
    {
        instance = this;
        RebuildZoneRegistry();
    }

    public void RegisterZone(CurrentZone zone)
    {
        if (zone != null && !zones.Contains(zone))
            zones.Add(zone);
    }

    public void UnregisterZone(CurrentZone zone) => zones.Remove(zone);

    public Vector3 SampleCurrent(Vector3 worldPos, float time)
    {
        float nx = Mathf.PerlinNoise(worldPos.x * frequency + time * drift, worldPos.z * frequency + 100f);
        float ny = Mathf.PerlinNoise(worldPos.y * frequency + 200f, worldPos.x * frequency + time * drift);
        float nz = Mathf.PerlinNoise(worldPos.z * frequency + time * drift + 300f, worldPos.y * frequency);

        Vector3 flow = new Vector3(nx - 0.5f, (ny - 0.5f) * 0.3f, nz - 0.5f) * baseStrength;

        float depthFactor = Mathf.Clamp01(Mathf.Abs(worldPos.y) / maxDepthForStrength);
        flow *= 1f + depthFactor * depthStrengthMultiplier;

        for (int i = 0; i < zones.Count; i++)
            flow += zones[i].GetForceAt(worldPos);

        return flow;
    }

    public Vector3 SampleCurrentNormalized(Vector3 worldPos, float time)
    {
        Vector3 c = SampleCurrent(worldPos, time);
        float mag = c.magnitude;
        return mag > 0.01f ? c / mag : Vector3.zero;
    }

    public float SampleStrength(Vector3 worldPos, float time)
    {
        return SampleCurrent(worldPos, time).magnitude;
    }

    void RebuildZoneRegistry()
    {
        zones.Clear();
        var existingZones = Object.FindObjectsByType<CurrentZone>();
        for (int i = 0; i < existingZones.Length; i++)
            RegisterZone(existingZones[i]);
    }
}
