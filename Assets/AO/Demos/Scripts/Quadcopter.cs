using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example controller for a quadcopter.
    /// </summary>
    public class Quadcopter : BaseAircraftController
    {
        public Rigidbody aircraftRigidbody, engine1Rigidbody, engine2Rigidbody, engine3Rigidbody, engine4Rigidbody;
        public Transform CGMarker;
        public AeroObject airSpeedSensor;
        public float baseEngineSpeed;
        public float pitchGain, rollGain, yawGain, engineSpeedGain;
        float engineSpeed1, engineSpeed2, engineSpeed3, engineSpeed4;
        float engineSpeedInput;
        InputAction engineSpeedAction;
        public float throttleSensitivity;

        public override void Awake()
        {
            base.Awake();
            engineSpeedAction = playerInput.actions.FindAction("Engine Speed");
        }

        // Engine numbering: 1 aft, 2 fwd, 3 stb, 4 port
        void Start()
        {
            aircraftRigidbody.centerOfMass = CGMarker.localPosition;
        }

        void FixedUpdate()
        {
            ReadControlInputs();
            ApplyControlInputs();
        }

        void ApplyControlInputs()
        {
            baseEngineSpeed = engineSpeedGain * engineSpeedInput;
            engineSpeed3 = baseEngineSpeed + (rollGain * rollDemand);
            engineSpeed4 = baseEngineSpeed - (rollGain * rollDemand);
            engineSpeed1 = baseEngineSpeed + (pitchGain * pitchDemand);
            engineSpeed2 = baseEngineSpeed - (pitchGain * pitchDemand);
            aircraftRigidbody.AddRelativeTorque
                (new Vector3(0, yawGain * (yawDemand - aircraftRigidbody.transform.InverseTransformDirection(aircraftRigidbody.angularVelocity).y), 0));

            engine1Rigidbody.angularVelocity = engine1Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeed1));
            engine2Rigidbody.angularVelocity = engine2Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeed2));
            engine3Rigidbody.angularVelocity = engine3Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeed3));
            engine4Rigidbody.angularVelocity = engine4Rigidbody.transform.TransformDirection(new Vector3(0, 0, engineSpeed4));
        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            engineSpeedInput += engineSpeedAction.ReadValue<float>() * 0.1f * throttleSensitivity * Time.fixedDeltaTime;
            engineSpeedInput = Mathf.Clamp(engineSpeedInput, 0f, 1f);
        }

        public override float AirSpeed => airSpeedSensor.localRelativeVelocity.magnitude;
        public override float Altitude => transform.position.y;
        public override float Throttle => engineSpeedInput * 100;
    }
}
