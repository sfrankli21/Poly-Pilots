using System;
using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Provides an inspector route to setting up ignored interactions between objects which already exist in the scene.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Ignore Interaction Helper", 31)]
    [DefaultExecutionOrder(50)]
    public class IgnoreInteractionHelper : MonoBehaviour
    {
        [Serializable]
        public class IgnoreInteractionCollection
        {
            public FlowAffected flowAffectedObject;

            public List<FlowPrimitive> ignoredPrimitives = new List<FlowPrimitive>();
            public List<FluidVolume> ignoredFluidVolumes = new List<FluidVolume>();
        }

        public IgnoreInteractionCollection[] ignoreInteractions = new IgnoreInteractionCollection[0];

        // Start is called before the first frame update
        void OnEnable()
        {
            for (int i = 0; i < ignoreInteractions.Length; i++)
            {
                FlowInteractionManager.AddIgnoreList(ignoreInteractions[i].flowAffectedObject.interactionID, ignoreInteractions[i].ignoredPrimitives);
                FlowInteractionManager.AddIgnoreList(ignoreInteractions[i].flowAffectedObject.interactionID, ignoreInteractions[i].ignoredFluidVolumes);
            }
        }
    }
}