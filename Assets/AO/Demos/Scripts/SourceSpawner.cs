using AerodynamicObjects.Flow;
using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Spawns point source flow primitives randomly within a sphere of a given radius.
    /// </summary>
    public class SourceSpawner : ObjectSpawner
    {
        public float sourceStrength = 1f;
        /// <summary>
        /// Object moves with the flow.
        /// </summary>
        [Tooltip("Object moves with the flow.")]
        public bool isDynamic = true;
        /// <summary>
        /// Object has finite life.
        /// </summary>
        [Tooltip("Object has finite life.")]
        public bool isTemporal = true;
        /// <summary>
        /// Life of object in seconds.
        /// </summary>
        [Tooltip("Life of object in seconds.")]
        public float life = 1f;
        /// <summary>
        /// How strength scale should vary over the liftime of the object.
        /// </summary>
        [Tooltip("How strength scale should vary over the liftime of the object.")]
        public AnimationCurve strengthOverTime;
        /// <summary>
        /// Objects are spawned randomly within a sphere of this radius. Set to 0 to spawn at a point.
        /// </summary>
        [Tooltip("Objects are spawned randomly within a sphere of this radius. Set to 0 to spawn at a point.")]
        public float spawnRadius = 0;

        public override void Spawn()
        {
            GameObject go = new GameObject("Source");
            go.transform.parent = transform;
            go.transform.position = transform.position + (spawnRadius * Random.insideUnitSphere);
            PointSource pointSource = go.AddComponent<PointSource>();
            FlowPrimitiveLifeTimer lifespan = go.AddComponent<FlowPrimitiveLifeTimer>();
            lifespan.lifeSpanDuration = life;
            pointSource.sourceStrength = sourceStrength;
            go.AddComponent<MoveWithFlow>();
        }
    }
}
