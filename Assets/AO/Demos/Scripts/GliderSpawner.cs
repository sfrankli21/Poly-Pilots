using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to spawn gliders into the scene for the glider demo.
    /// </summary>
    public class GliderSpawner : MonoBehaviour
    {
        public GameObject planePF;
        /// <summary>
        /// The radius of the ring around which the gliders are randomly spawned.
        /// </summary>
        [Tooltip("The radius of the ring around which the gliders are randomly spawned.")]
        [Range(1, 20)]
        public float spawnRadius = 10;
        /// <summary>
        /// Number of gliders launched per second.
        /// </summary>
        [Tooltip("Number of gliders launched per second.")]
        [Range(1, 20)]
        public float spawnRate = 1;
        float launchSpeed = 6f; // initial launch speed in m/s
        /// <summary>
        /// Fixed elevator setting for glider. Default value of 0 radians. Negative value reduces the trim speed, positive value increases the trim speed.
        /// </summary>
        [Tooltip("Fixed elevator setting for glider. Default value of 0 radians. Negative value reduces the trim speed, positive value increases the trim speed.")]
        [Range(-.1f, .1f)]
        public float elevatorTrim = 0;
        /// <summary>
        /// Fixed rudder setting for glider. Default value of 0.5 radians. Opposite sign changes the turn direction.
        /// </summary>
        [Tooltip("Fixed rudder setting for glider. Default value of 0.5 radians. Opposite sign changes the turn direction.")]
        [Range(-1f, 1f)]
        public float rudderTrim = .5f;
        /// <summary>
        /// The maximum number of gliders to be spawned.
        /// </summary>
        [Tooltip("The maximum number of gliders to be spawned.")]
        [Range(5, 500)]
        public int maxGliders = 100;

        public Transform spawnTarget;
        float time;
        GameObject go;
        Vector3 spawnPosition;
        float theta;

        List<GameObject> spawnList = new List<GameObject>();

        void Update()
        {
            // Remove any gliders that have despawned from the list
            spawnList.RemoveAll(item => item == null);

            time += Time.deltaTime;
            if (time >= (1f / spawnRate) && spawnList.Count < maxGliders)
            {
                theta = Random.Range(0, 2f * Mathf.PI);
                spawnPosition = new Vector3(spawnRadius * Mathf.Sin(theta), 0f, spawnRadius * Mathf.Cos(theta));
                theta += Mathf.PI / 2;
                go = Instantiate(planePF, spawnTarget.position + spawnPosition, new Quaternion(0, Mathf.Sin(theta / 2f), 0, Mathf.Cos(theta / 2f)));
                go.transform.parent = transform;
                go.GetComponentInChildren<Rigidbody>().linearVelocity = launchSpeed * go.transform.forward;
                go.GetComponent<ModelGlider>().elevator.deflectionAngle = elevatorTrim;
                go.GetComponent<ModelGlider>().rudder.deflectionAngle = rudderTrim;
                time -= 1f / spawnRate;

                spawnList.Add(go);
            }
        }
    }
}
