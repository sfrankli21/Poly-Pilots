using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// An example aircraft controller for a large transport aircraft.
    /// </summary>
    public class TransportAircraft : BaseAircraftController
    {
        public Rigidbody aircraftRigidbody, engine1Rigidbody, engine2Rigidbody, engine3Rigidbody, engine4Rigidbody;
        public Transform CoMMarker;

        public float aileronGain, elevatorGain, rudderGain, engineSpeedGain;
        public ControlSurface portAileron, starboardAileron, elevator, rudder;
        public AeroObject airSpeedSensor;
        public float throttleSensitivity;
        float pitchTrim, engineSpeedInput;

        InputAction engineSpeedAction, pitchTrimAction;

        public override void Awake()
        {
            base.Awake();
            engineSpeedAction = playerInput.actions.FindAction("Engine Speed");
            pitchTrimAction = playerInput.actions.FindAction("Pitch Trim");

        }
        void Start()
        {
            aircraftRigidbody.centerOfMass = CoMMarker.localPosition;
        }

        void FixedUpdate()
        {
            ReadControlInputs();
            ApplyControlInputs();
        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            pitchTrim += pitchTrimAction.ReadValue<float>() * 0.1f * controlSensitivity * Time.fixedDeltaTime;
            pitchTrim = Mathf.Clamp(pitchTrim, -0.1f, 0.1f);
            engineSpeedInput += engineSpeedAction.ReadValue<float>() * 0.1f * throttleSensitivity * Time.fixedDeltaTime;
            engineSpeedInput = Mathf.Clamp(engineSpeedInput, -0.5f, 1f);
        }

        void ApplyControlInputs()
        {
            portAileron.deflectionAngle = aileronGain * rollDemand;
            starboardAileron.deflectionAngle = -aileronGain * rollDemand;
            elevator.deflectionAngle = elevatorGain * (pitchTrim + pitchDemand);
            rudder.deflectionAngle = rudderGain * yawDemand;
            engine1Rigidbody.angularVelocity = engine1Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeedInput * engineSpeedGain));
            engine2Rigidbody.angularVelocity = engine2Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeedInput * engineSpeedGain));
            engine3Rigidbody.angularVelocity = engine3Rigidbody.transform.TransformDirection(new Vector3(0, 0, -engineSpeedInput * engineSpeedGain));
            engine4Rigidbody.angularVelocity = engine4Rigidbody.transform.TransformDirection(new Vector3(0, 0, -engineSpeedInput * engineSpeedGain));
        }

        public override float AirSpeed => airSpeedSensor.localRelativeVelocity.magnitude;
        public override float Altitude => transform.position.y;
        public override float Throttle => engineSpeedInput * 100;
    }
}
