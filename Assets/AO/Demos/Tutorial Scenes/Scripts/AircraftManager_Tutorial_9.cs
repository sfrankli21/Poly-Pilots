using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Tutorials
{
    public class AircraftManager_Tutorial_9 : MonoBehaviour
    {
        public Transform centreOfMassMarker;
        Rigidbody aircraftRigidBody;
        float propSpeed;
        public Transform propHub;
        public AeroObject portWing, starboardWing, horizontalStabiliser, portVerticalStabiliser, starboardVerticalStabiliser;
        public float rollControlGain, pitchControlGain, yawControlGain, controlResponsiveness, maxRPM;
        PlayerInput playerInput;
        InputAction rollAction;
        InputAction pitchAction;
        InputAction yawAction;
        InputAction throttleAction;
        float pitchInput, rollInput, yawInput;

        private void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            rollAction = playerInput.actions.FindAction("Roll");
            pitchAction = playerInput.actions.FindAction("Pitch");
            yawAction = playerInput.actions.FindAction("Yaw");
            throttleAction = playerInput.actions.FindAction("Throttle");
        }

        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();

        }

        void FixedUpdate()
        {
            aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;

            //Propulsion
            propSpeed = Mathf.Clamp(propSpeed + (0.2f * throttleAction.ReadValue<float>()), 0, maxRPM);
            propHub.localRotation *= Quaternion.Euler(0, 0, -15 * propSpeed * Time.fixedDeltaTime);
            //Roll
            rollInput = Mathf.MoveTowards(rollInput, rollAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);
            //portWing.ao.ControlCamber = rollControlGain * rollInput;
            //starboardWing.ao.ControlCamber = -rollControlGain * rollInput;
            //Pitch
            pitchInput = Mathf.MoveTowards(pitchInput, pitchAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);
            //horizontalStabiliser.ao.ControlCamber = pitchControlGain * pitchInput;
            //Yaw
            yawInput = Mathf.MoveTowards(yawInput, yawAction.ReadValue<float>(), controlResponsiveness * Time.fixedDeltaTime);
            //portVerticalStabiliser.ao.ControlCamber = yawControlGain * yawInput;
            // starboardVerticalStabiliser.ao.ControlCamber = yawControlGain * yawInput;

        }

    }
}