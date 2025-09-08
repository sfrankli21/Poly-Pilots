using UnityEngine;
using UnityEngine.InputSystem;
using AerodynamicObjects; // ControlSurface

namespace AerodynamicObjects.Tutorials
{
    [System.Serializable]
    public struct SurfaceElement
    {
        [Tooltip("Optional label for clarity in the inspector.")]
        public string name;

        [Tooltip("If checked, this element uses the ControlSurface component below.")]
        public bool isControlSurface; // <-- Checkbox: "Control Surface"

        [Tooltip("Used when isControlSurface = true.")]
        public ControlSurface controlSurface;

        [Tooltip("Pivot to rotate when isControlSurface = false.")]
        public Transform pivot;

        [Tooltip("Local axis around which to rotate the pivot when isControlSurface = false.")]
        public Vector3 localAxis;

        [Tooltip("Minimum angle (deg) when input = -1, used when isControlSurface = false.")]
        public float minDeflectionDeg;

        [Tooltip("Maximum angle (deg) when input = +1, used when isControlSurface = false.")]
        public float maxDeflectionDeg;

        // Runtime state (serialized so you can see values in Debug inspector)
        [HideInInspector] public Quaternion initialLocalRotation;
        [HideInInspector] public float currentAngleDeg;
    }

    public class AircraftManager_Jet : MonoBehaviour
    {
        [Header("Mass & Physics")]
        public Transform centreOfMassMarker;
        private Rigidbody aircraftRigidBody;

        [Header("Engines (per-side)")]
        [Tooltip("Each Transform's +Z is thrust direction; force is applied at that position.")]
        public Transform[] portEngines;
        public Transform[] starboardEngines;

        [Tooltip("Max thrust PER ENGINE, in Newtons (N).")]
        public float maxThrustPerEngine = 60000f;

        [Tooltip("1 = linear throttle, <1 snappier low-end, >1 softer low-end.")]
        public float throttleCurveGamma = 1.0f;

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
        [Tooltip("How quickly inputs reach their target values (rad/s).")]
        public float controlResponsiveness = 8f;

        [Tooltip("Roll share to WING ailerons (0..1).")]
        [Range(0f, 1f)] public float rollToWings = 1.0f;

        [Tooltip("Roll share to TAILERONS (0..1).")]
        [Range(0f, 1f)] public float rollToTailerons = 0.5f;

        [Tooltip("If true, rollToWings + rollToTailerons normalized to 1 at runtime.")]
        public bool normalizeRollMix = true;

        [Header("Direct-Rotate path (isControlSurface = false)")]
        [Tooltip("Slew rate for direct-rotated surfaces (deg/s).")]
        public float surfaceRotationSpeedDegPerSec = 120f;

        // Input System
        private PlayerInput playerInput;
        private InputAction rollAction, pitchAction, yawAction, throttleAction;

        // Smoothed inputs
        private float rollInput, pitchInput, yawInput, throttleInput;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            rollAction = playerInput.actions.FindAction("Roll");
            pitchAction = playerInput.actions.FindAction("Pitch");
            yawAction = playerInput.actions.FindAction("Yaw");
            throttleAction = playerInput.actions.FindAction("Throttle");

            rollAction?.Enable();
            pitchAction?.Enable();
            yawAction?.Enable();
            throttleAction?.Enable();
        }

        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();
            if (centreOfMassMarker) aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;

