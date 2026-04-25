using UnityEngine;

public class BuoyancySystem : MonoBehaviour
{
    [SerializeField] float buoyancyForce = 15f;
    [SerializeField] float buoyancyFalloff = 0.002f;
    [SerializeField] float surfacePushForce = 25f;

    Rigidbody rb;
    DepthTracker depthTracker;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        depthTracker = GetComponent<DepthTracker>();
    }

    void FixedUpdate()
    {
        if (depthTracker == null) return;

        float depth = Mathf.Abs(depthTracker.CurrentDepth);

        // Never push the bathyscaphe upward when it is already above the water surface.
        if (depth <= 0.01f)
        {
            if (rb.linearVelocity.y > 0f)
            {
                Vector3 velocity = rb.linearVelocity;
                velocity.y *= 0.85f;
                rb.linearVelocity = velocity;
            }
            return;
        }

        float force = buoyancyForce * Mathf.Exp(-depth * buoyancyFalloff);
        rb.AddForce(Vector3.up * force, ForceMode.Force);
    }
}
