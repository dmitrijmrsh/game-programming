using UnityEngine;

public class EchoCrystal : MonoBehaviour
{
    [SerializeField] float rotationSpeed = 30f;
    [SerializeField] float bobAmplitude = 0.5f;
    [SerializeField] float bobFrequency = 1f;
    [SerializeField] float collectRadius = 4f;
    [SerializeField] Color glowColor = new Color(0.3f, 1f, 0.8f);
    [SerializeField] float glowIntensity = 5f;
    [SerializeField] ParticleSystem shimmerVFX;

    Light crystalLight;
    Vector3 startPos;
    bool collected;
    float phaseOffset;

    public bool Collected => collected;

    void Awake()
    {
        crystalLight = GetComponentInChildren<Light>();
        startPos = transform.position;
        phaseOffset = Random.value * Mathf.PI * 2f;

        if (crystalLight != null)
        {
            crystalLight.color = glowColor;
            crystalLight.intensity = glowIntensity;
            crystalLight.range = 15f;
        }

        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var mat = rend.material;
            mat.SetColor("_EmissionColor", glowColor * glowIntensity);
            mat.EnableKeyword("_EMISSION");
            mat.color = glowColor;
        }

        var col = gameObject.AddComponent<SphereCollider>();
        col.isTrigger = true;
        col.radius = collectRadius;
    }

    void Update()
    {
        if (collected) return;

        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        float bob = Mathf.Sin(Time.time * bobFrequency + phaseOffset) * bobAmplitude;
        transform.position = startPos + Vector3.up * bob;

        if (crystalLight != null)
            crystalLight.intensity = glowIntensity + Mathf.Sin(Time.time * 3f + phaseOffset) * 1.5f;
    }

    void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.GetComponent<BathyscapheController>() == null &&
            other.GetComponentInParent<BathyscapheController>() == null) return;

        Collect();
    }

    void Collect()
    {
        collected = true;
        GameManager.Instance?.CollectCrystal();

        if (shimmerVFX != null)
        {
            var burst = shimmerVFX.emission;
            shimmerVFX.transform.SetParent(null);
            shimmerVFX.Play();
            Destroy(shimmerVFX.gameObject, 3f);
        }

        if (crystalLight != null)
        {
            crystalLight.intensity = glowIntensity * 3f;
            Destroy(crystalLight.gameObject, 0.5f);
        }

        Destroy(gameObject, 0.1f);
    }
}
