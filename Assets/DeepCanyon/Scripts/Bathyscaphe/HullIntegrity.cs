using UnityEngine;
using System;

public class HullIntegrity : MonoBehaviour
{
    [SerializeField] float maxHull = 100f;
    [SerializeField] float depthDamageRate = 0.4f;
    [SerializeField] float safeDepth = 30f;
    [SerializeField] ParticleSystem[] leakEffects;
    [Header("Leak VFX")]
    [SerializeField] float leakMinRate = 12f;
    [SerializeField] float leakMaxRate = 70f;
    [SerializeField] float leakMinSpeed = 2.5f;
    [SerializeField] float leakMaxSpeed = 9f;
    [SerializeField] float leakMinSize = 0.06f;
    [SerializeField] float leakMaxSize = 0.14f;
    [SerializeField] float leakPressureWeight = 0.35f;

    float currentHull;
    DepthTracker depthTracker;
    float armorMultiplier = 1f;

    public float CurrentHull => currentHull;
    public float MaxHull => maxHull;
    public float HullPercent => currentHull / maxHull;
    public float ArmorMultiplier { get => armorMultiplier; set => armorMultiplier = value; }

    public event Action OnHullDestroyed;
    public event Action<float> OnHullChanged;

    void Awake()
    {
        currentHull = maxHull;
        depthTracker = GetComponent<DepthTracker>();
    }

    void Update()
    {
        ApplyPressureStep(Time.deltaTime);
    }

    void ApplyPressureStep(float deltaTime)
    {
        if (depthTracker == null || currentHull <= 0f) return;

        bool hullChanged = false;
        float depth = Mathf.Abs(depthTracker.CurrentDepth);
        if (depth > safeDepth && deltaTime > 0f)
        {
            float overDepth = depth - safeDepth;
            float damage = depthTracker.PressureCoefficient * depthDamageRate * armorMultiplier * deltaTime;
            damage *= overDepth / depthTracker.MaxDepth;
            float previousHull = currentHull;
            currentHull = Mathf.Max(0f, currentHull - damage);
            hullChanged = !Mathf.Approximately(previousHull, currentHull);
        }

        UpdateLeakEffects();

        if (hullChanged)
            OnHullChanged?.Invoke(HullPercent);

        if (currentHull <= 0f)
            OnHullDestroyed?.Invoke();
    }

    public void Repair(float amount)
    {
        currentHull = Mathf.Min(maxHull, currentHull + amount);
        OnHullChanged?.Invoke(HullPercent);
        UpdateLeakEffects();
    }

    void UpdateLeakEffects()
    {
        if (leakEffects == null) return;

        float percent = HullPercent;
        float damageSeverity = 1f - percent;
        float pressureSeverity = depthTracker != null ? depthTracker.NormalizedPressure : 0f;
        float leakSeverity = Mathf.Clamp01(damageSeverity + pressureSeverity * leakPressureWeight);

        for (int i = 0; i < leakEffects.Length; i++)
        {
            if (leakEffects[i] == null) continue;

            float threshold = i switch
            {
                0 => 0.75f,
                1 => 0.50f,
                2 => 0.50f,
                3 => 0.25f,
                _ => 0.25f
            };

            bool shouldPlay = percent < threshold;
            ConfigureLeak(leakEffects[i], leakSeverity, i);
            if (shouldPlay && !leakEffects[i].isPlaying)
                leakEffects[i].Play();
            else if (!shouldPlay && leakEffects[i].isPlaying)
                leakEffects[i].Stop();
        }
    }

    void ConfigureLeak(ParticleSystem leakEffect, float leakSeverity, int leakIndex)
    {
        float indexBoost = 1f + leakIndex * 0.12f;
        float tunedSeverity = Mathf.Clamp01(leakSeverity * indexBoost);

        var emission = leakEffect.emission;
        emission.rateOverTime = Mathf.Lerp(leakMinRate, leakMaxRate, tunedSeverity);

        var main = leakEffect.main;
        main.startSpeed = Mathf.Lerp(leakMinSpeed, leakMaxSpeed, tunedSeverity);
        main.startSize = Mathf.Lerp(leakMinSize, leakMaxSize, tunedSeverity);

        Color baseColor = new Color(0.7f, 0.9f, 1f, 0.45f);
        Color intenseColor = new Color(0.85f, 0.97f, 1f, 0.8f);
        main.startColor = Color.Lerp(baseColor, intenseColor, tunedSeverity);
    }
}
