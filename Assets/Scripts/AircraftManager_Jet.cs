using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using AerodynamicObjects;

namespace AerodynamicObjects.Tutorials
{
    [System.Serializable]
    public struct SurfaceElement
    {
        public string name;
        public bool isControlSurface;
        public ControlSurface controlSurface;
        public Transform pivot;
        public Vector3 localAxis;
        public float minDeflectionDeg;
        public float maxDeflectionDeg;
        [HideInInspector] public Quaternion initialLocalRotation;
        [HideInInspector] public float currentAngleDeg;
    }

    [System.Serializable]
    public class EngineElement
    {
        public Transform thrustPoint;

        [Header("Thrust (kN)")]
        public float minThrust;
        public float maxThrust;

        [Header("Thrust Vectoring")]
        public bool enableThrustVector;
        public float minAOA;
        public float maxAOA;

        [Header("Push Directions")]
        public bool pushUp;
        public bool pushDown;
        public bool pushLeft;
        public bool pushRight;
        public bool pushForward;
        public bool pushBack;
    }

    public class AircraftManager_Jet : MonoBehaviour
    {
        const float KnotsPerMS = 1.9438444924406f; // m/s -> kt

        [Header("Mass & Physics")]
        public Transform centreOfMassMarker;
        private Rigidbody aircraftRigidBody;

        [Header("Engines")]
        public EngineElement[] portEngines;
        public EngineElement[] starboardEngines;

        public float throttleCurveGamma = 1.0f;
        public float engineResponsePerSec = 2.0f;

        [Header("UI (Optional)")]
        public TextMeshProUGUI leftThrottleSetpointText;
        public TextMeshProUGUI rightThrottleSetpointText;

        [Header("Wing Ailerons (per-side)")]
        public SurfaceElement[] portWingAilerons;
        public SurfaceElement[] starboardWingAilerons;

        [Header("Horizontal Stabilisers (Tailerons, per-side)")]
        public SurfaceElement[] portHorizontalStabilisers;
        public SurfaceElement[] starboardHorizontalStabilisers;

        [Header("Vertical Stabilisers (Rudders, per-side)")]
        public SurfaceElement[] portVerticalStabilisers;
        public SurfaceElement[] starboardVerticalStabilisers;

        [Header("Deflection Limits for ControlSurface path (deg)")]
        public float maxPitchDeflectionDeg = 20f;
        public float maxRollDeflectionDeg = 15f;
        public float maxYawDeflectionDeg = 20f;

        [Header("Response & Mixing")]
        public float controlResponsiveness = 8f;
        [Range(0f, 1f)] public float rollToWings = 1.0f;
        [Range(0f, 1f)] public float rollToTailerons = 0.5f;
        public bool normalizeRollMix = true;

        [Header("Direct-Rotate path (isControlSurface = false)")]
        public float surfaceRotationSpeedDegPerSec = 120f;

        [Header("Trim Target (X Rotation in Degrees)")]
        public Transform trimTarget;
        public float trimSlewDegPerSec = 60f;
        public bool useTrimCurve = true;
        public AnimationCurve trimCurveSpeedToTrim = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(500f, 0f),
            new Keyframe(1000f, 0f),
            new Keyframe(1500f, 0f)
        );
        public bool clampCurveX = true; // clamp X (speed) to first/last key
        public float manualTrimDegrees = 0f; // used when useTrimCurve == false

        private float trimCurrentDeg;

        private PlayerInput playerInput;
        private InputAction rollAction, pitchAction, yawAction;
        private InputAction leftEnginesAction, rightEnginesAction;
        private InputAction thrustVectorLeftAction, thrustVectorRightAction;

        private float rollInput, pitchInput, yawInput;

        private float leftThrottleSetpoint;
        private float rightThrottleSetpoint;
        private float leftThrottleOutput;
        private float rightThrottleOutput;

