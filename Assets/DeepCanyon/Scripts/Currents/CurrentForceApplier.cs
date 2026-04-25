using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CurrentForceApplier : MonoBehaviour
{
    [SerializeField] float forceMultiplier = 4f;

    Rigidbody rb;
    CreatureBuff creatureBuff;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        creatureBuff = GetComponent<CreatureBuff>();
    }

    void FixedUpdate()
    {
        if (CurrentField.Instance == null) return;

        bool hasSpeederBuff = creatureBuff != null && creatureBuff.SpeederActive;
        bool hasArmorerBuff = creatureBuff != null && creatureBuff.ArmorerActive;

        Vector3 current = SampleTacticalCurrent(Time.time, hasSpeederBuff, hasArmorerBuff);
        rb.AddForce(current * forceMultiplier, ForceMode.Force);
    }

    Vector3 SampleTacticalCurrent(float sampleTime, bool hasSpeederBuff, bool hasArmorerBuff)
    {
        Vector3 worldPos = transform.position;
        Vector3 baseCurrent = CurrentField.Instance != null
            ? CurrentField.Instance.SampleCurrent(worldPos, sampleTime)
            : Vector3.zero;

        var zones = Object.FindObjectsByType<CurrentZone>();
        for (int i = 0; i < zones.Length; i++)
        {
            Vector3 neutralZoneForce = zones[i].GetForceAt(worldPos);
            Vector3 tacticalZoneForce = zones[i].GetForceAt(worldPos, hasSpeederBuff, hasArmorerBuff);
            baseCurrent += tacticalZoneForce - neutralZoneForce;
        }

        return baseCurrent;
    }
}
