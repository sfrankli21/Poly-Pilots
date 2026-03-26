using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manages the orbit camera target to follow paper planes as they fall to the ground.
    /// </summary>
    public class PaperPlaneFollower : MonoBehaviour
    {
        /// <summary>
        /// Don't track a new plane if it is lower than this height.
        /// </summary>
        [Tooltip("Don't track a new plane if it is lower than this height.")]
        public float cutoffHeight = 1f;
        OrbitalCamera orbitalCamera;

        private void Awake()
        {
            orbitalCamera = FindObjectOfType<OrbitalCamera>();
        }

        void FixedUpdate()
        {
            if (orbitalCamera.target == null)
            {
                Rigidbody[] rbs = FindObjectsByType<Rigidbody>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);

                for (int i = 0; i < rbs.Length; i++)
                {
                    if (rbs[i].transform == orbitalCamera.transform || rbs[i].position.y < cutoffHeight)
                    {
                        continue;
                    }

                    orbitalCamera.target = rbs[i].transform;
                    break;
                }
            }
        }
    }
}