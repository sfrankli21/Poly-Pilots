using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// The container for all flow primitives that exist in a scene and are not contained in a Fluid Volume.
    /// All Flow Affected objects will be affected by the Global Fluid unless they specifically opt to ignore the Global Fluid.
    /// </summary>
    public static class GlobalFluid
    {
        private static List<FlowPrimitive> globalFlowPrimitives = new List<FlowPrimitive>();

        //public delegate Vector3 VelocityEventHandler(Vector3 position);

        //public static event VelocityEventHandler VelocityEvent = null;

        //private static Delegate[] eventHandlers;
        private static Vector3 velocity;

        /// <summary>
        /// Used to check what flow primitives are in the global list. It is not recommended to try and change this list.
        /// </summary>
        /// <returns>The list of globally acting flow primitives</returns>
        public static List<FlowPrimitive> GetFlowPrimitiveList() { return globalFlowPrimitives; }

        public static void AddFlowPrimitive(FlowPrimitive flowPrimitive)
        {
            if (globalFlowPrimitives.Contains(flowPrimitive))
            {
                return;
            }

            globalFlowPrimitives.Add(flowPrimitive);
        }

        public static void RemoveFlowPrimitive(FlowPrimitive flowPrimitive)
        {
            globalFlowPrimitives.Remove(flowPrimitive);
        }

        // I think we can use a delegate to pass the velocity functions of multiple fluid zones over to a particle system

        public static List<VelocityEventHandler> GetInteractingVelocityFunctions(int flowAffectedID)
        {
            List<VelocityEventHandler> velocityFunctions = new List<VelocityEventHandler>();
            for (int i = 0; i < globalFlowPrimitives.Count; i++)
            {
                if (FlowInteractionManager.IsInteractionIgnored(flowAffectedID, globalFlowPrimitives[i].interactionID))
                {
                    continue;
                }

                velocityFunctions.Add(globalFlowPrimitives[i].VelocityFunction);
            }

            return velocityFunctions;
        }

        public static List<VelocityEventHandler> GetInteractingVelocityFunctions(int flowAffectedID, List<VelocityEventHandler> velocityFunctions)
        {
            for (int i = 0; i < globalFlowPrimitives.Count; i++)
            {
                if (FlowInteractionManager.IsInteractionIgnored(flowAffectedID, globalFlowPrimitives[i].interactionID))
                {
                    continue;
                }

                velocityFunctions.Add(globalFlowPrimitives[i].VelocityFunction);
            }

            return velocityFunctions;
        }

        public static Vector3 GetVelocity(Vector3 position, int flowAffectedID)
        {
            velocity = new Vector3(0, 0, 0);

            if (globalFlowPrimitives != null)
            {
                for (int i = 0; i < globalFlowPrimitives.Count; i++)
                {
                    velocity += globalFlowPrimitives[i].VelocityFunction(position, flowAffectedID);
                }
            }

            return velocity;
        }

        public static Vector3 GetVelocity(Vector3 position)
        {
            velocity = new Vector3(0, 0, 0);

            if (globalFlowPrimitives != null)
            {
                for (int i = 0; i < globalFlowPrimitives.Count; i++)
                {
                    velocity += globalFlowPrimitives[i].VelocityFunction(position);
                }
            }

            return velocity;
        }

        //private static GlobalFluid instance;

        //public static GlobalFluid Instance => instance;

        private static FluidProperties _fluidProperties;

        /// <summary>
        /// Properties of the global fluid.
        /// </summary>
        public static FluidProperties FluidProperties
        {
            get
            {
                if (_fluidProperties == null)
                {
                    _fluidProperties = Resources.Load("GlobalFluidProperties") as FluidProperties;
                }

                return _fluidProperties;
            }
            set => _fluidProperties = value;
        }
    }
}
