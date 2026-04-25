using UnityEngine;
using UnityEngine.InputSystem;

public class BathyscapheController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float thrustForce = 60f;
    [SerializeField] float strafeForce = 45f;
    [SerializeField] float verticalForce = 50f;
    [SerializeField] float yawTorque = 25f;
    [SerializeField] float pitchTorque = 18f;
    [SerializeField] float maxSpeed = 25f;

    [Header("Pressure Damping")]
    [SerializeField] float controlDamping = 0.25f;

    [Header("Drag Scaling")]
    [SerializeField] float baseDrag = 0.8f;
    [SerializeField] float baseAngularDrag = 1.5f;
    [SerializeField] float depthDragMultiplier = 0.003f;

    Rigidbody rb;
    DepthTracker depthTracker;
    Vector2 moveInput;
    float verticalInput;
    Vector2 lookInput;
    float speedMultiplier = 1f;

    public float SpeedMultiplier { get => speedMultiplier; set => speedMultiplier = value; }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        depthTracker = GetComponent<DepthTracker>();
        rb.useGravity = false;
        rb.linearDamping = baseDrag;
        rb.angularDamping = baseAngularDrag;
    }

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void OnDisable()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void FixedUpdate()
    {
        float pressure = depthTracker ? depthTracker.NormalizedPressure : 0f;
        float dampFactor = 1f / (1f + pressure * controlDamping);

        float depthFactor = depthTracker ? Mathf.Abs(depthTracker.CurrentDepth) * depthDragMultiplier : 0f;
        rb.linearDamping = baseDrag + depthFactor;
        rb.angularDamping = baseAngularDrag + depthFactor * 0.5f;

        Vector3 thrust = (transform.forward * moveInput.y * thrustForce
                        + transform.right * moveInput.x * strafeForce
                        + transform.up * verticalInput * verticalForce)
                        * dampFactor * speedMultiplier;

        rb.AddForce(thrust, ForceMode.Force);

        Vector3 torque = (transform.up * lookInput.x * yawTorque
                        - transform.right * lookInput.y * pitchTorque)
                        * dampFactor;

        rb.AddTorque(torque, ForceMode.Force);

        float roll = transform.eulerAngles.z;
        if (roll > 180f) roll -= 360f;
        rb.AddTorque(-transform.forward * roll * 3f, ForceMode.Force);

        if (rb.linearVelocity.magnitude > maxSpeed * speedMultiplier)
            rb.linearVelocity = rb.linearVelocity.normalized * maxSpeed * speedMultiplier;
    }

    public void OnMove(InputValue value) => moveInput = value.Get<Vector2>();
    public void OnVertical(InputValue value) => verticalInput = value.Get<float>();
    public void OnLook(InputValue value) => lookInput = value.Get<Vector2>() * 0.3f;
}
