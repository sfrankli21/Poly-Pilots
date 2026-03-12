using System;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to combine Aero Objects by using the span of the overall group
    /// for aspect ratio corrections of individual objects within the group.
    /// This enables the construction of composite objects such as wings.
    /// </summary>
    [Serializable]
    [AddComponentMenu("Aerodynamic Objects/Aerodynamics/Aero Group")]
    public class AeroGroup : MonoBehaviour
    {
        /// <summary>
        /// The total span of the group. Any aerodynamic object in the group will use the group span to determine its own aspect ratio. (m)
        /// </summary>
        public float span;

        /// <summary>
        /// The direction of the group span. This is defined in the local frame of reference of the group.
        /// </summary>
        public Vector3 spanAxis = Vector3.right;

        /// <summary>
        /// Calculates the span in the global frame of reference. It's worth storing this value if using multiple times. (m)
        /// </summary>
        public Vector3 GlobalSpan => transform.TransformDirection(span * spanAxis.normalized);

        /// <summary>
        /// Calculates only the direction of the span axis in the global frame of reference. It's worth storing this value if using multiple times. (m)
        /// </summary>
        public Vector3 GlobalSpanAxis => transform.TransformDirection(spanAxis.normalized);

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawLine(transform.position + (0.5f * span * GlobalSpanAxis), transform.position - (0.5f * span * GlobalSpanAxis));
        }
    }
}
