using AerodynamicObjects.Flow;
using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Spawns contiguous (connected) line vortex filaments over time.
    /// </summary>
    public class ContiguousLineVortexSpawner : ObjectSpawner
    {
        public float initialStrength = 1f;
        public float coreRadius = 0.01f;
        /// <summary>
        /// Realistic wakes increase in strength just downstream of the body then eventually fade to zero. The time scale is linked to the wake length so that end point of the curve is the end of the wake.
        /// </summary>
        [Tooltip("Realistic wakes increase in strength just downstream of the body then eventually fade to zero. The time scale is linked to the wake length so that end point of the curve is the end of the wake.")]

        public AnimationCurve strengthOverTime = new AnimationCurve(new Keyframe[] { new(0, 0), new(.1f, 1), new(1, 0) });
        VortexNode previousNode;
        public float life; //

        public override void Spawn()
        {
            if (previousNode == null)
            {
                previousNode = CreateFlowPrimitiveTools.CreateVortexNode(transform.position, Quaternion.identity);
            }

            // Create a new filament!
            // The end node of the new filament is the one that was created with the filament, so we keep track of that instead
            previousNode = CreateFlowPrimitiveTools.AppendFilament(previousNode, transform.position, true, true, initialStrength, coreRadius, strengthOverTime, life).endNode;
        }
    }
}
