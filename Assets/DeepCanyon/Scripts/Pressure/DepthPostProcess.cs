using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DepthPostProcess : MonoBehaviour
{
    [SerializeField] Volume globalVolume;
    [SerializeField] DepthTracker depthTracker;

    [Header("Fog Colors")]
    [SerializeField] Color surfaceColor = new Color(0.4f, 0.75f, 1f, 1f);
    [SerializeField] Color midColor = new Color(0.1f, 0.3f, 0.65f, 1f);
    [SerializeField] Color deepColor = new Color(0.02f, 0.05f, 0.15f, 1f);

    [Header("Vignette")]
    [SerializeField] float minVignette = 0.1f;
    [SerializeField] float maxVignette = 0.5f;

    [Header("Bloom")]
    [SerializeField] float minBloom = 1.2f;
    [SerializeField] float maxBloom = 4f;

    ColorAdjustments colorAdj;
    Vignette vignette;
    Bloom bloom;
    ChromaticAberration chromatic;

    void Start()
    {
        if (globalVolume == null || depthTracker == null) return;
        globalVolume.profile.TryGet(out colorAdj);
        globalVolume.profile.TryGet(out vignette);
        globalVolume.profile.TryGet(out bloom);
        globalVolume.profile.TryGet(out chromatic);
    }

    void Update()
    {
        if (depthTracker == null) return;
        float t = depthTracker.NormalizedPressure;

        if (colorAdj != null)
        {
            Color fog = t < 0.5f
                ? Color.Lerp(surfaceColor, midColor, t * 2f)
                : Color.Lerp(midColor, deepColor, (t - 0.5f) * 2f);
            colorAdj.colorFilter.Override(fog);
            colorAdj.postExposure.Override(Mathf.Lerp(1.2f, -1f, t));
        }

        if (vignette != null)
            vignette.intensity.Override(Mathf.Lerp(minVignette, maxVignette, t));

        if (bloom != null)
            bloom.intensity.Override(Mathf.Lerp(minBloom, maxBloom, t));

        if (chromatic != null)
            chromatic.intensity.Override(t > 0.7f ? (t - 0.7f) / 0.3f * 0.3f : 0f);
    }
}
