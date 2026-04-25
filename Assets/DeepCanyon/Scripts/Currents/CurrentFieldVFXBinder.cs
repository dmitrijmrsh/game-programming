using UnityEngine;
using UnityEngine.VFX;

[RequireComponent(typeof(VisualEffect))]
public class CurrentFieldVFXBinder : MonoBehaviour
{
    [SerializeField] float sampleRadius = 5f;
    [SerializeField] string directionProperty = "CurrentDirection";
    [SerializeField] string strengthProperty = "CurrentStrength";

    VisualEffect vfx;

    void Awake() => vfx = GetComponent<VisualEffect>();

    void Update()
    {
        if (CurrentField.Instance == null || vfx == null) return;

        Vector3 current = CurrentField.Instance.SampleCurrent(transform.position, Time.time);
        float mag = current.magnitude;

        if (vfx.HasVector3(directionProperty))
            vfx.SetVector3(directionProperty, mag > 0.01f ? current / mag : Vector3.forward);

        if (vfx.HasFloat(strengthProperty))
            vfx.SetFloat(strengthProperty, mag);
    }
}
