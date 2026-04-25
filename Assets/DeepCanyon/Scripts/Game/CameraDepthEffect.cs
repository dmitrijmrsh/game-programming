using UnityEngine;

public class CameraDepthEffect : MonoBehaviour
{
    [SerializeField] Transform target;
    [SerializeField] DepthTracker depthTracker;

    [Header("Follow")]
    [SerializeField] Vector3 baseOffset = new(0f, 2.8f, -6.5f);
    [SerializeField] float followSharpness = 8f;
    [SerializeField] float lookSharpness = 10f;
    [SerializeField] float lookHeight = 0.9f;
    [SerializeField] float lookAheadDistance = 18f;

    [Header("Collision")]
    [SerializeField] float collisionRadius = 0.35f;
    [SerializeField] float wallPadding = 0.2f;
    [SerializeField] float minDistanceFromTarget = 2.5f;
    [SerializeField] LayerMask collisionMask = ~0;

    [Header("FOV")]
    [SerializeField] float normalFOV = 72f;
    [SerializeField] float pressureFOV = 56f;

    [Header("Shake")]
    [SerializeField] float maxShake = 0.35f;

    Camera attachedCamera;

    void Awake()
    {
        attachedCamera = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        if (target == null)
            return;

        float pressure = depthTracker != null ? depthTracker.NormalizedPressure : 0f;
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;

        Quaternion orbitRotation = Quaternion.LookRotation(forward.normalized, Vector3.up);

        float shake = pressure * maxShake;
        Vector3 shakeOffset = new(
            (Mathf.PerlinNoise(Time.time * 6f, 0f) - 0.5f) * shake,
            (Mathf.PerlinNoise(0f, Time.time * 6f) - 0.5f) * shake,
            0f);

        Vector3 origin = target.position + Vector3.up * lookHeight;
        Vector3 desiredPos = target.position + orbitRotation * (baseOffset + shakeOffset);
        desiredPos = ResolveCameraCollision(origin, desiredPos);
        float followT = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        transform.position = Vector3.Lerp(transform.position, desiredPos, followT);

        Vector3 lookTarget = origin + forward.normalized * lookAheadDistance;
        Quaternion desiredRot = Quaternion.LookRotation(lookTarget - transform.position, Vector3.up);
        float lookT = 1f - Mathf.Exp(-lookSharpness * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, desiredRot, lookT);

        if (attachedCamera != null)
            attachedCamera.fieldOfView = Mathf.Lerp(normalFOV, pressureFOV, pressure);
    }

    Vector3 ResolveCameraCollision(Vector3 origin, Vector3 desiredPos)
    {
        Vector3 toDesired = desiredPos - origin;
        float desiredDistance = toDesired.magnitude;
        if (desiredDistance <= 0.001f)
            return desiredPos;

        Vector3 direction = toDesired / desiredDistance;
        RaycastHit[] hits = Physics.SphereCastAll(
            origin,
            collisionRadius,
            direction,
            desiredDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore);

        float closestDistance = desiredDistance;
        bool foundBlockingHit = false;

        for (int i = 0; i < hits.Length; i++)
        {
            Transform hitTransform = hits[i].transform;
            if (hitTransform == null || hitTransform == target || hitTransform.IsChildOf(target))
                continue;

            if (hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                foundBlockingHit = true;
            }
        }

        if (!foundBlockingHit)
            return desiredPos;

        float clampedDistance = Mathf.Max(minDistanceFromTarget, closestDistance - wallPadding);
        return origin + direction * clampedDistance;
    }
}
