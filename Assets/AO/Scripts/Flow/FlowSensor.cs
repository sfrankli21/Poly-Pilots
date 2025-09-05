using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to measure relative flow velocity using the velocity of the object and any interactable flow primitives in the scene.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Flow Sensor", -22)]
    public class FlowSensor : FlowAffected
    {
        public Rigidbody rb;

        /// <summary>
        /// Which component should this object use to determine its velocity. If transform is selected, the object will use changes in its transform to determine velocity and angular velocity. For rigidbody, the object will use the selected rigidbody to obtain its velocity.
        /// </summary>
        [Tooltip("Which component should this object use to determine its velocity. If transform is selected, the object will use changes in its transform to determine velocity and angular velocity. For rigidbody, the object will use the selected rigidbody to obtain its velocity.")]
        public VelocitySource velocitySource = VelocitySource.Rigidbody;

        // The position of the object in the global reference frame at the previous time step
        internal Vector3 previousPosition;

        // The rotation of the object in the global reference frame at the previous time step
        internal Quaternion currentRotation;
        internal Quaternion previousRotation;
        internal Vector3 orientationAxis;
        internal float rotationAngle = 0.0f;

        /// <summary>
        /// Velocity of the object in the global (earth) frame of reference. (m/s)
        /// </summary>
        public Vector3 velocity = Vector3.zero;

        /// <summary>
        /// Velocity of the object in the object's local frame of reference. (m/s)
        /// </summary>
        public Vector3 localVelocity = Vector3.zero;

        /// <summary>
        /// Velocity of the object relative to the fluid. Measured in the global (earth) frame of reference. (m/s)
        /// </summary>
        public Vector3 relativeVelocity = Vector3.zero;

        /// <summary>
        /// Velocity of the object relative to the fluid. Measured in the object's local frame of reference. (m/s)
        /// </summary>
        public Vector3 localRelativeVelocity = Vector3.zero;

        /// <summary>
        /// Angular velocity of the object in the global (earth) frame of reference. (m/s)
        /// </summary>
        public Vector3 angularVelocity = Vector3.zero;

        /// <summary>
        /// Angular velocity of the object in the object's local frame of reference. (m/s)
        /// </summary>
        public Vector3 localAngularVelocity = Vector3.zero;

        public override void FixedUpdate()
        {
            // The base flow affected class determines the velocity of the flow
            base.FixedUpdate();

            // This class determines the velocity of the object
            GetObjectVelocity();

            // Finally, we get the relative velocity of the object and the flow
            GetRelativeVelocities();
        }

        public override void Awake()
        {
            base.Awake();
            //SetRigidBodyAndCollider();
            previousPosition = transform.position;
        }

        /// <summary>
        /// Determine the velocity and angular velocity of the object. Can use either the assigned rigidbody or the transform of the object.
        /// </summary>
        public void GetObjectVelocity()
        {
            switch (velocitySource)
            {
                case VelocitySource.Rigidbody:

                    // Translational velocity from rigidbody
                    velocity = rb.GetPointVelocity(transform.position);

                    // Angular velocity from rigidbody
                    angularVelocity = rb.angularVelocity;
                    break;

                case VelocitySource.Transform:

                    // Translational velocity
                    velocity = (transform.position - previousPosition) / Time.fixedDeltaTime;

                    // Rotational velocity
                    currentRotation = transform.rotation;
                    (currentRotation * Quaternion.Inverse(previousRotation)).ToAngleAxis(out rotationAngle, out orientationAxis);
                    angularVelocity = Mathf.Deg2Rad * rotationAngle * orientationAxis / Time.fixedDeltaTime;
                    break;

            }

            // Store previous position and rotation so we can smoothly switch to transform velocity calculations
            previousPosition = transform.position;
            previousRotation = transform.rotation;

            localVelocity = transform.InverseTransformDirection(velocity);
            localAngularVelocity = transform.InverseTransformDirection(angularVelocity);
        }

        /// <summary>
        /// Calculate and store the velocity of the object relative to the surrounding fluid.
        /// Calculations are done in both global and local frames of reference.
        /// </summary>
        internal void GetRelativeVelocities()
        {
            fluid.localVelocity = transform.InverseTransformDirection(fluid.globalVelocity);
            relativeVelocity = velocity - fluid.globalVelocity;
            localRelativeVelocity = localVelocity - fluid.localVelocity;
        }

        //void Reset()
        //{
        //    SetRigidBodyAndCollider();
        //}

        ///// <summary>
        ///// Adds a kinematic rigid body to this game object. Checks if there is a collider attached, and if so will set the collider to be a trigger and warn the user.
        ///// If there is no collider, this will add a sphere collider and set it to be a trigger.
        ///// These components are required for the flow sensor to detect fluid zones without existing as a physical object which could be collided with by other rigid bodies in the scene.
        ///// </summary>
        //void SetRigidBodyAndCollider()
        //{
        //    GetComponent<Rigidbody>().isKinematic = true;
        //    GetComponent<Rigidbody>().mass = 1e-07f; // not necessary but visual cue to user that this is not used for dynamics
        //    GetComponent<Rigidbody>().angularDrag = 0;// not necessary but visual cue to user that this is not used for dynamics
        //    if (!TryGetComponent(out Collider existingCollider))
        //    {
        //        // Create and add a sphere collider and set it to be a trigger
        //        gameObject.AddComponent<SphereCollider>().isTrigger = true;
        //    }
        //    else
        //    {
        //        // If there's already a collider, set it to be a trigger and warn the user.
        //        if (existingCollider.isTrigger == false)
        //        {
        //            Debug.LogWarning("Flow sensor requires a collider component which is set to be a trigger. This has been done using an existing collider on " + gameObject.name);
        //            existingCollider.isTrigger = true;
        //        }
        //    }
        //}
    }
}
