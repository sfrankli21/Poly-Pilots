using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example controller for a hang glider.
    /// </summary>
    public class HangGlider : BaseAircraftController
    {
        public Rigidbody aircraftRigidbody;
        public Transform CGMarker;
        public float aileronGain, elevatorGain;
        public AeroObject airSpeedSensor;
        public ControlSurface portAileron, starboardAileron;
        public float pitchTrim;

        InputAction pitchTrimAction;

        public override void Awake()
        {
            base.Awake();
            pitchTrimAction = playerInput.actions.FindAction("Pitch Trim");
        }

        void Start()
        {
            aircraftRigidbody.centerOfMass = CGMarker.localPosition;
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
        }

        void ApplyControlInputs()
        {
            portAileron.deflectionAngle = (elevatorGain * (pitchTrim + pitchDemand)) + (aileronGain * rollDemand);
            starboardAileron.deflectionAngle = (elevatorGain * (pitchTrim + pitchDemand)) - (aileronGain * rollDemand);
        }

        public override float AirSpeed => airSpeedSensor.localRelativeVelocity.magnitude;
        public override float Altitude => transform.position.y;
    }
}
