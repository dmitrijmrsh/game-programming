using UnityEngine;

public class UnderwaterAmbient : MonoBehaviour
{
    [SerializeField] Transform followTarget;
    [SerializeField] ParticleSystem dustParticles;
    [SerializeField] ParticleSystem bubbleParticles;

    void Update()
    {
        if (followTarget != null)
            transform.position = followTarget.position;
    }

    public void SetIntensity(float normalizedDepth)
    {
        if (dustParticles != null)
        {
            var emission = dustParticles.emission;
            emission.rateOverTime = Mathf.Lerp(20f, 80f, normalizedDepth);

            var main = dustParticles.main;
            Color dustColor = Color.Lerp(
                new Color(0.8f, 0.9f, 1f, 0.3f),
                new Color(0.2f, 0.3f, 0.5f, 0.15f),
                normalizedDepth);
            main.startColor = dustColor;
        }
    }
}
