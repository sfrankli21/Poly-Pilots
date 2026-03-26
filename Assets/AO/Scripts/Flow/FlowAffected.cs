using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Base class for any object which will be affected by the flow.
    /// This class obtains the local flow velocity and will ignore specific flow primitives and fluid volumes.
    /// The class maintains a list of the current local fluid volumes which are affecting the object
    /// using the subscription methods which are called when an object of this type enters a fluid volume.
    /// </summary>
    public class FlowAffected : MonoBehaviour
    {
        /// <summary>
        /// Will the global fluid velocity affect this object?
        /// </summary>
        public bool affectedByGlobalFluid = true;

        /// <summary>
        /// The unique ID number for this object, used for identifying pairs of ignored interactions by the FlowInteractionManager.
        /// </summary>
        [HideInInspector]
        public int interactionID = 0;

        /// <summary>
        /// The local fluid volumes that are currently affecting this object.
        /// </summary>
        public List<FluidVolume> localFluidVolumes = new List<FluidVolume>();

        /// <summary>
        /// The state and properties of the fluid around the object.
        /// </summary>
        public Fluid fluid = new Fluid();

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

        /// <summary>
        /// Tells the flow interaction manager that this object and the provided flow primitive should not interact.
        /// </summary>
        /// <param name="primitive">The flow primitive to ignore</param>
        public void IgnoreInteraction(FlowPrimitive primitive)
        {
            FlowInteractionManager.IgnoreInteraction(interactionID, primitive.interactionID);
        }

        /// <summary>
        /// Tells the flow interaction manager that this object and the provided fluid volume should not interact.
        /// </summary>
        /// <param name="fluidVolume">The fluid volume to ignore</param>
        public void IgnoreInteraction(FluidVolume fluidVolume)
        {
            FlowInteractionManager.IgnoreInteraction(interactionID, fluidVolume.interactionID);
        }

        public virtual void OnDestroy()
        {
            FlowInteractionManager.RemoveFlowAffected(interactionID);
        }

        public virtual void Awake()
        {
            GetInteractionID();
        }

        public virtual void FixedUpdate()
        {
            UpdateFluidVolumes();
            GetFluidVelocity(transform.position);
            GetFluidProperties();
        }

        /// <summary>
        /// Gets fluid properties, either from the most recent fluid volume or from the global fluid.
        /// Really this should be done on an event basis - i.e. only when new volumes are detected or when they are removed.
        /// However, it's difficult to tell when they have been removed because they could have been destroyed while the object is inside the volume.
        /// </summary>
        void GetFluidProperties()
        {
            if (localFluidVolumes.Count > 0)
            {
                fluid.CopyProperties(localFluidVolumes.Last().fluidProperties);
            }
            else
            {
                fluid.CopyProperties(GlobalFluid.FluidProperties);
            }
        }

        /// <summary>
        /// Add this fluid volume to the object's list of fluid volumes.
        /// The object will then be affected by this fluid volume.
        /// </summary>
        public void SubscribeToFluidVolume(FluidVolume fluidVolume)
        {
            // Do we let the interaction manager handle everything or just the lookup table?
            // I think for now, we'll just do the lookup table and leave tags+layers for another day

            if (FlowInteractionManager.IsInteractionIgnored(interactionID, fluidVolume.interactionID))
            {
                return;
            }

            localFluidVolumes.Add(fluidVolume);
        }

        /// <summary>
        /// Remove this fluid volume from the object's list of fluid volumes.
        /// The object will no longer be affected by this fluid volume.
        /// </summary>
        public void UnsubscribeFromFluidVolume(FluidVolume fluidVolume)
        {
            // No need to check here, just try and remove it in case something has happened in the time since subscribing
            localFluidVolumes.Remove(fluidVolume);
        }

        /// <summary>
        /// Looks at the fluid volumes which are currently affecting this object and removes any that have become null or are now ignored.
        /// </summary>
        public void UpdateFluidVolumes()
        {
            // This feels so wrong but so right
            // Removing all volumes which have become null or no longer interact with this object
            //localFluidVolumes.RemoveAll(item => item == null || FlowInteractionManager.IsInteractionIgnored(interactionID, item.interactionID));

            // Thanks skaughtx0r for the optimisation on this
            for (int i = localFluidVolumes.Count - 1; i >= 0; i--)
            {
                if (localFluidVolumes[i] == null || FlowInteractionManager.IsInteractionIgnored(interactionID, localFluidVolumes[i].interactionID))
                {
                    localFluidVolumes.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Removes any fluid volumes which have become null from the list of interacting fluid volumes for this object.
        /// </summary>
        public void RemoveNullVolumes()
        {
            localFluidVolumes.RemoveAll(item => item == null);
        }

        /// <summary>
        /// Gets the fluid velocity at the provided position and checks whether to include the global fluid velocity or not.
        /// If this function is called in a loop it may be more efficient to perform the check on affectedByGlobalFluid
        /// and then call GetFluidVelocityWithGlobal or GetFluidVelocityWithoutGlobal.
        /// </summary>
        /// <param name="position">The position to use when calculating fluid velocity.</param>
        public Vector3 GetFluidVelocity(Vector3 position)
        {

            if (affectedByGlobalFluid)
            {
                fluid.globalVelocity = GlobalFluid.GetVelocity(position, interactionID);
                fluid.pressure = GlobalFluid.FluidProperties.pressure;
                fluid.dynamicViscosity = GlobalFluid.FluidProperties.dynamicViscosity;
                fluid.density = GlobalFluid.FluidProperties.density;
            }
            else
            {
                fluid.globalVelocity = new Vector3(0, 0, 0);
            }

            for (int i = 0; i < localFluidVolumes.Count; i++)
            {
                if (localFluidVolumes[i].isActiveAndEnabled && localFluidVolumes[i].IsPositionInsideZone(position))
                {
                    fluid.globalVelocity += localFluidVolumes[i].VelocityFunction(position, interactionID);
                }
            }

            return fluid.globalVelocity;
        }

        /// <summary>
        /// Gets the fluid velocity at the provided position and checks whether to include the global fluid velocity or not.
        /// If this function is called in a loop it may be more efficient to perform the check on affectedByGlobalFluid
        /// and then call GetFluidVelocityWithGlobal or GetFluidVelocityWithoutGlobal.
        /// Does not perform any interaction checks for the ID of this object.
        /// </summary>
        /// <param name="position">The position to use when calculating fluid velocity.</param>
        public Vector3 GetFluidVelocityNoInteractionCheck(Vector3 position)
        {

            if (affectedByGlobalFluid)
            {
                fluid.globalVelocity = GlobalFluid.GetVelocity(position);
                fluid.pressure = GlobalFluid.FluidProperties.pressure;
                fluid.dynamicViscosity = GlobalFluid.FluidProperties.dynamicViscosity;
                fluid.density = GlobalFluid.FluidProperties.density;
            }
            else
            {
                fluid.globalVelocity = new Vector3(0, 0, 0);
            }

            for (int i = 0; i < localFluidVolumes.Count; i++)
            {
                if (localFluidVolumes[i].isActiveAndEnabled && localFluidVolumes[i].IsPositionInsideZone(position))
                {
                    fluid.globalVelocity += localFluidVolumes[i].VelocityFunction(position);
                }
            }

            return fluid.globalVelocity;
        }

        /// <summary>
        /// Gets the fluid velocity at the provided position under the assumption that the object IS affected by the global fluid.
        /// This is potentially useful 
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public Vector3 GetFluidVelocityWithGlobal(Vector3 position)
        {
            fluid.globalVelocity = GlobalFluid.GetVelocity(position, interactionID);
            fluid.pressure = GlobalFluid.FluidProperties.pressure;
            fluid.dynamicViscosity = GlobalFluid.FluidProperties.dynamicViscosity;
            fluid.density = GlobalFluid.FluidProperties.density;

            for (int i = 0; i < localFluidVolumes.Count; i++)
            {
                if (localFluidVolumes[i].isActiveAndEnabled && localFluidVolumes[i].IsPositionInsideZone(position))
                {
                    fluid.globalVelocity += localFluidVolumes[i].VelocityFunction(position);
                }
            }

            return fluid.globalVelocity;
        }

        public Vector3 GetFluidVelocityWithGlobalNoInteractionCheck(Vector3 position)
        {
            fluid.globalVelocity = GlobalFluid.GetVelocity(position);
            fluid.pressure = GlobalFluid.FluidProperties.pressure;
            fluid.dynamicViscosity = GlobalFluid.FluidProperties.dynamicViscosity;
            fluid.density = GlobalFluid.FluidProperties.density;

            for (int i = 0; i < localFluidVolumes.Count; i++)
            {
                if (localFluidVolumes[i].isActiveAndEnabled && localFluidVolumes[i].IsPositionInsideZone(position))
                {
                    fluid.globalVelocity += localFluidVolumes[i].VelocityFunction(position);
                }
            }

            return fluid.globalVelocity;
        }

        public Vector3 GetFluidVelocityWithoutGlobal(Vector3 position)
        {
            fluid.globalVelocity = new Vector3(0, 0, 0);

            for (int i = 0; i < localFluidVolumes.Count; i++)
            {
                if (localFluidVolumes[i].isActiveAndEnabled && localFluidVolumes[i].IsPositionInsideZone(position))
                {
                    fluid.globalVelocity += localFluidVolumes[i].VelocityFunction(position);
                }
            }

            return fluid.globalVelocity;
        }
    }
}
