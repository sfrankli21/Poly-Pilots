using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;

public class FlightPhysicsV2_ProbeForces : MonoBehaviour
{
    [System.Serializable]
    public class Engine
    {
        public InputActionReference thrustInput;
        public Transform thrustPoint;

        [Header("Thrust (kN)")]
        public float minThrust = 0f;    // kilonewtons
        public float maxThrust = 98f;   // kilonewtons

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
    [Tooltip("Set this to the collider that should receive forces. Commonly a BoxCollider or SphereCollider at the RB root.")]
    public Collider forceApplicationCollider;
    [Tooltip("If true, control forces are applied at the collider surface nearest each force point. If false, at the force point position.")]
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

    // ------------------ Probe Force Drag ------------------

    [Header("Probe Force Drag")]
    [Tooltip("Empty transform used as the probe. Its position is driven by this script.")]
    public Transform probe;
    [Tooltip("Distance ahead along the momentum heading where the probe sits.")]
    public float probeDistance = 5f;

    [Tooltip("Angle in degrees where the probe reaches Max Drag Force.")]
    public float maxOffBoreAngle = 60f;
    [Tooltip("Optional exponential shaping. 0 means linear. 2 to 5 gives faster growth near max.")]
    public float angleSharpness = 0f;
    [Tooltip("Maximum drag force in Newtons when off-bore equals or exceeds maxOffBoreAngle.")]
    public float maxDragForce = 20000f;
    [Tooltip("Reference speed in m per s used to scale the effect. 2 gives v squared like behavior.")]
    public float refSpeed = 150f;
    [Tooltip("Speed exponent. 2 is dynamic pressure style. Set to 0 to remove speed scaling.")]
    public float speedPower = 2f;

    [Tooltip("Smoothing for momentum heading. 0 raw, 1 heavy.")]
    [Range(0f, 1f)] public float momentumSmoothing = 0.15f;

    [Header("Probe Force Drag - Activation")]
    [Tooltip("Below this speed, the probe system hard-stops and resets. Prevents jitter at dead stop.")]
    public float zeroSpeedThreshold = 0.5f;
    [Tooltip("Speed to turn probe force ON when accelerating.")]
    public float minSpeedOn = 5f;
    [Tooltip("Speed to turn probe force OFF when slowing down. Should be lower than minSpeedOn.")]
    public float minSpeedOff = 3f;
    [Tooltip("Seconds to fade the force to zero when turning off.")]
    public float fadeOutTime = 0.25f;

    // ------------------ UI Debug ------------------

    [Header("UI Debug Output")]
    public TMP_Text debugText;   // optional. Assign a TextMeshProUGUI to display values

    // ------------------ Runtime state ------------------

    Vector3 _vhatSmoothed = Vector3.forward;
    bool _probeActive = false;
    float _currentStrength = 0f;       // applied strength after smoothing
    float _offBoreDegDisplay = 0f;     // for UI
    float _lastSpeed = 0f;             // for reference if needed

    // -----------------------------------------------------

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
        // Mirrors your original behavior. Upforce orientation is yaw only.
        if (upforcePoint != null)
        {
            Vector3 targetPosition = transform.position + Vector3.up * upforceYOffset;
            upforcePoint.position = targetPosition;

            Vector3 jetEuler = transform.eulerAngles;
            upforcePoint.rotation = Quaternion.Euler(0f, jetEuler.y, 0f);
        }

        // UI
        if (debugText != null)
        {
            debugText.text = $"Off-Bore: {_offBoreDegDisplay:F1} deg\nDrag Force: {_currentStrength:F0} N";
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

        ApplyProbeForce();

        _lastSpeed = targetRigidbody.linearVelocity.magnitude;
    }

    // ------------------ Helpers ------------------

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

    // ------------------ Controls ------------------

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

    // ------------------ Engines ------------------

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

    // ------------------ Upforce kept as in your base script ------------------

    void ApplyUpforce()
    {
        if (upforcePoint == null) return;

        float speed = targetRigidbody.linearVelocity.magnitude;
        float t = Mathf.Clamp01(speed / velocityForMaxUpforce);
        float force = Mathf.Lerp(0f, maxUpforce, t);

        Rigidbody rb = GetRB();
        Vector3 pos = (forceApplicationCollider != null)
            ? forceApplicationCollider.ClosestPoint(upforcePoint.position)
            : upforcePoint.position;

        rb.AddForceAtPosition(upforcePoint.up * force, pos, ForceMode.Force);
    }

