using AerodynamicObjects.Flow;
using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// An example spawner that will create objects that move with the flow.
    /// </summary>
    public class ExampleSpawner : ObjectSpawner
    {
        /// <summary>
        /// How long the objects will last in the scene after being spawned.
        /// </summary>
        [Tooltip("How long the objects will last in the scene after being spawned.")]
        public float lifeSpanDuration = 1f;

        public override void Spawn()
        {
            // First we create a new object. We're adding a kinematic rigid body and a trigger collider so that
            // the object is capable of detecting fluid volumes as well as the global flow (and then we will tell the object to ignore the fluid volume)
            // if we didn't add the collision detection requirements, then the object simply would not detect volumes and would only be
            // affected by the global flow. This could be a design choice but I don't recommend it as the mode of operation.
            GameObject go = CreateFlowPrimitiveTools.CreateKinematicTriggerObject(transform.position, Quaternion.identity, "Move with flow object");

            // Also create a sphere visual so we can see the objects
            GameObject objectVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(objectVisual.GetComponent<SphereCollider>());
            objectVisual.transform.SetParent(go.transform, false);
            objectVisual.transform.localScale = 0.25f * Vector3.one;

            // Add the move with flow script so it can detect and move with the flow
            go.AddComponent<MoveWithFlow>();

            // Add a lifespan object so the spawned objects die
            go.AddComponent<LifeTimer>().lifeSpanDuration = lifeSpanDuration;
        }
    }
}
