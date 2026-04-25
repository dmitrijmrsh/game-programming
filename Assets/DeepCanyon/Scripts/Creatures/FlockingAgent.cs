using UnityEngine;

public enum CreatureType { Healer, Speeder, Armorer, Scout }

public class FlockingAgent : MonoBehaviour
{
    [SerializeField] CreatureType creatureType = CreatureType.Healer;
    [SerializeField] float maxSpeed = 12f;
    [SerializeField] float wanderStrength = 2f;
    [SerializeField] float habitatRadius = 22f;
    [SerializeField] float hardHabitatRadius = 40f;
    [SerializeField] float returnToHabitatForce = 12f;

    public CreatureType Type => creatureType;
    public Vector3 Velocity { get; set; }
    public bool IsFollowing { get; set; }

    Light glowLight;
    TrailRenderer trail;
    float wanderAngle;
    Vector3 habitatCenter;

    static readonly Color[] typeColors = {
        new Color(0.2f, 1f, 0.4f),   // Healer - green
        new Color(1f, 0.9f, 0.2f),   // Speeder - yellow
        new Color(0.3f, 0.5f, 1f),   // Armorer - blue
        new Color(0.8f, 0.3f, 1f)    // Scout - purple
    };

    public Color GlowColor => typeColors[(int)creatureType];

    void Awake()
    {
        habitatCenter = transform.position;
        glowLight = GetComponentInChildren<Light>();
        trail = GetComponentInChildren<TrailRenderer>();
        ApplyVisuals();
    }

    void ApplyVisuals()
    {
        Color c = GlowColor;
        if (glowLight != null)
        {
            glowLight.color = c;
            glowLight.intensity = 3f;
            glowLight.range = 12f;
        }

        if (trail != null)
        {
            trail.startColor = c;
            trail.endColor = new Color(c.r, c.g, c.b, 0f);
        }

        var rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var mat = rend.material;
            mat.SetColor("_EmissionColor", c * 4f);
            mat.EnableKeyword("_EMISSION");
            mat.color = c * 0.5f;
        }
    }

    public Vector3 ComputeWander()
    {
        wanderAngle += Random.Range(-0.5f, 0.5f);
        Vector3 wanderDir = new Vector3(Mathf.Cos(wanderAngle), Mathf.Sin(wanderAngle * 0.3f), Mathf.Sin(wanderAngle));
        return wanderDir.normalized * wanderStrength;
    }

    public void ApplyVelocity()
    {
        if (Velocity.sqrMagnitude > maxSpeed * maxSpeed)
            Velocity = Velocity.normalized * maxSpeed;

        transform.position += Velocity * Time.deltaTime;

        if (Velocity.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                Quaternion.LookRotation(Velocity), Time.deltaTime * 5f);
    }

    void ApplyHabitatConstraint(float deltaTime)
    {
        if (IsFollowing)
            return;

        Vector3 toHabitat = habitatCenter - transform.position;
        float distance = toHabitat.magnitude;
        if (distance <= habitatRadius)
            return;

        if (distance > hardHabitatRadius && distance > 0.001f)
        {
            transform.position = habitatCenter - toHabitat.normalized * hardHabitatRadius;
            toHabitat = habitatCenter - transform.position;
            distance = toHabitat.magnitude;
        }

        float pullStrength = returnToHabitatForce * Mathf.Max(1f, (distance - habitatRadius) / Mathf.Max(0.01f, habitatRadius));
        Velocity += toHabitat.normalized * pullStrength * deltaTime;
    }

    public void TickHabitatConstraint(float deltaTime)
    {
        ApplyHabitatConstraint(deltaTime);
    }

    public void SetFollowIntensity(float t)
    {
        if (glowLight != null)
            glowLight.intensity = Mathf.Lerp(3f, 8f, t);
    }
}
