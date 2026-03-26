using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Constructs and manages a ring vortex using a set of vortex filaments.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Ring Vortex")]
    public class RingVortex : MonoBehaviour
    {
        /// <summary>
        /// Circulation strength of filaments in the vortex ring. (m^2/s)
        /// </summary>
        [Tooltip("Circulation strength of filaments in the vortex ring. (m^2/s)")]
        public float strength = 1;
        /// <summary>
        /// Radius of vortex core. Core model uses a solid body type rotation. The peak value of velocity induced by the vortex increases with decreasing core radius. Try increasing vortex core radius if high induced velocities lead to numerically unstable particle or rigidbody behaviour. Core radius must > 0. (m)
        /// </summary>
        [Tooltip("Radius of vortex core. Core model uses a solid body type rotation. The peak value of velocity induced by the vortex increases with decreasing core radius. Try increasing vortex core radius if high induced velocities lead to numerically unstable particle or rigidbody behaviour. Core radius must > 0. (m)")]
        public float coreRadius = 0.01f;
        /// <summary>
        /// A dynamic filament moves with the local flow field. A non dynamic filament is fixed in space
        /// </summary>
        [Tooltip("A dynamic filament moves with the local flow field. A non dynamic filament is fixed in space")]
        public bool isDynamic;
        /// <summary>
        /// A temporal filament has a finite life overwhich the strength varies
        /// </summary>
        [Tooltip("A temporal filament has a finite life overwhich the strength varies")]
        public bool isTemporal;
        /// <summary>
        /// The number of nodes used to create the ring, there will be the same number of filaments in the vortex ring.
        /// </summary>
        [Tooltip("The number of nodes used to create the ring, there will be the same number of filaments in the vortex ring.")]
        public int numNodes = 6;

        /// <summary>
        /// Radius of the circle that the vortex ring approximates. (m)
        /// </summary>
        [Tooltip("Radius of the circle that the vortex ring approximates. (m)")]
        public float radius = 1f;

        [HideInInspector]
        public VortexFilament[] filaments;

        void Awake()
        {
            //Initialise();
        }

        private void Reset()
        {
            Initialise();
        }

        private void OnValidate()
        {
            numNodes = Mathf.Max(numNodes, 3);
        }

        public void Initialise() // used in edit mode to update ring
        {
            // Remove pre-existing nodes and filaments
            DestroyFilaments();

            // We're relying heavily on a minimum of 3 nodes here. Possibly worth keeping a check here to ensure that
            if (numNodes < 3)
            {
                numNodes = 3;
            }

            filaments = new VortexFilament[numNodes];

            // Create the first filament
            filaments[0] = CreateFlowPrimitiveTools.CreateVortexFilament(transform.TransformPoint(Vector3.right * radius),
                    transform.TransformPoint(Quaternion.Euler(0, 0, 360f / numNodes) * Vector3.right * radius), isDynamic, isTemporal, strength, coreRadius);

            filaments[0].transform.parent = transform;

            // Create the rest of the loop/ring
            for (int i = 1; i < numNodes - 1; i++)
            {
                filaments[i] = CreateFlowPrimitiveTools.AppendFilament(filaments[i - 1], transform.TransformPoint(Quaternion.Euler(0, 0, (float)(i + 1) * 360f / numNodes) * Vector3.right * radius), isDynamic, isTemporal, strength, coreRadius);
                filaments[i].transform.parent = transform;
            }

            // Close the loop
            filaments[numNodes - 1] = CreateFlowPrimitiveTools.CreateVortexFilamentWithoutNodes(Vector3.zero, isDynamic, isTemporal, strength, coreRadius);
            filaments[numNodes - 1].transform.parent = transform;
            filaments[numNodes - 1].startNode = filaments[numNodes - 2].endNode;
            filaments[numNodes - 1].startNode.AddConnection();
            filaments[numNodes - 1].endNode = filaments[0].startNode;
            filaments[numNodes - 1].endNode.AddConnection();

            // We've been parenting the filaments as we go, but not the nodes
            // So go through and parent all the nodes as well
            for (int i = 0; i < filaments.Length; i++)
            {
                filaments[i].startNode.transform.parent = transform;

                // Fix
                filaments[i].Initialise();
            }
        }

        private void DestroyFilaments()
        {
            if (filaments == null)
            {
                filaments = GetComponentsInChildren<VortexFilament>();
            }

            if (filaments == null)
            {
                return;
            }

            for (int i = 0; i < filaments.Length; i++)
            {
                if (filaments[i] != null && filaments[i].gameObject != null)
                {
                    if (!Application.isPlaying)
                    {
                        filaments[i].RemoveConnections();
                    }

                    DestroyImmediate(filaments[i].gameObject);
                }
            }
        }
    }
}
