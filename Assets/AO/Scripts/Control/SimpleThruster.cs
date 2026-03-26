using UnityEngine;

namespace AerodynamicObjects.Control
{
    /// <summary>
    /// A simple thrust actuator which applies a thrust force directly proportional to the input signal provided.
    /// This is considered simple as most thrusters will produce a force which is also proportional to their velocity as they are generally constrained by their power output.
    /// Propeller thrusters also produce a reaction torque which is not included in this simplified model.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Control/Simple Thruster")]
    public class SimpleThruster : Actuator
    {
        [Tooltip("The rigid body to apply the thrust to. Thrust is applied at the position of this thruster, not at the rigid body's centre of mass.")]
        public Rigidbody rb;

        [Tooltip("The local direction of the thrust force vector.")]
        public Vector3 thrustDirection = Vector3.forward;

        //show user - could be confusing to see this and not be able to change it.
        [HideInInspector]
        public Vector3 thrustVector;

        private void FixedUpdate()
        {
            // Update the control value using the fixed update - fixed delta time
            UpdateValue(Time.fixedDeltaTime);

            // Set the thrust based on the current control value
            thrustVector = CurrentValue * thrustDirection;
            thrustVector = transform.TransformDirection(thrustVector);
            rb.AddForceAtPosition(thrustVector, transform.position);

        }
    }
}
