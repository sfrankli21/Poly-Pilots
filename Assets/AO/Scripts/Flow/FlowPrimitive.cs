using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Flow primitives are used to define the velocity behaviour of the fluid.
    /// By default, flow primitives will affect the global velocity of the fluid, i.e. they will affect every IFluidInteractive object in the scene.
    /// They can be restricted to a specific domain by adding a FluidDomain component to the same object or to any parent of the Flow Primitive.
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class FlowPrimitive : MonoBehaviour
    {
        /// <summary>
        /// This value is used to scale the flow primitive behaviour. For example, the LifeSpan class will change this value
        /// for a flow primitive to give the effect of the flow primitive decaying over time. For this value to properly work,
        /// the velocity returned by the flow primitive should be scaled by this Strength Scale value.
        /// </summary>
        //[HideInInspector]   //User should not be changing this directly
        /// <summary>
        /// Overal multiplier to the strength of the effect, value of 1 by default. Useful for fading in or out the effect via code or quickly experimenting with effect strength in the inspector.
        /// </summary>
        [Tooltip("Overal multiplier to the strength of the effect, value of 1 by default. Useful for fading in or out the effect via code or quickly experimenting with effect strength in the inspector.")]
        public float strengthScale = 1f;

        /// <summary>
        /// The unique ID number for this flow primitive, used for identifying pairs of ignored interactions by the FlowInteractionManager.
        /// </summary>
        [HideInInspector] public int interactionID = 0;

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

        public virtual void Awake()
        {
            GetInteractionID();
        }

        public virtual void OnEnable()
        {
            AddSelfToScene();
        }

        public virtual void OnDestroy()
        {
            RemoveFromScene();
        }

        public virtual void OnDisable()
        {
            RemoveFromScene();
        }

        public void AddSelfToScene()
        {
            FluidVolume fluidVolume = null;

            // Make sure we have a parent to check
            if (transform.parent != null)
            {
                FluidVolume[] fluidVolumes = transform.GetComponentsInParent<FluidVolume>(false);
                for (int i = 0; i < fluidVolumes.Length; i++)
                {
                    if (fluidVolumes[i].isActiveAndEnabled)
                    {
                        fluidVolume = fluidVolumes[i];
                        break;
                    }
                }
            }

            if (fluidVolume != null)
            {
                // Just to be safe?
                RemoveSelfFromGlobalFluid();
                fluidVolume.AddPrimitive(this);
            }
            else
            {
                AddSelfToGlobalFluid();
            }
        }

        public void AcquireFluidVolume(FluidVolume fluidVolume)
        {
            RemoveSelfFromGlobalFluid();
            fluidVolume.AddPrimitive(this);
        }

        public void RemoveFromScene()
        {
            FluidVolume fluidVolume = transform.GetComponentInParent<FluidVolume>();
            if (fluidVolume != null)
            {
                fluidVolume.RemovePrimitive(this);
            }

            RemoveSelfFromGlobalFluid();

        }

        /// <summary>
        /// Wrapper function which first checks if the interaction should happen. If the interaction is ignored then returns a zero velocity vector.
        /// Otherwise, the regular VelocityFunction(position) is returned.
        /// </summary>
        /// <param name="position">Position of the object in the global (earth) frame of reference.</param>
        /// <param name="flowAffectedID">The interaction ID of the object which is being affected by this flow primitive</param>
        /// <returns>The velocity of the fluid at the given position in the global (earth) frame.</returns>
        /// </summary>
        public Vector3 VelocityFunction(Vector3 position, int flowAffectedID)
        {
            if (IsInteractionIgnored(flowAffectedID))
            {
                return new Vector3(0, 0, 0);
            }

            return VelocityFunction(position);
        }

        /// <summary>
        /// Override this function to change the way the velocity of the fluid is calculated.
        /// </summary>
        /// <param name="position">Position of the object in the global (earth) frame of reference.</param>
        /// <returns>The velocity of the fluid at the given position in the global (earth) frame.</returns>
        /// </summary>
        public virtual Vector3 VelocityFunction(Vector3 position)
        {
            return Vector3.zero;
        }

        public bool IsInteractionIgnored(int flowAffectedID)
        {
            return FlowInteractionManager.IsInteractionIgnored(flowAffectedID, interactionID);
        }

        public void IgnoreInteraction(int flowAffectedID)
        {
            FlowInteractionManager.IgnoreInteraction(flowAffectedID, interactionID);
        }

        public void IgnoreInteraction(FlowAffected flowAffected)
        {
            FlowInteractionManager.IgnoreInteraction(flowAffected.interactionID, interactionID);
        }

        public void AddSelfToGlobalFluid()
        {
            GlobalFluid.AddFlowPrimitive(this);
        }

        public void RemoveSelfFromGlobalFluid()
        {
            GlobalFluid.RemoveFlowPrimitive(this);
        }
    }
}