        [Header("Runtime – Thrust Vector Inputs")]
        [Range(-1f, 1f)] public float leftThrustVectorInput;
        [Range(-1f, 1f)] public float rightThrustVectorInput;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            rollAction = playerInput.actions.FindAction("Roll");
            pitchAction = playerInput.actions.FindAction("Pitch");
            yawAction = playerInput.actions.FindAction("Yaw");
            leftEnginesAction = playerInput.actions.FindAction("LeftEngines");
            rightEnginesAction = playerInput.actions.FindAction("RightEngines");
            thrustVectorLeftAction = playerInput.actions.FindAction("ThrustVectorLeft");
            thrustVectorRightAction = playerInput.actions.FindAction("ThrustVectorRight");
            rollAction?.Enable();
            pitchAction?.Enable();
            yawAction?.Enable();
            leftEnginesAction?.Enable();
            rightEnginesAction?.Enable();
            thrustVectorLeftAction?.Enable();
            thrustVectorRightAction?.Enable();
        }

        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();
            if (centreOfMassMarker) aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;

            CacheInitialRotations(portWingAilerons);
            CacheInitialRotations(starboardWingAilerons);
            CacheInitialRotations(portHorizontalStabilisers);
            CacheInitialRotations(starboardHorizontalStabilisers);
            CacheInitialRotations(portVerticalStabilisers);
            CacheInitialRotations(starboardVerticalStabilisers);

            if (leftThrottleSetpointText) leftThrottleSetpointText.text = leftThrottleSetpoint.ToString("0.00");
            if (rightThrottleSetpointText) rightThrottleSetpointText.text = rightThrottleSetpoint.ToString("0.00");

            trimCurrentDeg = (trimTarget ? trimTarget.localEulerAngles.x : 0f);
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            float resp = Mathf.Max(0.0001f, controlResponsiveness);

            float rollTarget = rollAction.ReadValue<float>();
            float pitchTarget = pitchAction.ReadValue<float>();
            float yawTarget = yawAction.ReadValue<float>();

            rollInput = Mathf.MoveTowards(rollInput, rollTarget, resp * dt);
            pitchInput = Mathf.MoveTowards(pitchInput, pitchTarget, resp * dt);
            yawInput = Mathf.MoveTowards(yawInput, yawTarget, resp * dt);

            float leftRaw = Mathf.Clamp(leftEnginesAction.ReadValue<float>(), -1f, 1f);
            float rightRaw = Mathf.Clamp(rightEnginesAction.ReadValue<float>(), -1f, 1f);
            leftThrottleSetpoint = 0.5f * (leftRaw + 1f);
            rightThrottleSetpoint = 0.5f * (rightRaw + 1f);

            if (leftThrottleSetpointText) leftThrottleSetpointText.text = leftThrottleSetpoint.ToString("0.00");
            if (rightThrottleSetpointText) rightThrottleSetpointText.text = rightThrottleSetpoint.ToString("0.00");

            float spool = Mathf.Max(0f, engineResponsePerSec);
            leftThrottleOutput = Mathf.MoveTowards(leftThrottleOutput, leftThrottleSetpoint, spool * dt);
            rightThrottleOutput = Mathf.MoveTowards(rightThrottleOutput, rightThrottleSetpoint, spool * dt);

            float shapedLeft = (throttleCurveGamma <= 0.0001f) ? leftThrottleOutput : Mathf.Pow(Mathf.Clamp01(leftThrottleOutput), throttleCurveGamma);
            float shapedRight = (throttleCurveGamma <= 0.0001f) ? rightThrottleOutput : Mathf.Pow(Mathf.Clamp01(rightThrottleOutput), throttleCurveGamma);

            leftThrustVectorInput = Mathf.Clamp(thrustVectorLeftAction.ReadValue<float>(), -1f, 1f);
            rightThrustVectorInput = Mathf.Clamp(thrustVectorRightAction.ReadValue<float>(), -1f, 1f);

            if (aircraftRigidBody)
            {
                ApplyEngines(portEngines, shapedLeft, leftThrustVectorInput);
                ApplyEngines(starboardEngines, shapedRight, rightThrustVectorInput);
            }

            float rollMaxRad = Mathf.Deg2Rad * maxRollDeflectionDeg;
            float pitchMaxRad = Mathf.Deg2Rad * maxPitchDeflectionDeg;
            float yawMaxRad = Mathf.Deg2Rad * maxYawDeflectionDeg;

            float wingsMix = rollToWings;
            float tailsMix = rollToTailerons;
            if (normalizeRollMix)
            {
                float sum = Mathf.Max(1e-6f, wingsMix + tailsMix);
                wingsMix /= sum;
                tailsMix /= sum;
            }

            float rollCmdBaseNorm = Mathf.Clamp(rollInput, -1f, 1f);
            float pitchCmdNorm = Mathf.Clamp(pitchInput, -1f, 1f);
            float yawCmdNorm = Mathf.Clamp(yawInput, -1f, 1f);

            // Trim: exact X rotation from curve (degrees)
            if (trimTarget)
            {
                float targetDeg;
                if (useTrimCurve && aircraftRigidBody != null)
                {
                    Vector3 v = aircraftRigidBody.linearVelocity; // or .linearVelocity if that's your API
                    float forwardMS = Mathf.Max(0f, Vector3.Dot(v, transform.forward));
                    float speedKnots = forwardMS * KnotsPerMS;
                    targetDeg = EvaluateTrimAtSpeed(speedKnots);
                }
                else
                {
                    targetDeg = manualTrimDegrees;
                }


                float slew = Mathf.Max(0f, trimSlewDegPerSec);
                trimCurrentDeg = Mathf.MoveTowardsAngle(trimCurrentDeg, targetDeg, slew * dt);

                Vector3 eul = trimTarget.localEulerAngles;
                trimTarget.localEulerAngles = new Vector3(trimCurrentDeg, eul.y, eul.z);
            }

            float rollCmdWingsNorm = Mathf.Clamp(rollCmdBaseNorm * Mathf.Clamp01(wingsMix), -1f, 1f);
            float rollCmdWingsRad = rollMaxRad * rollCmdWingsNorm;
            ApplySurfaceCommands(portWingAilerons, rollCmdWingsRad, rollCmdWingsNorm, false);
            ApplySurfaceCommands(starboardWingAilerons, -rollCmdWingsRad, -rollCmdWingsNorm, false);

            float rollCmdTailNorm = Mathf.Clamp(rollCmdBaseNorm * Mathf.Clamp01(tailsMix), -1f, 1f);
            float pitchCmdRad = pitchMaxRad * pitchCmdNorm;
            float rollTailRad = rollMaxRad * rollCmdTailNorm;

            ApplySurfaceCommands(portHorizontalStabilisers, pitchCmdRad + rollTailRad, Mathf.Clamp(pitchCmdNorm + rollCmdTailNorm, -1f, 1f), true);
            ApplySurfaceCommands(starboardHorizontalStabilisers, pitchCmdRad - rollTailRad, Mathf.Clamp(pitchCmdNorm - rollCmdTailNorm, -1f, 1f), true);

            float yawCmdRad = yawMaxRad * yawCmdNorm;
            ApplySurfaceCommands(portVerticalStabilisers, yawCmdRad, yawCmdNorm, false);
            ApplySurfaceCommands(starboardVerticalStabilisers, yawCmdRad, yawCmdNorm, false);
        }

        float EvaluateTrimAtSpeed(float speedKnots)
        {
            if (trimCurveSpeedToTrim == null || trimCurveSpeedToTrim.length == 0) return 0f;
            if (!clampCurveX) return trimCurveSpeedToTrim.Evaluate(speedKnots);

            var keys = trimCurveSpeedToTrim.keys;
            float minX = keys[0].time;
            float maxX = keys[keys.Length - 1].time;
            float x = Mathf.Clamp(speedKnots, minX, maxX);
            return trimCurveSpeedToTrim.Evaluate(x);
        }

        private void ApplyEngines(EngineElement[] engines, float throttle01, float sideVectorInput)
        {
            if (engines == null || aircraftRigidBody == null) return;

            for (int i = 0; i < engines.Length; i++)
            {
                var e = engines[i];
                if (e == null || e.thrustPoint == null) continue;

                float thrustN = Mathf.Lerp(e.minThrust, e.maxThrust, Mathf.Clamp01(throttle01)) * 1000f;

                Vector3 dir = Vector3.zero;
                if (e.pushUp) dir += e.thrustPoint.up;
                if (e.pushDown) dir += -e.thrustPoint.up;
                if (e.pushRight) dir += e.thrustPoint.right;
                if (e.pushLeft) dir += -e.thrustPoint.right;
                if (e.pushForward) dir += e.thrustPoint.forward;
                if (e.pushBack) dir += -e.thrustPoint.forward;

                if (dir != Vector3.zero && thrustN > 0f)
                {
                    dir.Normalize();
                    aircraftRigidBody.AddForceAtPosition(dir * thrustN, e.thrustPoint.position, ForceMode.Force);
                }

                if (e.enableThrustVector)
                {
                    float v = Mathf.Clamp(sideVectorInput, -1f, 1f);
                    float aoa = Mathf.Lerp(e.minAOA, e.maxAOA, (v + 1f) * 0.5f);
                    Vector3 current = e.thrustPoint.localEulerAngles;
                    e.thrustPoint.localRotation = Quaternion.Euler(aoa, current.y, current.z);
                }
            }
        }

        private void ApplySurfaceCommands(SurfaceElement[] elems, float angleRad, float norm, bool instant)
        {
            if (elems == null) return;
            float slew = Mathf.Max(0f, surfaceRotationSpeedDegPerSec);
            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < elems.Length; i++)
            {
                var e = elems[i];
                if (e.isControlSurface)
                {
                    if (e.controlSurface) e.controlSurface.deflectionAngle = angleRad;
                }
                else
                {
                    if (e.pivot)
                    {
                        float t = 0.5f * (Mathf.Clamp(norm, -1f, 1f) + 1f);
                        float targetDeg = Mathf.Lerp(e.minDeflectionDeg, e.maxDeflectionDeg, t);
                        e.currentAngleDeg = instant ? targetDeg : Mathf.MoveTowardsAngle(e.currentAngleDeg, targetDeg, slew * dt);
                        Vector3 axis = (e.localAxis.sqrMagnitude < 1e-6f) ? Vector3.right : e.localAxis.normalized;
                        e.pivot.localRotation = e.initialLocalRotation * Quaternion.AngleAxis(e.currentAngleDeg, axis);
                    }
                }
                elems[i] = e;
            }
        }

        private void CacheInitialRotations(SurfaceElement[] elems)
        {
            if (elems == null) return;
            for (int i = 0; i < elems.Length; i++)
            {
                var e = elems[i];
                if (e.pivot)
                {
                    e.initialLocalRotation = e.pivot.localRotation;
                    e.currentAngleDeg = 0f;
                }
                elems[i] = e;
            }
        }
    }
}
