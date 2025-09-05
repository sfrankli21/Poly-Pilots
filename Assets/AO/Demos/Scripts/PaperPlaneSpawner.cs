using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Spawns paper planes in the scene for the paper plane demo.
    /// </summary>
    public class PaperPlaneSpawner : ObjectSpawner
    {
        public GameObject paperPlanePrefab;
        public Transform spawnTarget;

        /// <summary>
        /// Radius of sphere within which planes are spawned. Sphere centre located at spawn target, m.
        /// </summary>
        [Tooltip("Radius of sphere within which planes are spawned. Sphere centre located at spawn target, m.")]
        [Range(1, 10)]
        public float spawnRadius = 2;

        /// <summary>
        /// Sets the average trim state for planes. Random variation is applied either side of this. In radians.
        /// </summary>
        [Tooltip("Sets the average trim state for planes. Random variation is applied either side of this. In radians.")]
        [Range(-0.15f, -0.05f)]
        public float meanControlSurfaceDeflection = -0.1f; // negative is trailing edge up

        /// <summary>
        /// The range over which random variations are applied to control surface deflection, radians.
        /// </summary>
        [Tooltip("The range over which random variations are applied to control surface deflection, radians.")]
        [Range(0, 0.2f)]
        public float controlSurfaceRange = 0.01f;

        /// <summary>
        /// How long each plane exists in the scene before it is destroyed.
        /// </summary>
        [Tooltip("How long each plane exists in the scene before it is destroyed.")]
        [Range(2, 20)]
        public float planeLife = 5;

        /// <summary>
        /// How fast spawned planes travel.
        /// </summary>
        [Tooltip("How fast spawned planes travel.")]
        [Range(0, 10)]
        public float intialSpeed = 1;

        GameObject go;
        ControlSurface csP, csS;

        public override void Spawn()
        {
            go = Instantiate(paperPlanePrefab, spawnTarget.position + (Random.insideUnitSphere * spawnRadius), Random.rotation);
            go.transform.parent = transform;

            go.GetComponent<Rigidbody>().linearVelocity = intialSpeed * go.transform.forward;

            // Using transform.Find is not recommended as it will break if someone changes the name of a game object on the prefab
            // and things like intellisense aren't aware of the connection between the game object names and the strings used in the function.
            csP = go.transform.Find("port Aero object").gameObject.AddComponent<ControlSurface>();
            csP.surfaceChordRatio = 0.15f;
            csP.controlSurfaceHinge = go.transform.Find("Paper plane/Port hinge");
            csP.deflectionAngle = meanControlSurfaceDeflection + (Random.Range(-controlSurfaceRange, controlSurfaceRange) / 2);

            csS = go.transform.Find("stb Aero object").gameObject.AddComponent<ControlSurface>();
            csS.surfaceChordRatio = 0.15f;
            csS.controlSurfaceHinge = go.transform.Find("Paper plane/Stb hinge");

            csS.deflectionAngle = meanControlSurfaceDeflection + (Random.Range(-controlSurfaceRange, controlSurfaceRange) / 2);

            go.GetComponent<LifeTimer>().lifeSpanDuration = planeLife;

            //FindObjectOfType<OrbitalCamera>().target = go.transform;
        }
    }
}
