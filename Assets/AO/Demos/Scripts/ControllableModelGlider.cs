using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example script for a model glider which has an actuated rudder and elevator.
    /// </summary>
    public class ControllableModelGlider : BaseAircraftController
    {
        //public float wingSpan, wingAspectRatio, aileronChordRatio, dihedralAngle, sweepAngle;
        //public Transform starboardWingDiehdralAndSweep, portWingDihedralAndSweep, starboardWingOffsetAndScale, portWingOffsetAndScale, starboardAileronRotation, portAileronRotation, starboardAileronOffsetAndScale, portAileronOffsetAndScale;
        public float flapAngle, slatAngle;
        public AeroObject portWing, starboardWing;

        // Please can these be properly named
        public ControlSurface pf, ps, sf, ss;
        public Transform pfHinge, psHinge, sfHinge, ssHinge;
        Quaternion pfHingeInitialRotation, psHingeInitialRotation, sfHingeInitialRotation, ssHingeInitialRotation;
        public Rigidbody aircraftRigidbody;
        public Transform CGMarker;
        public ControlSurface rudder, elevator;
        public AeroObject airSpeedSensor;
        public float elevatorGain, rudderGain;
        float pitchTrim, yawTrim;
        InputAction yawTrimAction, pitchTrimAction;

        public override void Awake()
        {
            base.Awake();
            yawTrimAction = playerInput.actions.FindAction("Yaw Trim");
            pitchTrimAction = playerInput.actions.FindAction("Pitch Trim");

            aircraftRigidbody.centerOfMass = CGMarker.localPosition;

            ControlSurface[] controlSurfaces = portWing.transform.GetComponents<ControlSurface>();
            pf = controlSurfaces[0];
            ps = controlSurfaces[1];

            controlSurfaces = starboardWing.transform.GetComponents<ControlSurface>();
            sf = controlSurfaces[0];
            ss = controlSurfaces[1];

            pfHingeInitialRotation = pfHinge.transform.rotation;
            sfHingeInitialRotation = sfHinge.transform.rotation;
            psHingeInitialRotation = psHinge.transform.rotation;
            ssHingeInitialRotation = ssHinge.transform.rotation;
        }

        void FixedUpdate()
        {
            ApplyControlInputs();

        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            pitchTrim += pitchTrimAction.ReadValue<float>() * controlSensitivity * Time.fixedDeltaTime;
            pitchTrim = Mathf.Clamp(pitchTrim, -1f, 1f);
            yawTrim += yawTrimAction.ReadValue<float>() * controlSensitivity * Time.fixedDeltaTime;
            yawTrim = Mathf.Clamp(yawTrim, -1f, 1f);
        }

        void ApplyControlInputs()
        {
            elevator.deflectionAngle = elevatorGain * pitchTrim;
            rudder.deflectionAngle = rudderGain * yawTrim;

            //extras to test control surfaces
            pf.deflectionAngle = sf.deflectionAngle = flapAngle / 57.4f;
            ps.deflectionAngle = ss.deflectionAngle = slatAngle / 57.4f;

            pfHinge.rotation = pfHingeInitialRotation * Quaternion.Euler(-flapAngle, 0, 0);
            sfHinge.rotation = sfHingeInitialRotation * Quaternion.Euler(-flapAngle, 0, 0);
            psHinge.rotation = psHingeInitialRotation * Quaternion.Euler(slatAngle, 0, 0);
            ssHinge.rotation = ssHingeInitialRotation * Quaternion.Euler(slatAngle, 0, 0);
        }
    }
}
