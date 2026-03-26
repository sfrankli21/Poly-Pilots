using AerodynamicObjects.Flow;
using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to spawn a ring vortex periodically.
    /// </summary>
    public class RingVortexSpawner : ObjectSpawner
    {
        [Range(4, 12)]
        public int nodeCount;
        public bool isDynamic;
        public bool isTemporal = true;
        public float radius = 1f;
        public float coreRadius = 0.01f;
        public float initialStrength = 1f;
        public float lifeSpanDuration = 10f;

        RingVortex ringVortex;
        GameObject go;

        public override void Spawn()
        {
            go = new GameObject("Ring Vortex");
            go.transform.SetPositionAndRotation(transform.position, transform.rotation);
            go.transform.parent = transform;

            ringVortex = go.AddComponent<RingVortex>();
            ringVortex.numNodes = nodeCount;
            ringVortex.radius = radius;
            ringVortex.isDynamic = isDynamic;
            ringVortex.coreRadius = coreRadius;
            ringVortex.strength = initialStrength;
            ringVortex.Initialise();

            if (isTemporal)
            {
                go.AddComponent<FlowPrimitiveLifeTimer>().lifeSpanDuration = lifeSpanDuration;
            }
        }
    }
}
