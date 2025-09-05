using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Base class for any aircraft controllers.
    /// Handles the roll pitch and yaw input collection. Also provides the airspeed altitude and throttle functions for the flight info display class.
    /// </summary>
    public class BaseAircraftController : MonoBehaviour
    {
        public virtual float AirSpeed { get; }
        public virtual float Altitude { get; }
        public virtual float Throttle { get; }

        protected PlayerInput playerInput;
        protected InputAction rollAction;
        protected InputAction pitchAction;
        protected InputAction yawAction;

        protected float yawDemand, pitchDemand, rollDemand;

        public float controlSensitivity;

        public virtual void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            rollAction = playerInput.actions.FindAction("Roll");
            pitchAction = playerInput.actions.FindAction("Pitch");
            yawAction = playerInput.actions.FindAction("Yaw");
        }

        public virtual void ReadControlInputs()
        {
            pitchDemand = Mathf.MoveTowards(pitchDemand, pitchAction.ReadValue<float>(), controlSensitivity * Time.fixedDeltaTime);
            rollDemand = Mathf.MoveTowards(rollDemand, rollAction.ReadValue<float>(), controlSensitivity * Time.fixedDeltaTime);
            yawDemand = Mathf.MoveTowards(yawDemand, yawAction.ReadValue<float>(), controlSensitivity * Time.fixedDeltaTime);
        }
    }
}