    // ------------------ Probe Force core with dead-stop guard, hysteresis, fade ------------------

    void ApplyProbeForce()
    {
        if (targetRigidbody == null || probe == null) return;

        Vector3 v = targetRigidbody.linearVelocity;
        float speed = v.magnitude;

        // Hard stop at dead or near-dead speed
        if (speed <= Mathf.Max(0.0f, zeroSpeedThreshold))
        {
            _probeActive = false;
            _currentStrength = 0f;
            _offBoreDegDisplay = 0f;

            // Reset probe to aircraft so ClosestPoint is stable
            probe.position = transform.position;
            probe.rotation = transform.rotation;

            // Update UI now so it clears even if physics is paused
            if (debugText != null)
                debugText.text = $"Off-Bore: 0.0 deg\nDrag Force: 0 N";

            return;
        }

        // Hysteresis for activation when above zero-speed threshold
        if (_probeActive)
            _probeActive = speed > minSpeedOff;
        else
            _probeActive = speed > minSpeedOn;

        float targetStrength = 0f;
        _offBoreDegDisplay = 0f;

        if (_probeActive)
        {
            // Momentum heading with smoothing
            Vector3 vhat = v / speed;
            if (_vhatSmoothed.sqrMagnitude < 1e-8f) _vhatSmoothed = vhat;
            float slerpT = Mathf.Clamp01(1f - momentumSmoothing);
            _vhatSmoothed = Vector3.Slerp(_vhatSmoothed, vhat, slerpT);
            vhat = _vhatSmoothed;

            // Place the probe along momentum path
            probe.position = transform.position + vhat * Mathf.Max(0f, probeDistance);
            probe.rotation = Quaternion.LookRotation(vhat, transform.up);

            // Off-bore angle between desired heading and momentum heading
            _offBoreDegDisplay = Vector3.Angle(transform.forward, vhat);

            // Angle normalized 0..1
            float angleNorm = (maxOffBoreAngle > 0.0001f)
                ? Mathf.Clamp01(_offBoreDegDisplay / maxOffBoreAngle)
                : Mathf.Clamp01(_offBoreDegDisplay / 45f);

            // Optional exponential shaping
            if (angleSharpness > 0f)
            {
                float k = Mathf.Max(0f, angleSharpness);
                angleNorm = 1f - Mathf.Exp(-k * angleNorm);
            }

            // Speed scaling 0..1
            float speedNorm = (speedPower > 0f)
                ? Mathf.Clamp01(Mathf.Pow(speed / Mathf.Max(1f, refSpeed), speedPower))
                : 1f;

            // Target force
            targetStrength = Mathf.Lerp(0f, maxDragForce, angleNorm * speedNorm);
        }
        else
        {
            // Inactive: keep probe at aircraft and fade out
            probe.position = transform.position;
            probe.rotation = transform.rotation;
            targetStrength = 0f;
        }

        // Smooth the applied strength
        float dt = Time.fixedDeltaTime;
        float attackTime = 0.05f;                              // quick attack
        float attackRate = (attackTime > 1e-4f) ? 1f / attackTime : 999f;
        float releaseRate = (fadeOutTime > 1e-4f) ? 1f / fadeOutTime : 999f;

        if (targetStrength >= _currentStrength)
            _currentStrength = Mathf.MoveTowards(_currentStrength, targetStrength, attackRate * dt * maxDragForce);
        else
            _currentStrength = Mathf.MoveTowards(_currentStrength, targetStrength, releaseRate * dt * maxDragForce);

        // Apply the force if any
        if (_currentStrength > 0f)
        {
            Vector3 vhatNow = v.normalized;
            Vector3 applicationPoint = (forceApplicationCollider != null)
                ? forceApplicationCollider.ClosestPoint(probe.position)
                : probe.position;

            Vector3 F = -vhatNow * _currentStrength;
            GetRB().AddForceAtPosition(F, applicationPoint, ForceMode.Force);
        }

        // Update UI here too
        if (debugText != null)
        {
            debugText.text = $"Off-Bore: {_offBoreDegDisplay:F1} deg\nDrag Force: {_currentStrength:F0} N";
        }
    }
}
