using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Gets the list of flow primitives that are acting in the Global Fluid.
    /// Useful to check where flow primitives are being collected in a scene.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Debug/Global Fluid Debugger")]
    public class GlobalFluidDebugger : MonoBehaviour
    {
        public List<FlowPrimitive> FlowPrimitiveList;

        // Update is called once per frame
        void FixedUpdate()
        {
            FlowPrimitiveList = GlobalFluid.GetFlowPrimitiveList();
        }
    }
}