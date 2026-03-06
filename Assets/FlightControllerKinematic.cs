using UnityEngine;
using UnityEngine.InputSystem;

public class FlightControllerKinematic : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] InputActionReference throttleAction;

    [Header("Speed Range (m/s)")]
    [SerializeField] float minForwardSpeed = 0f;
    [SerializeField] float maxForwardSpeed = 350f;

    [Header("Spool Rates (m/s per second)")]
    [SerializeField] float spoolUpRate = 40f;
    [SerializeField] float spoolDownRate = 25f;

    [Header("Optional Physics")]
    [SerializeField] bool useGravity = false;
    [SerializeField] float gravity = 9.81f;

    [Header("NetForces")]
    [SerializeField] float NetRoll;
    [SerializeField] float NetPitch;
    [SerializeField] float NetYaw;
    [SerializeField] float NetAccelerateRate;
    [SerializeField] float NetDecelerateRate;

    [Header("State (Read Only)")]
    [SerializeField, Range(0f, 1f)] float throttle01;
    [SerializeField] float targetForwardSpeed;
    [SerializeField] float currentForwardSpeed;
    [SerializeField] Vector3 velocityWorld;

    void OnEnable()
    {
        if (throttleAction != null) throttleAction.action.Enable();
    }

    void OnDisable()
    {
        if (throttleAction != null) throttleAction.action.Disable();
    }

    void Update()
    {
        ReadThrottle();
        UpdateForwardSpeedSpool();
        ApplyOptionalGravity();
        MoveTransform();
    }

    void ReadThrottle()
    {
        if (throttleAction == null)
        {
            throttle01 = Mathf.Clamp01(throttle01);
            return;
        }

        float raw = throttleAction.action.ReadValue<float>();

        if (raw < 0f) raw = (raw + 1f) * 0.5f;

        throttle01 = Mathf.Clamp01(raw);
    }

    void UpdateForwardSpeedSpool()
    {
        targetForwardSpeed = Mathf.Lerp(minForwardSpeed, maxForwardSpeed, throttle01);

        Vector3 fwd = transform.forward;

        float forwardSpeed = Vector3.Dot(velocityWorld, fwd);

        float rate = (targetForwardSpeed > forwardSpeed) ? spoolUpRate : spoolDownRate;

        float newForwardSpeed = Mathf.MoveTowards(forwardSpeed, targetForwardSpeed, rate * Time.deltaTime);

        Vector3 forwardComponentOld = fwd * forwardSpeed;
        Vector3 lateralComponent = velocityWorld - forwardComponentOld;

        velocityWorld = lateralComponent + (fwd * newForwardSpeed);

        currentForwardSpeed = newForwardSpeed;
    }

    void ApplyOptionalGravity()
    {
        if (!useGravity) return;
        velocityWorld += Vector3.down * gravity * Time.deltaTime;
    }

    void MoveTransform()
    {
        transform.position += velocityWorld * Time.deltaTime;
    }

    public void SetThrottle01(float value01)
    {
        throttle01 = Mathf.Clamp01(value01);
    }

    public float GetThrottle01() => throttle01;
    public float GetTargetForwardSpeed() => targetForwardSpeed;
    public float GetCurrentForwardSpeed() => currentForwardSpeed;

    public Vector3 GetVelocityWorld() => velocityWorld;
    public void SetVelocityWorld(Vector3 v) => velocityWorld = v;
    public void AddVelocityWorld(Vector3 dv) => velocityWorld += dv;
}
