using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example controller for a large airship.
    /// </summary>
    public class Airship : BaseAircraftController
    {
        public Rigidbody aircraftRigidbody, PortEngineRigidbody, StarboardEngineRigidbody;
        public Transform CGMarker, StarboardEngineGimbal, PortEngineGimbal;
        public float elevatorGain, rudderGain, engineSpeedGain;
        public ControlSurface elevator, rudder;
        public AeroObject airSpeedSensor;
        [Range(-45, 45)]
        public float propellerPitchAngle;
        [HideInInspector]
        public float engineSpeedInput, verticalInput;
        public float engineYawGain;
        public Transform blade1Geometry, blade2Geometry, blade3Geometry, blade4Geometry;
        public float elevatorTrim, verticalTrim, lateralTrim;
        public float fixedDeltaTime;
        InputAction engineSpeedAction, verticalAction;
        public float throttleSensitivity;

        public override float AirSpeed => airSpeedSensor.localRelativeVelocity.magnitude;
        public override float Altitude => transform.position.y;
        public override float Throttle => engineSpeedInput * 100;

        public override void Awake()
        {
            base.Awake();
            engineSpeedAction = playerInput.actions.FindAction("Engine Speed");
            verticalAction = playerInput.actions.FindAction("Vertical");

        }
        void Start()
        {
            aircraftRigidbody.centerOfMass = CGMarker.localPosition;
            blade1Geometry.localEulerAngles = blade2Geometry.localEulerAngles = blade3Geometry.localEulerAngles = blade4Geometry.localEulerAngles = new Vector3(0, propellerPitchAngle, 0);
            Time.fixedDeltaTime = fixedDeltaTime;// physics update rate    
        }

        void FixedUpdate()
        {
            ReadControlInputs();
            ApplyControlInputs();
        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            engineSpeedInput += engineSpeedAction.ReadValue<float>() * throttleSensitivity * Time.fixedDeltaTime;
            engineSpeedInput = Mathf.Clamp(engineSpeedInput, -1f, 1f);
            verticalInput += verticalAction.ReadValue<float>() * 0.1f * controlSensitivity * Time.fixedDeltaTime;
        }

        void ApplyControlInputs()
        {
            elevator.deflectionAngle = elevatorGain * pitchDemand;
            rudder.deflectionAngle = rudderGain * yawDemand;
            PortEngineRigidbody.angularVelocity =
                PortEngineRigidbody.transform.TransformDirection(new Vector3(0, 0, (engineYawGain * yawDemand) + (engineSpeedGain * engineSpeedInput)));
            StarboardEngineRigidbody.angularVelocity =
                StarboardEngineRigidbody.transform.TransformDirection(new Vector3(0, 0, (-engineYawGain * yawDemand) + (engineSpeedGain * engineSpeedInput)));

            StarboardEngineGimbal.transform.localEulerAngles = PortEngineGimbal.transform.localEulerAngles =
               90 * new Vector3(-verticalInput, 0, 0);
        }
    }
}
