using AerodynamicObjects.Aerodynamics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to calculate the aerodynamic forces and moments acting on an object based on geometry and flow velocity.
    /// </summary>
    [Serializable]
    [AddComponentMenu("Aerodynamic Objects/Aerodynamics/Aero Object")]
    public class AeroObject : FlowSensor
    {
        public bool hasDrag;
        public bool hasLift;
        public bool hasRotationalDamping;
        public bool hasRotationalLift;
        public bool hasBuoyancy;

        /// <summary>
        /// Set to true if the dimensions of this object will change during play mode. Having this set to false saves a fairly costly update to the frames of reference used by the aerodynamics.
        /// </summary>
        [Tooltip("Set to true if the dimensions of this object will change during play mode. Having this set to false saves a fairly costly update to the frames of reference used by the aerodynamics.")]
        public bool updateDimensionsInRuntime = false;

        /// <summary>
        /// Used to descripe what shape the reference area of an aerodynamic object is.
        /// </summary>
        public enum ReferenceAreaShape
        {
            Ellipse,
            Rectangle
        }

        /// <summary>
        /// What shape should reference areas use. This is useful when an AO represents a lifting surface and the area should be rectangular. If we took an ellipse with major and minor axes equivalent to a rectangle's width and height, the ellipsoid's area would be 1/(4*PI) times smaller than the rectangular area.
        /// </summary>
        [Tooltip("What shape should reference areas use. This is useful when an AO represents a lifting surface and the area should be rectangular. If we took an ellipse with major and minor axes equivalent to a rectangle's width and height, the ellipsoid's area would be 1/(4*PI) times smaller than the rectangular area.")]
        public ReferenceAreaShape referenceAreaShape = ReferenceAreaShape.Ellipse;

        /// <summary>
        /// The aerodynamic group that this object belongs to. Groups control the aspect ratio when aero objects are used to create a wing, ensuring that sensible values of lift are produced.
        /// </summary>
        [Tooltip("The aerodynamic group that this object belongs to. Groups control the aspect ratio when aero objects are used to create a wing, ensuring that sensible values of lift are produced.")]
        public AeroGroup myGroup;

        // ================= Camber and Controls ===================
        /// <summary>
        /// Body camber is the camber due to the shape of the object.
        /// This does not include the camber due to control surface deflection. (m)
        /// </summary>
        [Obsolete("Body Camber is no longer stored as a single value, it is now defined for each axis of the object independently.", true)]
        public float BodyCamber;

        /// <summary>
        /// All the control surfaces affecting the aero object.
        /// These are added automatically by including a component on the same game object as this Aero Object.
        /// </summary>
        public List<ControlSurface> controlSurfaces = new List<ControlSurface>();

        /// <summary>
        /// Total camber is the combination of body camber and camber due to control surface deflection. (m) (Read Only)
        /// </summary>
        [Obsolete("Total Camber is no longer used. Control surfaces now contribute a chord and angle of deflection and are used directly by the lift and drag models.", true)]
        public float TotalCamber => BodyCamber + ControlCamber;

        /// <summary>
        /// Control camber is the camber due to the deflection of the control surface attached to the  (m)
        /// </summary>
        [Obsolete("Control Camber is no longer used. Control surfaces now contribute a chord and angle of deflection and are used directly by the lift and drag models.", true)]
        public float ControlCamber;

        /// <summary>
        /// Overall size of the object in the local frame of reference. Accounting for relative dimensions and the scale of this object's transform. (m)
        /// </summary>
        [Tooltip("Overall size of the object in the local frame of reference. Accounting for relative dimensions and the scale of this object's transform. (m)")]
        public Vector3 dimensions;

        /// <summary>
        ///  The dimensions of the object in the local frame of reference, relative to the scale of the object's transform. I.e. relative dimensions of (2,2,2) will yield an aero object twice the size of the transform object.
        /// </summary>
        [Tooltip(" The dimensions of the object in the local frame of reference, relative to the scale of the object's transform. I.e. relative dimensions of (2,2,2) will yield an aero object twice the size of the transform object.")]
        public Vector3 relativeDimensions = Vector3.one;

        /// <summary>
        /// Camber of the object along each axis in the local frame of reference. (m)
        /// </summary>
        [Tooltip("Camber of the object along each axis in the local frame of reference. (m)")]
        public Vector3 camber;

        /// <summary>
        /// The collection of aerodynamic models which will be applied to this object.
        /// </summary>
        public IAerodynamicModel[] aerodynamicModels = new IAerodynamicModel[0];

        /// <summary>
        /// The aerodynamic loads acting on the object.
        /// Given by the aerodynamic models attached to the object.
        /// <br>In the local frame of reference.</br>
        /// </summary>
        public AerodynamicLoad[] aerodynamicLoads = new AerodynamicLoad[0];

        /// <summary>
        /// The sum of all the aerodynamic loads acting on the object.
        /// Given by the aerodynamic models attached to the object.
        /// <br>In the local frame of reference.</br>
        /// </summary>
        public AerodynamicLoad netAerodynamicLoad = new AerodynamicLoad();

        /// <summary>
        /// The dynamic pressure due to motion of the object through the fluid.
        /// Given by 0.5 * rho * V^2 (Pa)
        /// </summary>
        public float dynamicPressure;

        public override void Awake()
        {
            base.Awake();
            Initialise();

            // Quick warning if the object should be using a rigid body but it doesn't have one
            if (velocitySource == VelocitySource.Rigidbody && rb == null)
            {
                Debug.LogWarning("Expecting a rigidbody component to be assigned for the AeroObject on " + gameObject.name + " for the velocity source, but one has not been assigned.");
            }
        }

        public void OnValidate()
        {
            UpdateDimensions();
        }

        private void Reset()
        {
            AddCheckedModels();
        }

        /// <summary>
        /// Adds the specified models to the object and performs any dimension-related calculations.
        /// Also stores the current transform position and rotation ready for use in velocity calculations which use the object's transform (used when useTransformVelocity is true).
        /// </summary>
        public void Initialise()
        {
            AddCheckedModels();

            UpdateDimensions();

            // Make sure to set the previous rotation and position so that they aren't zero
            // which could lead to huge velocities in the first time step!
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }

        Vector3 groupSpanAxis;
        public Vector3 groupDimensions;
        /// <summary>
        /// Update the dimensions for the aero object using its transform scale and the relative dimensions.
        /// Then update the dimension values for any aerodynamic models associated with this aero object.
        /// </summary>
        public void UpdateDimensions()
        {
            // Dimensions for an individual aero object
            dimensions = Vector3.Scale(transform.lossyScale, relativeDimensions);

            // Now we want to include the group span if we have one
            if (myGroup != null)
            {
                // Get the group span
                groupSpanAxis = transform.InverseTransformDirection(myGroup.GlobalSpanAxis);

                // Use the rejection of dimensions on the span axis to combine the object's dimensions with the group span dimension
                groupDimensions = dimensions - (Vector3.Dot(dimensions, groupSpanAxis) * groupSpanAxis) + (myGroup.span * groupSpanAxis);
            }
            else
            {
                // Set the group dimensions to be the same, this way the models don't need to check if there is a group or not
                groupDimensions = dimensions;
            }

            // A body can have no models attached so if that happens we just do nothing here.
            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                return;
            }

            // Tell the models to update their dimension-dependent values now that we have updated the aero object's dimensions
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                aerodynamicModels[i].UpdateDimensionValues(this);
            }
        }

        /// <summary>
        /// Set the velocity and local velocity of the object. Assumes the angular velocity is zero and will set the values accordingly.
        /// </summary>
        /// <param name="velocity">The translational velocity for the object in the global frame of reference. (m/s)</param>
        public void SetVelocity(Vector3 velocity)
        {
            this.velocity = velocity;
            localVelocity = transform.InverseTransformDirection(velocity);
            angularVelocity = Vector3.zero;
            localAngularVelocity = Vector3.zero;
        }

        /// <summary>
        /// Set the position and rotation of the object's transform. Also stores the position and rotation in case the transform is being used
        /// to determine velocity.
        /// </summary>
        /// <param name="position">Position of the object in the global frame of reference. (m)</param>
        /// <param name="rotation">Quaternion rotation of the object in the global frame of reference.</param>
        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            previousPosition = position;
            previousRotation = rotation;
        }

        /// <summary>
        /// The net aerodynamic force acting on this object in the global frame of reference.
        /// </summary>
        public Vector3 GlobalNetForce()
        {
            return transform.TransformDirection(netAerodynamicLoad.force);
        }

        /// <summary>
        /// The net aerodynamic force acting on this object in the local frame of reference.
        /// </summary>
        public Vector3 LocalNetForce()
        {
            return netAerodynamicLoad.force;
        }

        /// <summary>
        /// The net aerodynamic torque acting on this object in the global frame of reference.
        /// </summary>
        public Vector3 GlobalNetTorque()
        {
            return transform.TransformDirection(netAerodynamicLoad.moment);
        }

        /// <summary>
        /// The net aerodynamic torque acting on this object in the local frame of reference.
        /// </summary>
        public Vector3 LocalNetTorque()
        {
            return netAerodynamicLoad.moment;
        }

        /// <summary>
        /// Resolve the net force on this aero object into lift and drag using the local relative velocity.
        /// </summary>
        /// <returns>The lift and drag forces in the object's local frame of reference.</returns>
        public (Vector3 lift, Vector3 drag) GetLocalLiftAndDrag()
        {
            Vector3 windDirection = localRelativeVelocity.normalized;
            Vector3 drag = Vector3.Dot(netAerodynamicLoad.force, windDirection) * windDirection;
            Vector3 lift = netAerodynamicLoad.force - drag;
            return (lift, drag);
        }

        /// <summary>
        /// Resolve the net force on this aero object into lift and drag using the global relative velocity.
        /// </summary>
        /// <returns>The lift and drag forces in the global frame of reference.</returns>
        public (Vector3 lift, Vector3 drag) GetGlobalLiftAndDrag()
        {
            Vector3 windDirection = relativeVelocity.normalized;
            Vector3 drag = Vector3.Dot(transform.TransformDirection(netAerodynamicLoad.force), windDirection) * windDirection;
            Vector3 lift = netAerodynamicLoad.force - drag;
            return (lift, drag);
        }

        /// <summary>
        /// If the Aero Object has a drag model, calculates and returns the pressure drag coefficient.
        /// </summary>
        /// <returns>Pressure drag coefficient</returns>
        public float GetDragCoefficient()
        {
            if (!hasDrag)
            {
                return 0f;
            }

            return GetModel<DragModel>().GetDragCoefficient();
        }

        /// <summary>
        /// Uses the resolved net force of the object in the wind direction and the provided reference area to determine a drag coefficient.
        /// </summary>
        /// <returns>Net drag coefficient</returns>
        public float GetDragCoefficient(float referenceArea)
        {
            // Dot product provides the magnitude of the net force in the direction of the fluid velocity (drag force)
            // Then we divide by 0.5 * rho * V^2 S to get CD
            return Vector3.Dot(netAerodynamicLoad.force, localRelativeVelocity.normalized) / (dynamicPressure * referenceArea);
        }

        /// <summary>
        /// If the Aero Object has a lift model, returns the lift coefficient stored in the lift model.
        /// </summary>
        /// <returns>Lift coefficient</returns>
        public float GetLiftCoefficient()
        {
            if (!hasLift)
            {
                return 0f;
            }

            return GetModel<LiftModel>().CL;
        }

        /// <summary>
        /// Uses the resolved net force of the object orthogonal to the wind direction and the provided reference area to determine a lift coefficient.
        /// </summary>
        /// <returns>Net lift coefficient</returns>
        public float GetLiftCoefficient(float referenceArea)
        {
            Vector3 windDirection = localRelativeVelocity.normalized;
            return (netAerodynamicLoad.force - (Vector3.Dot(netAerodynamicLoad.force, windDirection) * windDirection)).magnitude / (dynamicPressure * referenceArea);
        }

        /// <summary>
        /// Returns the position of the object's aerodynamic centre. If no lifting model is present, this is assumed to be the centre of the aero object
        /// </summary>
        /// <returns></returns>
        public Vector3 GetAerodynamicCentreGlobalPosition()
        {
            if (!hasLift)
            {
                return transform.position;
            }

            return transform.position + transform.TransformDirection(GetModel<LiftModel>().GetLocalAerodynamicCentre());
        }

        public override void FixedUpdate()
        {
            // Run the fixed update for the flow affected base class to determine the fluid velocity at the aero object's position
            // And for the flow sensor base class to determine the relative velocity of the aero object
            base.FixedUpdate();

            // Update the dimensions of the object. If the dimensions won't change then this can be called
            // once in Start or OnEnable
            if (updateDimensionsInRuntime)
            {
                UpdateDimensions();
            }

            // Run all the aerodynamic calculations
            RunAerodynamics();

            if (rb)
            {
                // Apply the resulting aerodynamic force to the rigid body - note the aero object transform might not align
                // with the rigid body transform!
                rb.AddForceAtPosition(GlobalNetForce(), transform.position);

                // Apply the resulting aerodynamic moment to the rigid body - we can't use relative torque here
                // because the aero object transform could be at a different orientation to the transform of the rigid body
                // i.e. they could be on separate game objects
                rb.AddTorque(GlobalNetTorque());
            }
        }

        /// <summary>
        /// Calculate the dynamic pressure, based on the body relative velocity.
        /// </summary>
        public void GetDynamicPressure()
        {
            // Calculate and store the dynamic pressure
            dynamicPressure = 0.5f * fluid.density * localRelativeVelocity.sqrMagnitude;
        }

        /// <summary>
        /// Iterate through all of the attached aerodynamic models and store the forces
        /// they provide for the body in the aerodynamicForces array.
        /// </summary>
        private void ComputeAerodynamicForces()
        {
            // No garbage creation here!
            netAerodynamicLoad.force.x = 0;
            netAerodynamicLoad.force.y = 0;
            netAerodynamicLoad.force.z = 0;
            netAerodynamicLoad.moment.x = 0;
            netAerodynamicLoad.moment.y = 0;
            netAerodynamicLoad.moment.z = 0;

            // A body can have no models attached so if that happens we just do nothing here.
            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                return;
            }

            // Make sure we can store as many forces as there are model components
            if (aerodynamicLoads.Length != aerodynamicModels.Length)
            {
                aerodynamicLoads = new AerodynamicLoad[aerodynamicModels.Length];
            }

            // Go through all of the aerodynamic model components and compute their forces
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                // Pass this object and the state of the object in the body frame
                aerodynamicLoads[i] = aerodynamicModels[i].GetAerodynamicLoad(this);

                // Keep the running total of the net force
                netAerodynamicLoad.force += aerodynamicLoads[i].force;
                netAerodynamicLoad.moment += aerodynamicLoads[i].moment;
            }
        }

        /// <summary>
        /// Perform all of the necessary calculations to find the aerodynamic forces acting on the object, based on the state of the object and the surrounding fluid.
        /// </summary>
        public void RunAerodynamics()
        {
            GetRelativeVelocities();
            GetDynamicPressure();
            ComputeAerodynamicForces();
        }

        /// <summary>
        /// Adds the given aerodynamic model to the aerodynamic object.
        /// If the object already has a model of the same type then an additional model will not be added.
        /// </summary>
        public void AddMonoBehaviourModel<T>() where T : MonoBehaviour, IAerodynamicModel, new()
        {

            if (gameObject.TryGetComponent<T>(out T modelComponent))
            {
                // The gameobject already has the model attached
                // Now we just need to make sure the AO has the model too

                if (aerodynamicModels == null || aerodynamicModels.Length == 0)
                {
                    aerodynamicModels = new IAerodynamicModel[] { modelComponent };
                    return;
                }

                for (int i = 0; i < aerodynamicModels.Length; i++)
                {
                    // Need to make sure we can cast before checking it's the correct model.
                    // This doesn't make sense to do both checks when we're removing just based on type and not instance but hey
                    if (aerodynamicModels[i].GetType() == typeof(T))
                    {
                        if (((T)aerodynamicModels[i]).Equals(modelComponent))
                        {
                            // The aerodynamic object already has this model
                            return;
                        }
                    }
                }

                // Increase the model array size
                Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
                // Add the new model
                aerodynamicModels[aerodynamicModels.Length - 1] = modelComponent;
            }
            else
            {
                // We need to add the model component first and then check
                modelComponent = gameObject.AddComponent<T>();

                if (aerodynamicModels == null || aerodynamicModels.Length == 0)
                {
                    aerodynamicModels = new IAerodynamicModel[] { modelComponent };
                    return;
                }

                // Just to be safe, we'll remove any existing monobehaviour models on the object
                //RemoveMonoBehaviourModel<T>();
                // This could cause problems if users want multiple instances of the same model on
                // one aero object. Though I can't really think of a case where this would make sense

                // Increase the model array size
                Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
                // Add the new model
                aerodynamicModels[aerodynamicModels.Length - 1] = modelComponent;
            }
        }

        /// <summary>
        /// Adds the given aerodynamic model to the aerodynamic object.
        /// If the object already has a model of the same type then an additional model will NOT be added.
        /// </summary>
        public void AddModel<T>() where T : IAerodynamicModel, new()
        {

            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                aerodynamicModels = new IAerodynamicModel[] { new T() };
                return;
            }

            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                if (aerodynamicModels[i].GetType() == typeof(T))
                {
                    // The aerodynamic object already has this type of model so return and do nothing
                    return;
                }
            }

            // Increase the model array size
            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
            // Add the new model
            aerodynamicModels[aerodynamicModels.Length - 1] = new T();
        }

        /// <summary>
        /// Looks for a monobehaviour component which implements the IAerodynamicModel interface and removes it from this object's game object.
        /// Also cleans up the list of aerodynamic models stored by this object.
        /// </summary>
        /// <typeparam name="T">The model type to remove.</typeparam>
        public void RemoveMonoBehaviourModel<T>() where T : MonoBehaviour, IAerodynamicModel, new()
        {

            if (gameObject.TryGetComponent(out T component))
            {
                DestroyImmediate(component);
            }

            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                return;
            }

            int removeID = -1;

            // Find the index of the model we want to remove
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                if (aerodynamicModels[i].GetType() == typeof(T))
                {
                    removeID = i;
                    break;
                }
            }

            // Check if we found the model or not
            if (removeID < 0)
            {
                return;
            }

            // Remove the model from the array

            // Shift all the elements
            for (int i = removeID; i < aerodynamicModels.Length - 1; i++)
            {
                aerodynamicModels[i] = aerodynamicModels[i + 1];
            }

            // Decrease the model array size
            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length - 1);
        }

        /// <summary>
        /// Remove the specified aerodynamic model from this object.
        /// </summary>
        /// <typeparam name="T">The model type to remove.</typeparam>
        public void RemoveModel<T>() where T : IAerodynamicModel, new()
        {
            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                return;
            }

            int removeID = -1;

            // Find the index of the model we want to remove
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                if (aerodynamicModels[i].GetType() == typeof(T))
                {
                    removeID = i;
                    break;
                }
            }

            // Check if we found the model or not
            if (removeID < 0)
            {
                return;
            }

            // Remove the model from the array

            // Shift all the elements
            for (int i = removeID; i < aerodynamicModels.Length - 1; i++)
            {
                aerodynamicModels[i] = aerodynamicModels[i + 1];
            }

            // Decrease the model array size
            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length - 1);
        }

        /// <summary>
        /// Remove all aerodynamic models from this object.
        /// </summary>
        public void ClearModels()
        {
            aerodynamicModels = new IAerodynamicModel[0];
        }

        /// <summary>
        /// Get the instance of the specified aerodynamic model for this object. Returns default (which should be null) if the specified model type is not stored by this object.
        /// </summary>
        /// <typeparam name="T">The model type to get.</typeparam>
        /// <returns>Instance of T for this object. Null if no model of type T is found.</returns>
        public T GetModel<T>() where T : IAerodynamicModel
        {
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                if (aerodynamicModels[i].GetType() == typeof(T))
                {
                    return (T)aerodynamicModels[i];
                }
            }

            return default;
        }

        /// <summary>
        /// Get the index of the specified aerodynamic model for this object in the aerodynamicModels array. Returns -1 if the specified model type is not stored by this object.
        /// </summary>
        /// <typeparam name="T">The model type to get.</typeparam>
        /// <returns>Index of T in the aerodynamicModels array for this object. -1 if no model of type T is found.</returns>
        public int GetModelIndex<T>() where T : IAerodynamicModel
        {
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                if (aerodynamicModels[i].GetType() == typeof(T))
                {
                    return i;
                }
            }

            return -1;
        }

        private void AddCheckedModels()
        {
            // There MUST be a better way to do this...
            if (hasDrag)
            {
                AddModel<DragModel>();
            }

            if (hasLift)
            {
                AddModel<LiftModel>();
            }

            if (hasRotationalDamping)
            {
                AddModel<RotationalDampingModel>();
            }

            if (hasRotationalLift)
            {
                AddModel<RotationalLiftModel>();
            }

            if (hasBuoyancy)
            {
                AddModel<BuoyancyModel>();
            }

            aerodynamicLoads = new AerodynamicLoad[aerodynamicModels.Length];
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;

            switch (referenceAreaShape)
            {
                case ReferenceAreaShape.Ellipse:
                    Gizmos.DrawWireMesh(GizmoMeshLoader.EllipsoidMesh, Vector3.zero, Quaternion.identity, relativeDimensions);

                    break;
                case ReferenceAreaShape.Rectangle:
                    Gizmos.DrawWireMesh(GizmoMeshLoader.CubeMesh, Vector3.zero, Quaternion.identity, relativeDimensions);

                    break;
                default:
                    break;
            }
        }
