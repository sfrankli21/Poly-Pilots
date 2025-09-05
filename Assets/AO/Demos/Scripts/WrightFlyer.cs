using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example controller for a Wright flyer style aircraft.
    /// </summary>
    public class WrightFlyer : BaseAircraftController
    {
        public Rigidbody aircraftRigidbody, portEngineRigidbody, starboardEngineRigidbody;
        public Transform CoMMarker, blade1Geometry, blade2Geometry, blade3Geometry, blade4Geometry;
        public float aileronGain, elevatorGain, rudderGain, engineSpeedGain;
        public ControlSurface portAileronLower, starboardAileronLower, portAileronUpper, starboardAileronUpper;
        public AeroObject airSpeedSensor;
        public Transform forePlane, portFin, starboardFin;
        [Range(-45, 45)]
        public float propellerPitchAngle;
        float pitchTrim, engineSpeedInput;
        InputAction engineSpeedAction, pitchTrimAction;
        public float throttleSensitivity;

        public override void Awake()
        {
            base.Awake();
            engineSpeedAction = playerInput.actions.FindAction("Engine Speed");
            pitchTrimAction = playerInput.actions.FindAction("Pitch Trim");
        }

        void Start()
        {
            aircraftRigidbody.centerOfMass = CoMMarker.localPosition;
            blade1Geometry.localEulerAngles = blade2Geometry.localEulerAngles = new Vector3(0, propellerPitchAngle, 0);
            blade3Geometry.localEulerAngles = blade4Geometry.localEulerAngles = new Vector3(0, -propellerPitchAngle, 0);
        }

        public override float AirSpeed => airSpeedSensor.localRelativeVelocity.magnitude;
        public override float Altitude => transform.position.y;
        public override float Throttle => engineSpeedInput * 100;

        void FixedUpdate()
        {
            ReadControlInputs();
            ApplyControlInputs();
        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            pitchTrim += pitchTrimAction.ReadValue<float>() * 0.1f * controlSensitivity * Time.fixedDeltaTime;
            pitchTrim = Mathf.Clamp(pitchTrim, -1f, 1f);
            engineSpeedInput += engineSpeedAction.ReadValue<float>() * 0.1f * throttleSensitivity * Time.fixedDeltaTime;
            engineSpeedInput = Mathf.Clamp(engineSpeedInput, 0f, 1f);
        }

        void ApplyControlInputs()
        {
            portAileronLower.deflectionAngle = portAileronUpper.deflectionAngle = aileronGain * rollDemand;
            starboardAileronLower.deflectionAngle = starboardAileronUpper.deflectionAngle = -aileronGain * rollDemand;
            forePlane.localEulerAngles = new Vector3(-4 + (elevatorGain * (pitchTrim + pitchDemand)), 0, 0);
            portFin.localEulerAngles = starboardFin.localEulerAngles = new Vector3(0, rudderGain * yawDemand, 90);
            portEngineRigidbody.angularVelocity = portEngineRigidbody.transform.TransformDirection(new Vector3(0, 0, -engineSpeedInput * engineSpeedGain));
            starboardEngineRigidbody.angularVelocity = starboardEngineRigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeedInput * engineSpeedGain));
        }
    }
}
