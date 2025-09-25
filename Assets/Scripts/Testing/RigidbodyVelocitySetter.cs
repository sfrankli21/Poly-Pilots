using UnityEngine;

public class RigidbodyVelocitySetter : MonoBehaviour
{
    public enum Mode
    {
        ForwardSpeed,       // Set speed along a transform's forward
        KeepDirectionSpeed, // Keep current direction, set new speed
        ExactVector,        // Set exact world-space velocity vector
        Stop,               // Zero linear (and optional angular) velocity
        ClampMaxSpeed       // Only clamp to a max speed
    }

    [Header("Target")]
    public Rigidbody target;

    [Header("Behavior")]
    public Mode mode = Mode.ForwardSpeed;
    [Tooltip("Apply once on Start/when toggled off, or every FixedUpdate if enabled.")]
    public bool applyEveryFixedUpdate = true;

    [Header("ForwardSpeed / KeepDirectionSpeed")]
    public Transform directionFrame; // if null, uses 'this' transform
    public float speedKnots = 100f;

    [Header("ExactVector (m/s, world)")]
    public Vector3 exactVelocity = new Vector3(0f, 0f, 100f);

    [Header("ClampMaxSpeed")]
    public float maxSpeedKnots = 300f;

    [Header("Stop")]
    public bool zeroAngularOnStop = true;

    const float KnotsToMS = 0.514444f;

    void Reset()
    {
        directionFrame = transform;
    }

    void Start()
    {
        if (!applyEveryFixedUpdate) Apply();
    }

    void FixedUpdate()
    {
        if (applyEveryFixedUpdate) Apply();
    }

    public void Apply()
    {
        if (target == null) return;

        switch (mode)
        {
            case Mode.ForwardSpeed:
                {
                    var frame = directionFrame != null ? directionFrame : transform;
                    float speed = Mathf.Max(0f, speedKnots * KnotsToMS);
                    Vector3 dir = frame.forward.normalized;
                    target.linearVelocity = dir * speed;
                    break;
                }

            case Mode.KeepDirectionSpeed:
                {
                    float speed = Mathf.Max(0f, speedKnots * KnotsToMS);
                    Vector3 v = target.linearVelocity;
                    Vector3 dir = v.sqrMagnitude > 1e-6f ? v.normalized : (directionFrame ? directionFrame.forward : transform.forward).normalized;
                    target.linearVelocity = dir * speed;
                    break;
                }

            case Mode.ExactVector:
                {
                    target.linearVelocity = exactVelocity;
                    break;
                }

            case Mode.Stop:
                {
                    target.linearVelocity = Vector3.zero;
                    if (zeroAngularOnStop) target.angularVelocity = Vector3.zero;
                    break;
                }

            case Mode.ClampMaxSpeed:
                {
                    float maxSpeed = Mathf.Max(0f, maxSpeedKnots * KnotsToMS);
                    target.linearVelocity = Vector3.ClampMagnitude(target.linearVelocity, maxSpeed);
                    break;
                }
        }
    }
}