#endif

        /// <summary>
        /// Sets the velocity of the fluid around the object using a velocity in the global (earth) frame of reference.
        /// </summary>
        /// <param name="velocity">Velocity of the fluid in the global (earth) frame of reference. (m/s)</param>
        public void SetFluidVelocity(Vector3 velocity)
        {
            fluid.globalVelocity = velocity;
        }

        /// <summary>
        /// Add to the velocity of the fluid around the object. Velocity is measured in the global (earth) frame of reference.
        /// </summary>
        /// <param name="velocity">Velocity increment for the fluid in the global (earth) frame of reference. (m/s)</param>
        public void AddToFluidVelocity(Vector3 velocity)
        {
            fluid.globalVelocity += velocity;
        }

        // ===================== Surface Area ========================

        /// <summary>
        /// The approximate surface area of the ellipsoid  (m^2)
        /// </summary>
        public float GetEllipsoidSurfaceArea()
        {
            return 4f * Mathf.PI * Mathf.Pow(1f / 3f * (Mathf.Pow(0.25f * dimensions.x * dimensions.y, 1.6f)
                                                              + Mathf.Pow(0.25f * dimensions.x * dimensions.z, 1.6f)
                                                              + Mathf.Pow(0.25f * dimensions.y * dimensions.z, 1.6f)), 1f / 1.6f);
        }

        /// <summary>
        /// Calculate the angles of attack and sideslip for the object, based on its velocity.
        /// </summary>
        [Obsolete("Models have been updated and sideslip is no longer used. This function has been replaced by GetVelocityAnglesAndDynamicPressure.")]
        public void GetAnglesOfAttackAndSideslip()
        {

        }

        /// <summary>
        /// Get the global fluid velocity and use the properties of the global fluid for this object.
        /// </summary>
        [Obsolete("The global fluid velocity and properties are now determined by the base FlowAffected class.", true)]
        private void GetGlobalFluidEffects()
        {
            // This feels clunky
            AddToFluidVelocity(GlobalFluid.GetVelocity(transform.position, interactionID));
            fluid.pressure = GlobalFluid.FluidProperties.pressure;
            fluid.dynamicViscosity = GlobalFluid.FluidProperties.dynamicViscosity;
            fluid.density = GlobalFluid.FluidProperties.density;
        }

        [Obsolete("Dynamic pressure is no longer a function, it is cached during the get dynamic pressure function as part of the run aerodynamics process.")]
        public float DynamicPressure()
        {
            return 0.5f * fluid.density * localRelativeVelocity.sqrMagnitude;
        }
    }
}
