using System;
using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Fluid volumes are used to contain and manage flow primitives so that they only affect objects within the defined volume.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [AddComponentMenu("Aerodynamic Objects/Flow/Fluid Volume", 31)]
    public class FluidVolume : MonoBehaviour
    {
        /// <summary>
        /// Defines the shape of the volume.
        /// </summary>
        public enum Shape
        {
            Box,
            Sphere,
            Capsule,
            Mesh
        }

        public Vector3 boxSize = new Vector3(1, 1, 1);
        public float sphereRadius = 0.5f;
        public float capsuleRadius = 0.5f;
        public float capsuleHeight = 1f;

        public Shape shape = Shape.Box;

        private new Collider collider;
        public FluidProperties fluidProperties;
        public FlowPrimitive[] flowPrimitives = new FlowPrimitive[0];

        /// <summary>
        /// The unique ID number for this fluid volume, used for identifying pairs of ignored interactions by the FlowInteractionManager.
        /// </summary>
        public int interactionID = 0;

        /// <summary>
        /// Obtains a unique interaction ID for the object.
        /// Should be called when the object is created.
        /// </summary>
        public void GetInteractionID()
        {
            if (interactionID == 0)
            {
                interactionID = FlowInteractionManager.GetUniqueID();
            }
        }

        public void OnEnable()
        {
            SetColliderRuntime();
            GetFluidPrimitives();
        }

        private void Awake()
        {
            GetInteractionID();
        }

        private void OnDestroy()
        {
            ReleasePrimitives();
        }

        private void OnDisable()
        {
            ReleasePrimitives();
        }

        void ReleasePrimitives()
        {
            if (flowPrimitives == null)
            {
                return;
            }

            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (flowPrimitives[i] != null)
                {
                    flowPrimitives[i].AddSelfToGlobalFluid();
                }
            }

            flowPrimitives = null;
        }

        public void RemovePrimitive(FlowPrimitive flowPrimitive)
        {
            int index = -1;

            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (flowPrimitives[i] == flowPrimitive)
                {
                    index = i;
                    break;
                }
            }

            if (index == -1)
            {
                return;
            }

            FlowPrimitive[] newFlowPrimitives = new FlowPrimitive[flowPrimitives.Length - 1];

            if (index > 0)
            {
                Array.Copy(flowPrimitives, 0, newFlowPrimitives, 0, index);
            }

            if (index < flowPrimitives.Length - 1)
            {
                Array.Copy(flowPrimitives, index + 1, newFlowPrimitives, index, flowPrimitives.Length - index - 1);
            }

            flowPrimitives = newFlowPrimitives;
        }

        public void AddPrimitive(FlowPrimitive flowPrimitive)
        {
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (flowPrimitives[i] == flowPrimitive)
                {
                    // The primitive is already stored in the volume
                    return;
                }
            }

            FlowPrimitive[] newFlowPrimitives = new FlowPrimitive[flowPrimitives.Length + 1];

            Array.Copy(flowPrimitives, 0, newFlowPrimitives, 0, flowPrimitives.Length);
            newFlowPrimitives[newFlowPrimitives.Length - 1] = flowPrimitive;

            flowPrimitives = newFlowPrimitives;
        }

        void GetFluidPrimitives()
        {
            flowPrimitives = GetComponentsInChildren<FlowPrimitive>();
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                flowPrimitives[i].AcquireFluidVolume(this);
            }
        }

        public virtual void Reset()
        {
            fluidProperties = GlobalFluid.FluidProperties;
            //SetColliderEditor();
        }

        public void ApplyColliderProperties()
        {
            collider = GetComponent<Collider>();

            switch (shape)
            {
                case Shape.Box:
                    ((BoxCollider)collider).size = boxSize;
                    break;

                case Shape.Sphere:
                    ((SphereCollider)collider).radius = sphereRadius;
                    break;

                case Shape.Capsule:
                    ((CapsuleCollider)collider).radius = capsuleRadius;
                    ((CapsuleCollider)collider).height = capsuleHeight;
                    break;

                case Shape.Mesh:
                    ((MeshCollider)collider).convex = true;
                    break;
            }

            if (collider)
            {
                collider.isTrigger = true;
            }
        }

        public void SetColliderRuntime()
        {
            collider = GetComponent<Collider>();

            switch (shape)
            {
                case Shape.Box:
                    if (collider.GetType() != typeof(BoxCollider))
                    {
                        Destroy(collider);
                        collider = gameObject.AddComponent<BoxCollider>();
                    }

                    break;
                case Shape.Sphere:
                    if (collider.GetType() != typeof(SphereCollider))
                    {
                        Destroy(collider);
                        collider = gameObject.AddComponent<SphereCollider>();
                    }

                    break;
                case Shape.Capsule:
                    if (collider.GetType() != typeof(CapsuleCollider))
                    {
                        Destroy(collider);
                        collider = gameObject.AddComponent<CapsuleCollider>();
                    }

                    break;
                case Shape.Mesh:
                    if (collider.GetType() != typeof(MeshCollider))
                    {
                        Destroy(collider);
                        collider = gameObject.AddComponent<MeshCollider>();
                    }

                    break;
                default:
                    break;
            }

            ApplyColliderProperties();
        }

        public void SetColliderEditor()
        {
            collider = GetComponent<Collider>();

            switch (shape)
            {
                case Shape.Box:
                    if (collider.GetType() != typeof(BoxCollider))
                    {
                        EditorDestroy(collider);
                        collider = gameObject.AddComponent<BoxCollider>();
                    }

                    break;
                case Shape.Sphere:
                    if (collider.GetType() != typeof(SphereCollider))
                    {
                        EditorDestroy(collider);
                        collider = gameObject.AddComponent<SphereCollider>();
                    }

                    break;
                case Shape.Capsule:
                    if (collider.GetType() != typeof(CapsuleCollider))
                    {
                        EditorDestroy(collider);
                        collider = gameObject.AddComponent<CapsuleCollider>();
                    }

                    break;
                case Shape.Mesh:
                    if (collider.GetType() != typeof(MeshCollider))
                    {
                        EditorDestroy(collider);
                        collider = gameObject.AddComponent<MeshCollider>();
                    }

                    break;
                default:
                    break;
            }

            ApplyColliderProperties();
        }

        public virtual bool IsPositionInsideZone(Vector3 position)
        {
            // This sucks but I can't find a better way to cheaply do this kind of detection
            return collider.bounds.Contains(position);
        }

        Vector3 velocity = Vector3.zero;

        /// <summary>
        /// Calculate the velocity of all flow primitives contained in this fluid volume - no checks for if they interact.
        /// </summary>
        /// <param name="position">Position of the object in the global (earth) frame of reference.</param>
        /// <returns>The velocity of the fluid at the given position in the global (earth) frame.</returns>
        public Vector3 VelocityFunction(Vector3 position)
        {
            velocity = new Vector3(0, 0, 0);
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                velocity += flowPrimitives[i].VelocityFunction(position);
            }

            return velocity;
        }

        public bool IsInteractionIgnored(int flowAffectedID)
        {
            return FlowInteractionManager.IsInteractionIgnored(flowAffectedID, interactionID);
        }

        public void IgnoreInteraction(int flowAffectedID)
        {
            FlowInteractionManager.IgnoreInteraction(flowAffectedID, interactionID);
        }

        /// <summary>
        /// Calculate the velocity of all flow primitives contained in this fluid volume including checks for if each flow primitive should interact with the provided ID.
        /// </summary>
        /// <param name="position">Position of the object in the global (earth) frame of reference.</param>
        /// <param name="flowAffectedID">The interaction ID of the object which is being affected by this fluid volume.</param>
        /// <returns>The velocity of the fluid at the given position in the global (earth) frame.</returns>
        /// </summary>
        public Vector3 VelocityFunction(Vector3 position, int flowAffectedID)
        {
            velocity = new Vector3(0, 0, 0);
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                velocity += flowPrimitives[i].VelocityFunction(position, flowAffectedID);
            }

            return velocity;
        }

        public Vector3 VelocityFunction(Vector3 position, int flowAffectedID, List<FlowPrimitive> interactingFlowPrimitives)
        {

            velocity = new Vector3(0, 0, 0);
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                velocity += flowPrimitives[i].VelocityFunction(position, flowAffectedID);
            }

            return velocity;
        }

        public List<VelocityEventHandler> GetInteractingVelocityFunctions(int flowAffectedID)
        {
            List<VelocityEventHandler> velocityFunctions = new List<VelocityEventHandler>();
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (FlowInteractionManager.IsInteractionIgnored(flowAffectedID, flowPrimitives[i].interactionID))
                {
                    continue;
                }

                velocityFunctions.Add(flowPrimitives[i].VelocityFunction);
            }

            return velocityFunctions;
        }

        /// <summary>
        /// Iterates over the list of flow primitives and adds each primitive which will interact with the provided ID.
        /// This function is useful when you need to cache multiple interaction checks before getting velocities.
        /// For example, when a particle system is getting the velocities for each particle, the interaction check is
        /// done at the Flow Particles level with a single ID for the whole collection of particles.
        /// </summary>
        /// <param name="flowAffectedID">ID of the flow affected object to check the primitives agains</param>
        /// <returns>List of flow primitives from the fluid volume which interact with the ID.</returns>
        public List<FlowPrimitive> GetInteractingPrimitives(int flowAffectedID)
        {
            List<FlowPrimitive> interactingPrimitives = new List<FlowPrimitive>();
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (FlowInteractionManager.IsInteractionIgnored(flowAffectedID, flowPrimitives[i].interactionID))
                {
                    continue;
                }

                interactingPrimitives.Add(flowPrimitives[i]);
            }

            return interactingPrimitives;
        }

        /// <summary>
        /// Iterates over the list of flow primitives and adds each primitive which will interact with the provided ID.
        /// This function is useful when you need to cache multiple interaction checks before getting velocities.
        /// For example, when a particle system is getting the velocities for each particle, the interaction check is
        /// done at the Flow Particles level with a single ID for the whole collection of particles.
        /// </summary>
        /// <param name="flowAffectedID">ID of the flow affected object to check the primitives agains</param>
        /// <returns>List of flow primitives from the fluid volume which interact with the ID.</returns>
        public List<FlowPrimitive> GetInteractingPrimitives(int flowAffectedID, List<FlowPrimitive> interactingPrimitives)
        {
            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                if (FlowInteractionManager.IsInteractionIgnored(flowAffectedID, flowPrimitives[i].interactionID))
                {
                    continue;
                }

                interactingPrimitives.Add(flowPrimitives[i]);
            }

            return interactingPrimitives;
        }

        /// <summary>
        /// Override this function to change the way fluid properties are calculated. E.g. to add a pressure gradient.
        /// </summary>
        /// <param name="aeroObject">The aerodynamic object for which the fluid properties are relevant.</param>
        public virtual void UpdateFluidProperties(AeroObject aeroObject)
        {
            // An example use for this could be to include a change in density and pressure with height of the aeroObject
            aeroObject.fluid.density = fluidProperties.density;
            aeroObject.fluid.dynamicViscosity = fluidProperties.dynamicViscosity;
            aeroObject.fluid.pressure = fluidProperties.pressure;
        }

        public virtual void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.TryGetComponent(out FlowAffected fluidInteractiveObject))
            {
                fluidInteractiveObject.SubscribeToFluidVolume(this);
            }
        }

        public virtual void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out FlowAffected fluidInteractiveObject))
            {
                fluidInteractiveObject.UnsubscribeFromFluidVolume(this);
            }
        }

        //public virtual void OnValidate()
        //{
        //    if (Application.isPlaying)
        //    {
        //        SetColliderRuntime();
        //    }
        //    else
        //    {
        //        SetColliderEditor();
        //    }
        //}

        void EditorDestroy(UnityEngine.Object obj)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                DestroyImmediate(obj);
            };
        }
    }
}
