using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class FlightPhysicsTorque : MonoBehaviour
{
    [System.Serializable]
    public class Engine
    {
        public InputActionReference thrustInput;
        public Transform thrustPoint;

        [Header("Thrust (kN)")]
        public float minThrust = 0f;   
        public float maxThrust = 98f;   

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

    [Header("Rigidbody")]
    public Rigidbody targetRigidbody;


    [Header("Roll")]
    public InputActionReference rollInput;

    [Header("Pitch")]
    public InputActionReference pitchInput;

    [Header("Yaw")]
    public InputActionReference yawInput;

  
    [Header("Engines")]
    public List<Engine> engines = new List<Engine>();


    [Header("Debug")]
    [Tooltip("If true, overrides engine inputs and applies thrust to hold a constant speed (knots).")]
    public bool constantSpeed = false;

    [Tooltip("Target airspeed in knots when Constant Speed is enabled.")]
    public float debugTargetSpeedKnots = 300f;

    [Tooltip("Optional index in 'engines' list to treat as LEFT engine. -1 = auto/all fwd engines.")]
    public int leftEngineIndex = -1;

    [Tooltip("Optional index in 'engines' list to treat as RIGHT engine. -1 = auto/all fwd engines.")]
    public int rightEngineIndex = -1;

    [Tooltip("Proportional gain for speed hold (accel = Kp * (v_target - v)).")]
    public float debugSpeedKp = 1.5f;

    [Tooltip("Maximum acceleration magnitude the speed hold is allowed to command (m/s^2).")]
    public float debugMaxAccel = 30f;


    [Header("Speed / Scaling")]
    [Tooltip("Maximum speed (knots). If > 0, velocity is clamped to this.")]
    public float speedCapKnots = 800f;

    [Tooltip("Speed scaling when speed is ~0.")]
    public float minSpeedScale = 0.35f;

    [Tooltip("Speed scaling when speed approaches Speed Cap.")]
    public float maxSpeedScale = 1.0f;


    [Header("Pitch Torque")]
    [Tooltip("Commanded angular acceleration for full pitch input at max speed scale (deg/s^2).")]
    public float pitchRateDegPerSec = 60f;

    const float KNOTS_TO_MS = 0.514444f;
    const float DEG2RAD = Mathf.Deg2Rad;

    void OnEnable()
    {
        rollInput?.action?.Enable();
        pitchInput?.action?.Enable();
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
        rollInput?.action?.Disable();
        pitchInput?.action?.Disable();
        yawInput?.action?.Disable();

        foreach (var engine in engines)
        {
            engine.thrustInput?.action?.Disable();
            if (engine.enableThrustVector)
                engine.thrustVectorInput?.action?.Disable();
        }
    }

    void FixedUpdate()
    {
        if (targetRigidbody == null) return;

       
        if (constantSpeed)
            ApplyConstantSpeedThrust();
        else
            ApplyEngineThrust();

      
        ApplyPitchTorqueControl();

        
        ApplySpeedCap();
    }

    void ApplyEngineThrust()
    {
        if (targetRigidbody == null) return;

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
                Vector3 pos = engine.thrustPoint.position;
                targetRigidbody.AddForceAtPosition(dir * thrustN, pos, ForceMode.Force);
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

   
    void ApplyConstantSpeedThrust()
    {
        if (targetRigidbody == null) return;

        Vector3 v = targetRigidbody.linearVelocity;
        float speed = v.magnitude;
        float targetSpeed = Mathf.Max(0f, debugTargetSpeedKnots) * KNOTS_TO_MS;
        float error = targetSpeed - speed;

        float accelCmd = Mathf.Clamp(debugSpeedKp * error, -debugMaxAccel, debugMaxAccel);

        var driven = GetDrivenEngines();
        if (driven.Count == 0) return;

        float forceTotal = targetRigidbody.mass * accelCmd;
        float perEngineForce = forceTotal / driven.Count;

        foreach (var eIndex in driven)
        {
            if (eIndex < 0 || eIndex >= engines.Count) continue;
            var engine = engines[eIndex];
            if (engine.thrustPoint == null) continue;

            Vector3 fwd = engine.thrustPoint.forward;
            Vector3 dir = (accelCmd >= 0f) ? fwd : -fwd;

            float thrustN = Mathf.Abs(perEngineForce);
            float maxN = Mathf.Max(0f, engine.maxThrust) * 1000f;
            thrustN = Mathf.Min(thrustN, maxN);

            targetRigidbody.AddForceAtPosition(dir.normalized * thrustN, engine.thrustPoint.position, ForceMode.Force);

            if (engine.enableThrustVector && engine.thrustVectorInput != null)
            {
                float vIn = Mathf.Clamp(engine.thrustVectorInput.action.ReadValue<float>(), -1f, 1f);
                float aoa = Mathf.Lerp(engine.minAOA, engine.maxAOA, (vIn + 1f) * 0.5f);
                Vector3 eul = engine.thrustPoint.localEulerAngles;
                engine.thrustPoint.localRotation = Quaternion.Euler(aoa, eul.y, eul.z);
            }
        }
    }

    List<int> GetDrivenEngines()
    {
        var outIdx = new List<int>(2);

        bool leftValid = leftEngineIndex >= 0 && leftEngineIndex < engines.Count && engines[leftEngineIndex] != null;
        bool rightValid = rightEngineIndex >= 0 && rightEngineIndex < engines.Count && engines[rightEngineIndex] != null;

        if (leftValid) outIdx.Add(leftEngineIndex);
        if (rightValid) outIdx.Add(rightEngineIndex);

        if (outIdx.Count > 0) return outIdx;

        for (int i = 0; i < engines.Count; i++)
        {
            var e = engines[i];
            if (e == null || e.thrustPoint == null) continue;
            if (e.pushForward) outIdx.Add(i);
        }
        if (outIdx.Count == 0)
        {
            for (int i = 0; i < engines.Count; i++)
            {
                var e = engines[i];
                if (e == null || e.thrustPoint == null) continue;
                outIdx.Add(i);
            }
        }
        return outIdx;
    }

  
    void ApplyPitchTorqueControl()
    {
        if (pitchInput == null || targetRigidbody == null) return;


        float speedScale = ComputeSpeedScale01();
        float scale = Mathf.Lerp(minSpeedScale, maxSpeedScale, speedScale);

     
        float u = Mathf.Clamp(pitchInput.action.ReadValue<float>(), -1f, 1f);

        float alphaDeg = u * Mathf.Max(0f, pitchRateDegPerSec) * scale;
        float alphaRad = alphaDeg * DEG2RAD;

     
        Vector3 axisWorld = transform.right; 
        Vector3 torque = ComputeTorqueForAngularAcceleration(axisWorld, alphaRad);

        targetRigidbody.AddTorque(torque, ForceMode.Force);
    }

  
    float ComputeSpeedScale01()
    {
        float v = targetRigidbody.linearVelocity.magnitude;
        float capMS = Mathf.Max(0.1f, speedCapKnots) * KNOTS_TO_MS; 
        float t = Mathf.Clamp01(v / capMS);
        return t;
    }

 
    Vector3 ComputeTorqueForAngularAcceleration(Vector3 axisWorld, float alphaRadPerSec2)
    {
        axisWorld = axisWorld.normalized;

       
        Quaternion q = targetRigidbody.inertiaTensorRotation;
        Vector3 Ilocal = targetRigidbody.inertiaTensor;

     
        Vector3 axisInertiaSpace = Quaternion.Inverse(q) * axisWorld;

    
        Vector3 tauInertiaSpace = new Vector3(
            Ilocal.x * axisInertiaSpace.x,
            Ilocal.y * axisInertiaSpace.y,
            Ilocal.z * axisInertiaSpace.z
        ) * alphaRadPerSec2;

      
        Vector3 tauWorld = q * tauInertiaSpace;
        return tauWorld;
    }

  
    void ApplySpeedCap()
    {
        if (speedCapKnots <= 0f || targetRigidbody == null) return;

        float capMS = speedCapKnots * KNOTS_TO_MS;
        Vector3 v = targetRigidbody.linearVelocity;
        float s = v.magnitude;
        if (s > capMS && s > 1e-3f)
        {
            Vector3 nhat = v / s;
            targetRigidbody.linearVelocity = nhat * capMS;
        }
    }
}