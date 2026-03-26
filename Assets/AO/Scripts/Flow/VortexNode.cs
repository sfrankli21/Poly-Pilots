using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Used to define an end point for a Vortex Filament.
    /// Nodes manage the number of filaments they are connected to and will automatically delete themselves from the scene when they have no connections.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Vortex Node")]
    public class VortexNode : MonoBehaviour
    {
        /// <summary>
        /// Number of connected filaments, 0 by default
        /// </summary>
        public int numConnections = 0;

        bool destroyed = false;
        public void RemoveConnection()
        {
            numConnections--;
            if (numConnections < 1 && !destroyed)
            {
                destroyed = true;

                if (Application.isPlaying)
                {
                    Destroy(gameObject);
                }
                else
                {
                    UnityEditor.EditorApplication.delayCall += () =>
                    {
                        DestroyImmediate(gameObject);
                    };
                }
            }
        }

        public void AddConnection()
        {
            numConnections++;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, 0.1f * HandleUtility.GetHandleSize(transform.position));
        }
    }
#endif
}
