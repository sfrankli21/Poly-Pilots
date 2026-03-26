using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to spawn aeroplanes in a scene.
    /// </summary>
    public class PlaneSpawner : MonoBehaviour
    {
        public GameObject paperPlanePrefab;
        public Transform spawnTarget;
        /// <summary>
        /// Rate at which planes are spawned in planes per second.
        /// </summary>
        [Tooltip("Rate at which planes are spawned in planes per second.")]
        [Range(1, 20)]
        public float spawnrate = 5;
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
        /// Slow motion factor for the scene. Paper planes move fast for their size so it is good to slow down the action. Smaller number is slower.
        /// </summary>
        [Tooltip("Slow motion factor for the scene. Paper planes move fast for their size so it is good to slow down the action. Smaller number is slower.")]
        [Range(0.05f, 1)]
        public float timescale = 0.1f;

        float timer;
        GameObject go;
        ControlSurface csP, csS;
        // Start is called before the first frame update
        void Start()
        {
            Time.timeScale = timescale;
        }

        // Update is called once per frame
        void Update()
        {
            timer -= Time.deltaTime;
            if (timer < 0)
            {
                go = Instantiate(paperPlanePrefab, spawnTarget.position + (Random.insideUnitSphere * spawnRadius), Random.rotation);
                timer = 1 / spawnrate;
                go.transform.parent = transform;
                csP = go.transform.Find("port Aero object").gameObject.AddComponent<ControlSurface>();
                csP.surfaceChordRatio = 0.15f;
                csP.controlSurfaceHinge = go.transform.Find("Paper plane/Port hinge");
                csP.deflectionAngle = meanControlSurfaceDeflection + (Random.Range(-controlSurfaceRange, controlSurfaceRange) / 2);

                csS = go.transform.Find("stb Aero object").gameObject.AddComponent<ControlSurface>();
                csS.surfaceChordRatio = 0.15f;
                csS.controlSurfaceHinge = go.transform.Find("Paper plane/Stb hinge");

                csS.deflectionAngle = meanControlSurfaceDeflection + (Random.Range(-controlSurfaceRange, controlSurfaceRange) / 2);

                go.GetComponent<LifeTimer>().lifeSpanDuration = planeLife;

            }
        }
    }
}
