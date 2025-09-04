using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FlightPhysics : MonoBehaviour
{
    [System.Serializable]
    public class Engine
    {
        public InputActionReference thrustInput;
        public Transform thrustPoint;

        [Header("Thrust (kN)")]
        public float minThrust = 0f;   // kilonewtons
        public float maxThrust = 98f;  // kilonewtons

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

    [Header("Lift Application Point")]
    [Tooltip("Where lift is applied. This transform will be positioned and rotated to follow the jet attitude.")]
    public Transform upforcePoint;
    [Tooltip("Vertical offset for the lift point relative to the jet origin.")]
    public float upforceYOffset = -5f;

    [Header("Engines")]
    public List<Engine> engines = new List<Engine>();

    [Header("Aerodynamic Drag")]
    [Tooltip("Air density in kg per m^3. Sea level is about 1.225.")]
    public float airDensity = 1.225f;
    [Tooltip("Reference areas per local axis (m^2). X=right, Y=up, Z=forward.")]
    public Vector3 referenceArea = new Vector3(4.5f, 9.0f, 2.8f);
    [Tooltip("Drag coefficients per local axis. X=side, Y=vertical, Z=forward.")]
    public Vector3 Cd = new Vector3(0.9f, 0.7f, 0.16f);
    [Tooltip("Global multiplier for drag forces.")]
    public float dragScale = 1f;

    [Header("Aerodynamic Lift")]
    [Tooltip("Wing reference area in m^2.")]
    public float wingArea = 46.5f;
    [Tooltip("Lift coefficient at zero angle of attack.")]
    public float CL0 = 0.2f;
    [Tooltip("Lift curve slope per radian.")]
    public float CL_alpha = 5.5f;
    [Tooltip("Clamp maximum CL to avoid absurd values.")]
    public float CL_max = 1.2f;
    [Tooltip("Clamp minimum CL (negative).")]
    public float CL_min = -0.6f;
    [Tooltip("Induced drag factor. Scales drag due to lift. Typical small value.")]
    public float inducedDragK = 0.03f;

    [Header("Angular Damping")]
    [Tooltip("Linear angular damping gains per local axis (N*m per rad/s). Order: X roll, Y yaw, Z pitch.")]
    public Vector3 angularDampingLinear = new Vector3(300f, 450f, 300f);
    [Tooltip("Quadratic angular damping gains per local axis (N*m per (rad/s)^2). Set to zero to disable.")]
    public Vector3 angularDampingQuadratic = new Vector3(8f, 12f, 8f);
    [Tooltip("Global multiplier for angular damping torques.")]
    public float angularDampingScale = 1f;

    [Header("Static Stability")]
    [Tooltip("Enable or disable static stability torque model.")]
    public bool enableStaticStability = true;
    [Tooltip("Do not apply stability below this speed in m/s.")]
    public float minSpeedForStability = 5f;
    [Tooltip("Pitch stability gain. Positive restores toward trim, negative diverges.")]
    public float pitchStability = 0.0f;
    [Tooltip("Yaw stability gain. Positive restores nose into the relative wind, negative diverges.")]
    public float yawStability = 0.0f;
    [Tooltip("Roll stability gain. Positive rolls toward wings level, negative rolls away.")]
    public float rollStability = 0.0f;
    [Tooltip("Scales stability with speed using a simple dynamic pressure style factor.")]
    public float stabilityScale = 1.0f;
    [Tooltip("Reference speed in m/s where stability reaches near full strength.")]
    public float stabilityRefSpeed = 100f;
    [Tooltip("Optional extra multiplier for stability when AoA or slip is large.")]
    public float stabilityNonlinear = 0.0f; // 0 means linear only

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
            // Follow jet position with vertical offset
            upforcePoint.position = transform.position + Vector3.up * upforceYOffset;
            // Match full jet attitude so lift tilts with pitch and roll
            upforcePoint.rotation = transform.rotation;
        }
    }

    void FixedUpdate()
    {
        if (targetRigidbody == null) return;

        ApplyRollControl();
        ApplyPitchControl();
        ApplyYawControl();
        ApplyEngineThrust();

        ApplyLift();            // new lift model
        ApplyAerodynamicDrag(); // v^2 drag
        ApplyAngularDamping();  // rate damping torques
        ApplyStaticStability(); // optional static stability bias
    }

    // --- helpers ---

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

    // --- roll ---

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

    // --- pitch ---

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

    // --- yaw ---

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

    // --- engines ---

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
                Vector3 pos = (forceApplicationCollider != null)
                    ? forceApplicationCollider.ClosestPoint(engine.thrustPoint.position)
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

    // --- aerodynamic lift ---

    void ApplyLift()
    {
        if (upforcePoint == null) return;

        Vector3 v = targetRigidbody.linearVelocity;
        float speed = v.magnitude;
        if (speed < 0.1f) return;

        // Angle of attack in radians, positive when nose above velocity
        Vector3 velDir = v.normalized;
        float aoaDeg = Vector3.SignedAngle(transform.forward, velDir, transform.right);
        float aoaRad = aoaDeg * Mathf.Deg2Rad;

        // Lift coefficient with clamp
        float CL = Mathf.Clamp(CL0 + CL_alpha * aoaRad, CL_min, CL_max);

        // Lift magnitude: 0.5 * rho * V^2 * S * CL
        float q = 0.5f * airDensity * speed * speed; // dynamic pressure
        float liftMag = q * wingArea * Mathf.Max(0f, CL); // no negative lift unless you want inverted behavior

        // Lift direction follows aircraft body up so it tilts when banking
        Vector3 liftDir = transform.up;

        // Apply lift at a stable point on the collider or at upforcePoint
        Rigidbody rb = GetRB();
        Vector3 pos = (forceApplicationCollider != null)
            ? forceApplicationCollider.ClosestPoint(upforcePoint.position)
            : upforcePoint.position;

        rb.AddForceAtPosition(liftDir * liftMag, pos, ForceMode.Force);

        // Induced drag proportional to CL^2 in the opposite direction of motion
        if (inducedDragK > 0f)
        {
            Vector3 inducedDrag = -velDir * (q * wingArea * inducedDragK * CL * CL);
            rb.AddForce(inducedDrag, ForceMode.Force);
        }
    }

    // --- aerodynamic drag ---

    void ApplyAerodynamicDrag()
    {
        if (dragScale <= 0f) return;

        Vector3 vWorld = targetRigidbody.linearVelocity;
        if (vWorld.sqrMagnitude < 1e-6f) return;

        Vector3 vLocal = transform.InverseTransformDirection(vWorld);

        Vector3 dragLocal = new Vector3(
            -0.5f * airDensity * referenceArea.x * Cd.x * vLocal.x * Mathf.Abs(vLocal.x),
            -0.5f * airDensity * referenceArea.y * Cd.y * vLocal.y * Mathf.Abs(vLocal.y),
            -0.5f * airDensity * referenceArea.z * Cd.z * vLocal.z * Mathf.Abs(vLocal.z)
        );

        dragLocal *= dragScale;

        Vector3 dragWorld = transform.TransformDirection(dragLocal);
        targetRigidbody.AddForce(dragWorld, ForceMode.Force);
    }

    // --- angular damping ---

    void ApplyAngularDamping()
    {
        if (angularDampingScale <= 0f) return;

        Vector3 wWorld = targetRigidbody.angularVelocity;
        if (wWorld.sqrMagnitude < 1e-8f) return;

        Vector3 wLocal = transform.InverseTransformDirection(wWorld);

        Vector3 tqLinearLocal = new Vector3(
            -angularDampingLinear.x * wLocal.x,
            -angularDampingLinear.y * wLocal.y,
            -angularDampingLinear.z * wLocal.z
        );

        Vector3 tqQuadLocal = new Vector3(
            -angularDampingQuadratic.x * wLocal.x * Mathf.Abs(wLocal.x),
            -angularDampingQuadratic.y * wLocal.y * Mathf.Abs(wLocal.y),
            -angularDampingQuadratic.z * wLocal.z * Mathf.Abs(wLocal.z)
        );

        Vector3 torqueLocal = (tqLinearLocal + tqQuadLocal) * angularDampingScale;
        Vector3 torqueWorld = transform.TransformDirection(torqueLocal);

        targetRigidbody.AddTorque(torqueWorld, ForceMode.Force);
    }

    // --- static stability ---

    void ApplyStaticStability()
    {
        if (!enableStaticStability) return;

        Vector3 v = targetRigidbody.linearVelocity;
        float speed = v.magnitude;
        if (speed < minSpeedForStability) return;

        float speedGain = Mathf.Clamp01(speed / Mathf.Max(1f, stabilityRefSpeed));

        // Angle of attack about local right
        float aoa = Vector3.SignedAngle(transform.forward, v.normalized, transform.right);
        // Slip angle about local up
        float slip = Vector3.SignedAngle(transform.forward, v.normalized, transform.up);
        // Bank angle relative to world up about local forward
        float bank = Vector3.SignedAngle(transform.up, Vector3.up, transform.forward);

        float aoaBoost = 1f + stabilityNonlinear * Mathf.Abs(aoa) / 30f;
        float slipBoost = 1f + stabilityNonlinear * Mathf.Abs(slip) / 30f;
        float bankBoost = 1f + stabilityNonlinear * Mathf.Abs(bank) / 45f;

        Vector3 torqueLocal = Vector3.zero;
        torqueLocal += new Vector3(0f, 0f, pitchStability * aoa * aoaBoost);  // Z is pitch
        torqueLocal += new Vector3(0f, yawStability * slip * slipBoost, 0f);   // Y is yaw
        torqueLocal += new Vector3(rollStability * bank * bankBoost, 0f, 0f);  // X is roll

        Vector3 torqueWorld = transform.TransformDirection(torqueLocal * stabilityScale * speedGain);
        targetRigidbody.AddTorque(torqueWorld, ForceMode.Force);
    }
}
