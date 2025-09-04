using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FlightPhysicsV2 : MonoBehaviour
{
    [System.Serializable]
    public class Engine
    {
        public InputActionReference thrustInput;
        public Transform thrustPoint;

        [Header("Thrust (kN)")]
        public float minThrust = 0f;   // in kilonewtons
        public float maxThrust = 98f;  // in kilonewtons

        public bool enableThrustVector = false;
        public InputActionReference thrustVectorInput;
        public float minAOA = -30f;
        public float maxAOA = 30f;

        [Header("Push Directions")]
        public bool pushUp = false;
        public bool pushDown = false;
        public bool pushLeft = false;
        public bool pushRight = false;
        public bool pushForward = true;
        public bool pushBack = false;
    }

    public Rigidbody targetRigidbody;

    [Header("Force Application Target")]
    [Tooltip("Set this to your BoxCollider. Control forces can apply at this collider surface for stable torque.")]
    public Collider forceApplicationCollider;
    [Tooltip("If true, control forces are applied at the collider surface nearest each force point; if false, at the force point position.")]
    public bool useColliderSurfaceForControls = true;

    [Header("Roll Control (labels show push direction)")]
    public Transform rollForcePointPushDown;
    public Transform rollForcePointPushUp;
    public float rollForceStrength = 10f;
    public InputActionReference rollInput;
    public float rollMin = -2.5f;
    public float rollMax = 2.5f;

    [Header("Pitch Control (labels show push direction)")]
    public Transform pitchForcePointPushDown;
    public Transform pitchForcePointPushUp;
    public float pitchForceStrength = 10f;
    public InputActionReference pitchInput;
    public float pitchMin = -2.5f;
    public float pitchMax = 2.5f;

    [Header("Yaw Control (labels show push direction)")]
    public Transform yawForcePointPushRight;
    public Transform yawForcePointPushLeft;
    public float yawForceStrength = 10f;
    public InputActionReference yawInput;
    public float yawMin = -1f;
    public float yawMax = 1f;

    [Header("Upforce")]
    public float velocityForMaxUpforce = 40f;
    public float maxUpforce = 9.81f;
    public Transform upforcePoint;
    public float upforceYOffset = -5f;

    [Header("Engines")]
    public List<Engine> engines = new List<Engine>();

    [Header("Off-Bore Linear Damping")]
    [Tooltip("Exponential curve sharpness. Higher values reach max sooner.")]
    public float angleSharpness = 3f; // try 2..5
    [Tooltip("Damping when desired and momentum headings are aligned.")]
    public float baseDamping = 0.005f;
    [Tooltip("Damping when off-bore angle reaches maxOffBoreAngle.")]
    public float maxDamping = 0.12f;
    [Tooltip("Angle in degrees where damping saturates at maxDamping.")]
    public float maxOffBoreAngle = 45f; // degrees
    [Tooltip("Reference speed for scaling (m/s). Effect ramps up toward this speed.")]
    public float refSpeed = 100f;
    [Tooltip("Speed exponent for scaling. 2.0 approximates v^2 growth, clamped to 0..1.")]
    public float speedPower = 2f;

    void OnEnable()
    {
        pitchInput?.action?.Enable();
        rollInput?.action?.Enable();
        yawInput?.action?.Enable();

        foreach (var engine in engines)
        {
            engine.thrustInput?.action?.Enable();
            if (engine.enableThrustVector)
                engine.thrustVectorInput?.action?.Enable();
        }
    }

    void OnDisable()
    {
        pitchInput?.action?.Disable();
        rollInput?.action?.Disable();
        yawInput?.action?.Disable();

        foreach (var engine in engines)
        {
            engine.thrustInput?.action?.Disable();
            if (engine.enableThrustVector)
                engine.thrustVectorInput?.action?.Disable();
        }
    }

    void LateUpdate()
    {
        if (upforcePoint != null)
        {
            Vector3 targetPosition = transform.position + Vector3.up * upforceYOffset;
            upforcePoint.position = targetPosition;

            Vector3 jetEuler = transform.eulerAngles;
            upforcePoint.rotation = Quaternion.Euler(0f, jetEuler.y, 0f);
        }
    }

    void FixedUpdate()
    {
        if (targetRigidbody == null) return;

        ApplyRollControl();
        ApplyPitchControl();
        ApplyYawControl();
        ApplyEngineThrust();
        ApplyUpforce();

        ApplyOffBoreLinearDamping(); // auto-computed from base, max, and max angle
    }

    Rigidbody GetRB()
    {
        if (forceApplicationCollider != null && forceApplicationCollider.attachedRigidbody != null)
            return forceApplicationCollider.attachedRigidbody;
        return targetRigidbody;
    }

    Vector3 ControlApplicationPoint(Transform forcePoint)
    {
        if (useColliderSurfaceForControls && forceApplicationCollider != null && forcePoint != null)
            return forceApplicationCollider.ClosestPoint(forcePoint.position);
        return (forcePoint != null) ? forcePoint.position : Vector3.zero;
    }

    static Vector3 DirFrom(Transform t, Vector3 localDir)
    {
        return (t != null) ? t.TransformDirection(localDir) : Vector3.zero;
    }

    void ApplyControlForce(Transform forcePoint, Vector3 localDirection, float strength)
    {
        if (forcePoint == null || strength == 0f) return;
        Rigidbody rb = GetRB();
        Vector3 dir = DirFrom(forcePoint, localDirection);
        Vector3 pos = ControlApplicationPoint(forcePoint);
        rb.AddForceAtPosition(dir * strength, pos, ForceMode.Force);
    }

    void ApplyRollControl()
    {
        float input = rollInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(rollMin, rollMax, (input + 1f) * 0.5f);

        if (rollForcePointPushDown != null)
        {
            var p = rollForcePointPushDown.localPosition;
            p.x = offset;
            rollForcePointPushDown.localPosition = p;
            ApplyControlForce(rollForcePointPushDown, Vector3.down, rollForceStrength);
        }

        if (rollForcePointPushUp != null)
        {
            var p = rollForcePointPushUp.localPosition;
            p.x = -offset;
            rollForcePointPushUp.localPosition = p;
            ApplyControlForce(rollForcePointPushUp, Vector3.up, rollForceStrength);
        }
    }

    void ApplyPitchControl()
    {
        float input = pitchInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(pitchMin, pitchMax, (input + 1f) * 0.5f);

        if (pitchForcePointPushDown != null)
        {
            var p = pitchForcePointPushDown.localPosition;
            p.z = offset;
            pitchForcePointPushDown.localPosition = p;
            ApplyControlForce(pitchForcePointPushDown, Vector3.down, pitchForceStrength);
        }

        if (pitchForcePointPushUp != null)
        {
            var p = pitchForcePointPushUp.localPosition;
            p.z = -offset;
            pitchForcePointPushUp.localPosition = p;
            ApplyControlForce(pitchForcePointPushUp, Vector3.up, pitchForceStrength);
        }
    }

    void ApplyYawControl()
    {
        float input = yawInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(yawMin, yawMax, (input + 1f) * 0.5f);

        if (yawForcePointPushRight != null)
        {
            var p = yawForcePointPushRight.localPosition;
            p.z = offset;
            yawForcePointPushRight.localPosition = p;
            ApplyControlForce(yawForcePointPushRight, Vector3.right, yawForceStrength);
        }

        if (yawForcePointPushLeft != null)
        {
            var p = yawForcePointPushLeft.localPosition;
            p.z = -offset;
            yawForcePointPushLeft.localPosition = p;
            ApplyControlForce(yawForcePointPushLeft, Vector3.left, yawForceStrength);
        }
    }

    void ApplyEngineThrust()
    {
        foreach (var engine in engines)
        {
            if (engine.thrustPoint == null || engine.thrustInput == null) continue;

            float input = Mathf.Clamp(engine.thrustInput.action.ReadValue<float>(), -1f, 1f);
            float thrustN = Mathf.Lerp(engine.minThrust, engine.maxThrust, (input + 1f) * 0.5f) * 1000f;

            Vector3 dir = Vector3.zero;
            if (engine.pushUp) dir += engine.thrustPoint.up;
            if (engine.pushDown) dir += -engine.thrustPoint.up;
            if (engine.pushRight) dir += engine.thrustPoint.right;
            if (engine.pushLeft) dir += -engine.thrustPoint.right;
            if (engine.pushForward) dir += engine.thrustPoint.forward;
            if (engine.pushBack) dir += -engine.thrustPoint.forward;

            if (dir != Vector3.zero)
            {
                dir.Normalize();
                Rigidbody rb = GetRB();
                Vector3 pos = (forceApplicationCollider != null) ? forceApplicationCollider.ClosestPoint(engine.thrustPoint.position)
                                                                 : engine.thrustPoint.position;
                rb.AddForceAtPosition(dir * thrustN, pos, ForceMode.Force);
            }

            if (engine.enableThrustVector && engine.thrustVectorInput != null)
            {
                float v = Mathf.Clamp(engine.thrustVectorInput.action.ReadValue<float>(), -1f, 1f);
                float aoa = Mathf.Lerp(engine.minAOA, engine.maxAOA, (v + 1f) * 0.5f);
                Vector3 e = engine.thrustPoint.localEulerAngles;
                engine.thrustPoint.localRotation = Quaternion.Euler(aoa, e.y, e.z);
            }
        }
    }

    void ApplyUpforce()
    {
        if (upforcePoint == null) return;

        float speed = targetRigidbody.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / velocityForMaxUpforce);
        float force = Mathf.Lerp(0f, maxUpforce, t);

        Rigidbody rb = GetRB();
        Vector3 pos = (forceApplicationCollider != null) ? forceApplicationCollider.ClosestPoint(upforcePoint.position)
                                                         : upforcePoint.position;
        rb.AddForceAtPosition(upforcePoint.up * force, pos, ForceMode.Force);
    }

    // Auto-computed off-bore driven linear damping
    void ApplyOffBoreLinearDamping()
    {
        if (targetRigidbody == null) return;

        Vector3 v = targetRigidbody.linearVelocity;
        float speed = v.magnitude;

        // When basically stopped, keep baseline
        if (speed < 0.5f)
        {
            targetRigidbody.linearDamping = baseDamping;
            return;
        }

        Vector3 vhat = v / Mathf.Max(speed, 1e-6f);
        Vector3 fhat = transform.forward;

        // Off-bore angle in degrees [0..180]
        float offBoreDeg = Vector3.Angle(fhat, vhat);

        // Normalize by maxOffBoreAngle to get 0..1
        float angleNorm = 0f;
        if (maxOffBoreAngle > 0.0001f)
            angleNorm = Mathf.Clamp01(offBoreDeg / maxOffBoreAngle);
        else
            angleNorm = Mathf.Clamp01(offBoreDeg / 45f); // fallback if maxOffBoreAngle is zero

        // Speed scaling in 0..1 so we never exceed maxDamping because of speed alone
        float speedNorm = Mathf.Clamp01(Mathf.Pow(speed / Mathf.Max(1f, refSpeed), Mathf.Max(0.01f, speedPower)));

        // Exponential easing from baseDamping to maxDamping
        // angleSharpness controls how quickly we approach max as angle grows
        float angleCurve = 1f - Mathf.Exp(-Mathf.Max(0f, angleSharpness) * angleNorm);

        float damping = Mathf.Lerp(baseDamping, maxDamping, angleCurve * speedNorm);
        targetRigidbody.linearDamping = damping; // or linearDamping on newer API
    }

}
