using UnityEngine;
using UnityEngine.InputSystem;

public class LightBeam : MonoBehaviour
{
    [SerializeField] float beamRange = 50f;
    [SerializeField] float beamAngle = 70f;
    [SerializeField] Light spotLight;
    [SerializeField] FlockingManager flockingManager;
    [SerializeField] ParticleSystem beamParticles;
    [SerializeField] LineRenderer beamLine;

    bool isActive;

    public bool IsActive => isActive;

    void Start()
    {
        SetBeamActive(false);
    }

    public void OnLightBeam(InputValue value)
    {
        isActive = !isActive;
        SetBeamActive(isActive);
    }

    void Update()
    {
        if (!isActive) return;

        if (beamLine != null && spotLight != null)
        {
            Transform origin = spotLight.transform;
            beamLine.SetPosition(0, origin.position);
            beamLine.SetPosition(1, origin.position + origin.forward * beamRange);
        }
    }

    void SetBeamActive(bool active)
    {
        isActive = active;

        if (spotLight != null)
        {
            spotLight.enabled = active;
            spotLight.range = beamRange;
            spotLight.spotAngle = beamAngle;
            spotLight.color = new Color(0.5f, 0.9f, 1f);
            spotLight.intensity = 25f;
        }

        if (beamParticles != null)
        {
            if (active) beamParticles.Play();
            else beamParticles.Stop();
        }

        if (beamLine != null)
            beamLine.enabled = active;

        if (flockingManager != null)
        {
            flockingManager.AttractionRange = beamRange;
            flockingManager.AttractionAngle = beamAngle;
            flockingManager.SetAttractTarget(spotLight != null ? spotLight.transform : transform, active);
        }
    }
}
