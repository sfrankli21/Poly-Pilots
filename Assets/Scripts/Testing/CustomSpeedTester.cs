using UnityEngine;

public class CustomSpeedTester : MonoBehaviour
{
    [Header("Target")]
    public Rigidbody target;

    [Header("Speed (Knots)")]
    public float targetSpeedKnots = 100f;

    [Header("Control")]
    [Tooltip("Proportional gain from speed error (m/s) to commanded acceleration (m/s^2).")]
    public float kp = 0.8f;
    [Tooltip("Max acceleration magnitude applied (m/s^2).")]
    public float maxAcceleration = 15f;
    [Tooltip("Deadzone around target speed (knots) where no force is applied.")]
    public float toleranceKnots = 1.0f;

    [Header("Thrust Direction")]
    public Transform thrustFrame;
    public Vector3 localThrustAxis = Vector3.forward;

    const float KnotsToMS = 0.514444f;

    void Reset()
    {
        thrustFrame = transform;
    }

    void FixedUpdate()
    {
        if (target == null) return;

        float targetSpeedMS = Mathf.Max(0f, targetSpeedKnots * KnotsToMS);
        float toleranceMS = Mathf.Abs(toleranceKnots) * KnotsToMS;

        Vector3 v = target.linearVelocity;
        float speed = v.magnitude;

        float error = targetSpeedMS - speed;
        if (Mathf.Abs(error) <= toleranceMS) return;

        float cmdAccel = Mathf.Clamp(error * kp, -maxAcceleration, maxAcceleration);

        Vector3 forwardDir = (thrustFrame ? thrustFrame : transform).TransformDirection(localThrustAxis).normalized;
        Vector3 forceDir;

        if (error > 0f)
        {
            forceDir = forwardDir;
        }
        else
        {
            forceDir = v.sqrMagnitude > 1e-4f ? -v.normalized : -forwardDir;
        }

        Vector3 force = forceDir * (target.mass * Mathf.Abs(cmdAccel));
        target.AddForce(force, ForceMode.Force);
    }
}
