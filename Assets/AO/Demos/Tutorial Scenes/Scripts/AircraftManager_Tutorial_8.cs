using UnityEngine;
using UnityEngine.InputSystem;
using AerodynamicObjects;

namespace AerodynamicObjects.Tutorials
{
    [System.Serializable]
    public struct PropHubEntry
    {
        public Transform hub;
        public bool counterClockwise;
    }

    public class AircraftManager_Tutorial_8 : MonoBehaviour
    {
        public Transform centreOfMassMarker;
        Rigidbody aircraftRigidBody;

        public PropHubEntry[] propHubs;
        public Rigidbody[] engineRigidbodies;

        public float maxRPM = 2000f;
        public float engineSpeedGain = 200f;

        public ControlSurface portAileron;
        public ControlSurface starboardAileron;
        public ControlSurface elevator;
        public ControlSurface rudder;

        public float maxRollDeflectionDeg = 15f;
        public float maxPitchDeflectionDeg = 15f;
        public float maxYawDeflectionDeg = 15f;
        public float controlResponsiveness = 8f;

        PlayerInput playerInput;
        InputAction rollAction;
        InputAction pitchAction;
        InputAction yawAction;
        InputAction throttleAction;

        float rollInput, pitchInput, yawInput, throttleInput;
        float propSpeed;

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
        }

        void FixedUpdate()
        {
            throttleInput = throttleAction.ReadValue<float>();
            rollInput = Mathf.MoveTowards(rollInput, rollAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);
            pitchInput = Mathf.MoveTowards(pitchInput, pitchAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);
            yawInput = Mathf.MoveTowards(yawInput, yawAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);

            propSpeed = Mathf.Clamp(propSpeed + (0.2f * throttleInput), 0f, maxRPM);

            if (propHubs != null)
            {
                float baseStep = -15f * propSpeed * Time.fixedDeltaTime;
                for (int i = 0; i < propHubs.Length; i++)
                {
                    var entry = propHubs[i];
                    if (!entry.hub) continue;
                    float step = entry.counterClockwise ? -baseStep : baseStep;
                    entry.hub.localRotation *= Quaternion.Euler(0f, 0f, step);
                }
            }

            if (engineRigidbodies != null)
            {
                Vector3 av = new Vector3(0f, 0f, throttleInput * engineSpeedGain);
                for (int i = 0; i < engineRigidbodies.Length; i++)
                {
                    var rb = engineRigidbodies[i];
                    if (rb) rb.angularVelocity = rb.transform.TransformDirection(av);
                }
            }

            float rollMaxRad = Mathf.Deg2Rad * maxRollDeflectionDeg;
            float pitchMaxRad = Mathf.Deg2Rad * maxPitchDeflectionDeg;
            float yawMaxRad = Mathf.Deg2Rad * maxYawDeflectionDeg;

            if (portAileron) portAileron.deflectionAngle = rollMaxRad * rollInput;
            if (starboardAileron) starboardAileron.deflectionAngle = -rollMaxRad * rollInput;
            if (elevator) elevator.deflectionAngle = pitchMaxRad * pitchInput;
            if (rudder) rudder.deflectionAngle = yawMaxRad * yawInput;
        }
    }
}
