using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Main code for controlling all aspects of the helicopter in the demo scene.
    /// </summary>
    public class Helicopter : BaseAircraftController
    {
        public Rigidbody fuselageRigidbody;
        public Transform CoMmarker;
        float heaveRateDemand;
        public float yawRateGain, heaveRateGain;
        public float pitchRateGain, rollRateGain, rotorSpeedGain;

        public TailRotorController tailRotorController;
        public MainRotorController mainRotorController;
        float mainRotorSpeedDemand;
        float bladePitchInput, bladeRollInput;
        public FlowSensor airSpeedSensor;
        public bool useRotorWakeModel = false;

        InputAction heaveAction;
        InputAction rotorSpeedAction;

        public override void Awake()
        {
            base.Awake();
            heaveAction = playerInput.actions.FindAction("Heave");
            rotorSpeedAction = playerInput.actions.FindAction("Rotor Speed");
        }

        void Start()
        {
            GetComponentInChildren<HelicopterRingVortexController>().gameObject.SetActive(useRotorWakeModel);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                SceneManager.LoadSceneAsync(SceneManager.GetActiveScene().name);
            }
        }

        void FixedUpdate()
        {
            ReadControlInputs();
            ApplyControlInputs();
        }

        public override void ReadControlInputs()
        {
            base.ReadControlInputs();
            heaveRateDemand += 0.02f * heaveAction.ReadValue<float>() * controlSensitivity * Time.fixedDeltaTime;
            mainRotorSpeedDemand += rotorSpeedAction.ReadValue<float>() * controlSensitivity * Time.fixedDeltaTime;
            mainRotorSpeedDemand = Mathf.Clamp(mainRotorSpeedDemand, 0, 100);
            bladePitchInput = pitchDemand;
            bladeRollInput = rollDemand;
        }

        void ApplyControlInputs()
        {
            mainRotorController.collective = heaveRateGain * heaveRateDemand;
            tailRotorController.angularVelocity = 10 * mainRotorController.hubAngularVelocity;
            mainRotorController.angularVelocityDemand = mainRotorSpeedDemand;
            tailRotorController.bladePitch = Mathf.Clamp(yawRateGain * (yawDemand - fuselageRigidbody.transform.InverseTransformDirection(fuselageRigidbody.angularVelocity).y), -20, 20);

            mainRotorController.cyclic = Mathf.Clamp(new Vector2(bladePitchInput, bladeRollInput).magnitude, -15, 15);
            mainRotorController.phase = Mathf.Rad2Deg * Mathf.Atan2(bladePitchInput, bladeRollInput);
        }

        public override float AirSpeed => airSpeedSensor.relativeVelocity.magnitude;

        public override float Altitude => transform.position.y;

        public override float Throttle => 100f * mainRotorSpeedDemand / rotorSpeedGain;
    }
}