            // Capture initial rotations for all non-engine arrays
            CacheInitialRotations(portWingAilerons);
            CacheInitialRotations(starboardWingAilerons);
            CacheInitialRotations(portHorizontalStabilisers);
            CacheInitialRotations(starboardHorizontalStabilisers);
            CacheInitialRotations(portVerticalStabilisers);
            CacheInitialRotations(starboardVerticalStabilisers);
        }

        void FixedUpdate()
        {
            // Read & smooth inputs
            float dt = Time.fixedDeltaTime;
            float resp = Mathf.Max(0.0001f, controlResponsiveness);

            float rollTarget = rollAction.ReadValue<float>();
            float pitchTarget = pitchAction.ReadValue<float>();
            float yawTarget = yawAction.ReadValue<float>();
            float throttleTarget = Mathf.Clamp01(throttleAction.ReadValue<float>());

            rollInput = Mathf.MoveTowards(rollInput, rollTarget, resp * dt);
            pitchInput = Mathf.MoveTowards(pitchInput, pitchTarget, resp * dt);
            yawInput = Mathf.MoveTowards(yawInput, yawTarget, resp * dt);
            throttleInput = Mathf.MoveTowards(throttleInput, throttleTarget, resp * dt);

            // Throttle shaping
            float shapedThrottle = (throttleCurveGamma <= 0.0001f)
                ? throttleInput
                : Mathf.Pow(Mathf.Clamp01(throttleInput), throttleCurveGamma);

            // Engines: thrust per engine (N) along +Z
            if (aircraftRigidBody)
            {
                ApplyThrustArray(portEngines, shapedThrottle);
                ApplyThrustArray(starboardEngines, shapedThrottle);
            }

            // Convert control-surface limits to radians
            float rollMaxRad = Mathf.Deg2Rad * maxRollDeflectionDeg;
            float pitchMaxRad = Mathf.Deg2Rad * maxPitchDeflectionDeg;
            float yawMaxRad = Mathf.Deg2Rad * maxYawDeflectionDeg;

            // Roll mixing between wings & tailerons
            float wingsMix = rollToWings;
            float tailsMix = rollToTailerons;
            if (normalizeRollMix)
            {
                float sum = Mathf.Max(1e-6f, wingsMix + tailsMix);
                wingsMix /= sum;
                tailsMix /= sum;
            }

            // Command signals
            float rollCmdBaseNorm = Mathf.Clamp(rollInput, -1f, 1f);
            float pitchCmdNorm = Mathf.Clamp(pitchInput, -1f, 1f);
            float yawCmdNorm = Mathf.Clamp(yawInput, -1f, 1f);

            // 1) Wing ailerons: roll only
            float rollCmdWingsNorm = Mathf.Clamp(rollCmdBaseNorm * Mathf.Clamp01(wingsMix), -1f, 1f);
            float rollCmdWingsRad = rollMaxRad * rollCmdWingsNorm;
            ApplySurfaceCommands(portWingAilerons, rollCmdWingsRad, rollCmdWingsNorm);
            ApplySurfaceCommands(starboardWingAilerons, -rollCmdWingsRad, -rollCmdWingsNorm);

            // 2) Tailerons: pitch ± roll
            float rollCmdTailNorm = Mathf.Clamp(rollCmdBaseNorm * Mathf.Clamp01(tailsMix), -1f, 1f);
            float pitchCmdRad = pitchMaxRad * pitchCmdNorm;
            float rollTailRad = rollMaxRad * rollCmdTailNorm;

            // Port = pitch + roll ; Starboard = pitch - roll
            ApplySurfaceCommands(portHorizontalStabilisers, pitchCmdRad + rollTailRad, Mathf.Clamp(pitchCmdNorm + rollCmdTailNorm, -1f, 1f));
            ApplySurfaceCommands(starboardHorizontalStabilisers, pitchCmdRad - rollTailRad, Mathf.Clamp(pitchCmdNorm - rollCmdTailNorm, -1f, 1f));

            // 3) Rudders: yaw
            float yawCmdRad = yawMaxRad * yawCmdNorm;
            ApplySurfaceCommands(portVerticalStabilisers, yawCmdRad, yawCmdNorm);
            ApplySurfaceCommands(starboardVerticalStabilisers, yawCmdRad, yawCmdNorm);
        }

        // ================= Helpers =================

        private void ApplyThrustArray(Transform[] enginePoints, float throttle01)
        {
            if (enginePoints == null) return;
            float perEngineThrust = Mathf.Max(0f, maxThrustPerEngine) * Mathf.Clamp01(throttle01);

            for (int i = 0; i < enginePoints.Length; i++)
            {
                Transform t = enginePoints[i];
                if (!t) continue;

                Vector3 force = t.forward * perEngineThrust; // +Z thrust
                aircraftRigidBody.AddForceAtPosition(force, t.position, ForceMode.Force);
            }
        }

        /// <summary>
        /// Apply to a surface array. 
        /// angleRad: target angle (radians) for ControlSurface path.
        /// norm: normalized command (-1..1) for direct-rotate path (maps to min/max per element).
        /// </summary>
        private void ApplySurfaceCommands(SurfaceElement[] elems, float angleRad, float norm)
        {
            if (elems == null) return;
            float slew = Mathf.Max(0f, surfaceRotationSpeedDegPerSec);
            float dt = Time.fixedDeltaTime;

            for (int i = 0; i < elems.Length; i++)
            {
                var e = elems[i];

                if (e.isControlSurface)
                {
                    if (e.controlSurface)
                    {
                        e.controlSurface.deflectionAngle = angleRad; // radians
                    }
                }
                else
                {
                    if (e.pivot)
                    {
                        // Map -1..1 -> [min,max]
                        float t = 0.5f * (Mathf.Clamp(norm, -1f, 1f) + 1f);
                        float targetDeg = Mathf.Lerp(e.minDeflectionDeg, e.maxDeflectionDeg, t);

                        // Smooth toward target
                        e.currentAngleDeg = Mathf.MoveTowardsAngle(e.currentAngleDeg, targetDeg, slew * dt);

                        Vector3 axis = (e.localAxis.sqrMagnitude < 1e-6f) ? Vector3.right : e.localAxis.normalized;
                        e.pivot.localRotation = e.initialLocalRotation * Quaternion.AngleAxis(e.currentAngleDeg, axis);
                    }
                }

                elems[i] = e; // write back (struct)
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
                    // Initialize current to whatever the pivot already is relative to initial (assume 0)
                    e.currentAngleDeg = 0f;
                }
                elems[i] = e;
            }
        }
    }
}
