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
        public float minThrust = 0f;
        public float maxThrust = 10f;

        [Header("Spool")]
        public float accelerateRate = 10f;
        public float decelerateRate = 10f;

        [HideInInspector] public float currentThrust;
        [HideInInspector] public float targetThrust;

        public bool enableThrustVector = false;
        public InputActionReference thrustVectorInput;
        public float minAOA = -30f;
        public float maxAOA = 30f;

        public bool pushUp = false;
        public bool pushDown = false;
        public bool pushLeft = false;
        public bool pushRight = false;
        public bool pushForward = true;
        public bool pushBack = false;
    }

    [System.Serializable]
    public class ControlSurfaceVisual
    {
        [InspectorName("Transform")]
        public Transform targetTransform;

        [InspectorName("Max Deflect Angle")]
        public float maxDeflectAngle = 15f;

        [InspectorName("Min Deflect Angle")]
        public float minDeflectAngle = -15f;

        [InspectorName("Rotate Local X")]
        public bool rotateLocalX;

        [InspectorName("Rotate Local Y")]
        public bool rotateLocalY;

        [InspectorName("Rotate Local Z")]
        public bool rotateLocalZ;
    }

    public Rigidbody targetRigidbody;

    [Header("Roll Control")]
    public Transform rollForcePoint;
    public Transform rollMirroredForcePoint;
    public float rollForceStrength = 10f;
    public InputActionReference rollInput;
    public float rollMin = -2.5f;
    public float rollMax = 2.5f;

    [SerializeField, InspectorName("[Roll] Control Surface Visuals")]
    ControlSurfaceVisual[] rollControlSurfaceVisuals;

    [Header("Pitch Control")]
    public Transform pitchForcePoint;
    public Transform pitchMirroredForcePoint;
    public float pitchForceStrength = 10f;
    public InputActionReference pitchInput;
    public float pitchMin = -2.5f;
    public float pitchMax = 2.5f;

    [SerializeField, InspectorName("[Pitch] Control Surface Visuals")]
    ControlSurfaceVisual[] pitchControlSurfaceVisuals;

    [Header("Yaw Control")]
    public Transform yawForcePoint;
    public Transform yawMirroredForcePoint;
    public float yawForceStrength = 10f;
    public InputActionReference yawInput;
    public float yawMin = -1f;
    public float yawMax = 1f;

    [SerializeField, InspectorName("[Yaw] Control Surface Visuals")]
    ControlSurfaceVisual[] yawControlSurfaceVisuals;

    [Header("Engines")]
    public List<Engine> engines = new List<Engine>();

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

    void FixedUpdate()
    {
        if (targetRigidbody == null) return;

        ApplyRollControl();
        ApplyPitchControl();
        ApplyYawControl();
        ApplyEngineThrust();
    }

    void ApplyRollControl()
    {
        float input = rollInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(rollMin, rollMax, (input + 1f) * 0.5f);

        ApplyControlSurfaceVisuals(rollControlSurfaceVisuals, input);

        if (rollForcePoint != null)
        {
            Vector3 pos = rollForcePoint.localPosition;
            pos.x = offset;
            rollForcePoint.localPosition = pos;

            Vector3 dir = rollForcePoint.TransformDirection(Vector3.down);
            targetRigidbody.AddForceAtPosition(dir * rollForceStrength, rollForcePoint.position);
        }

        if (rollMirroredForcePoint != null)
        {
            Vector3 pos = rollMirroredForcePoint.localPosition;
            pos.x = -offset;
            rollMirroredForcePoint.localPosition = pos;

            Vector3 dir = rollMirroredForcePoint.TransformDirection(Vector3.up);
            targetRigidbody.AddForceAtPosition(dir * rollForceStrength, rollMirroredForcePoint.position);
        }
    }

    void ApplyPitchControl()
    {
        float input = pitchInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(pitchMin, pitchMax, (input + 1f) * 0.5f);

        ApplyControlSurfaceVisuals(pitchControlSurfaceVisuals, input);

        if (pitchForcePoint != null)
        {
            Vector3 pos = pitchForcePoint.localPosition;
            pos.z = offset;
            pitchForcePoint.localPosition = pos;

            Vector3 dir = pitchForcePoint.TransformDirection(Vector3.down);
            targetRigidbody.AddForceAtPosition(dir * pitchForceStrength, pitchForcePoint.position);
        }

        if (pitchMirroredForcePoint != null)
        {
            Vector3 pos = pitchMirroredForcePoint.localPosition;
            pos.z = -offset;
            pitchMirroredForcePoint.localPosition = pos;

            Vector3 dir = pitchMirroredForcePoint.TransformDirection(Vector3.up);
            targetRigidbody.AddForceAtPosition(dir * pitchForceStrength, pitchMirroredForcePoint.position);
        }
    }

    void ApplyYawControl()
    {
        float input = yawInput?.action?.ReadValue<float>() ?? 0f;
        float offset = Mathf.Lerp(yawMin, yawMax, (input + 1f) * 0.5f);

        ApplyControlSurfaceVisuals(yawControlSurfaceVisuals, input);

        if (yawForcePoint != null)
        {
            Vector3 pos = yawForcePoint.localPosition;
            pos.z = offset;
            yawForcePoint.localPosition = pos;

            Vector3 dir = yawForcePoint.TransformDirection(Vector3.right);
            targetRigidbody.AddForceAtPosition(dir * yawForceStrength, yawForcePoint.position);
        }

        if (yawMirroredForcePoint != null)
        {
            Vector3 pos = yawMirroredForcePoint.localPosition;
            pos.z = -offset;
            yawMirroredForcePoint.localPosition = pos;

            Vector3 dir = yawMirroredForcePoint.TransformDirection(Vector3.left);
            targetRigidbody.AddForceAtPosition(dir * yawForceStrength, yawMirroredForcePoint.position);
        }
    }

    void ApplyEngineThrust()
    {
        foreach (var engine in engines)
        {
            if (engine.thrustPoint == null || engine.thrustInput == null) continue;

            float input = Mathf.Clamp(engine.thrustInput.action.ReadValue<float>(), -1f, 1f);
            engine.targetThrust = Mathf.Lerp(engine.minThrust, engine.maxThrust, (input + 1f) * 0.5f);

            float rate = (engine.targetThrust > engine.currentThrust) ? engine.accelerateRate : engine.decelerateRate;
            engine.currentThrust = Mathf.MoveTowards(engine.currentThrust, engine.targetThrust, rate * Time.fixedDeltaTime);

            Vector3 totalDirection = Vector3.zero;
            if (engine.pushUp) totalDirection += engine.thrustPoint.up;
            if (engine.pushDown) totalDirection += -engine.thrustPoint.up;
            if (engine.pushRight) totalDirection += engine.thrustPoint.right;
            if (engine.pushLeft) totalDirection += -engine.thrustPoint.right;
            if (engine.pushForward) totalDirection += engine.thrustPoint.forward;
            if (engine.pushBack) totalDirection += -engine.thrustPoint.forward;

            if (totalDirection != Vector3.zero)
            {
                totalDirection.Normalize();
                targetRigidbody.AddForceAtPosition(totalDirection * engine.currentThrust, engine.thrustPoint.position, ForceMode.Force);
            }

            if (engine.enableThrustVector && engine.thrustVectorInput != null)
            {
                float vectorInput = Mathf.Clamp(engine.thrustVectorInput.action.ReadValue<float>(), -1f, 1f);
                float aoa = Mathf.Lerp(engine.minAOA, engine.maxAOA, (vectorInput + 1f) * 0.5f);
                Vector3 currentEuler = engine.thrustPoint.localEulerAngles;
                engine.thrustPoint.localRotation = Quaternion.Euler(aoa, currentEuler.y, currentEuler.z);
            }
        }
    }

    void ApplyControlSurfaceVisuals(ControlSurfaceVisual[] visuals, float input)
    {
        if (visuals == null || visuals.Length == 0) return;

        float t = (input + 1f) * 0.5f;

        for (int i = 0; i < visuals.Length; i++)
        {
            var v = visuals[i];
            if (v == null || v.targetTransform == null) continue;

            float deflect = Mathf.Lerp(v.minDeflectAngle, v.maxDeflectAngle, t);

            Vector3 e = v.targetTransform.localEulerAngles;

            float x = e.x;
            float y = e.y;
            float z = e.z;

            if (v.rotateLocalX) x = deflect;
            if (v.rotateLocalY) y = deflect;
            if (v.rotateLocalZ) z = deflect;

            v.targetTransform.localEulerAngles = new Vector3(x, y, z);
        }
    }
}
