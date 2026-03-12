using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides flow velocity orthogonal to a line with specified length and a direction defined by the transform's forward direction.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Line Source")]
    public class LineSource : FlowPrimitive
    {
        /// <summary>
        /// The volume flow rate produced by the source in m^3/s. A negative value changes the source into a sink
        /// </summary>
        [Tooltip("The volume flow rate produced by the source in m^3/s. A negative value changes the source into a sink")]
        public float sourceStrength = 1;

        /// <summary>
        /// Used to limit the maximum velocity magnitude. Defines a core region in which the velocity magnitude is a constant equal to the velocity magnitude at the core radius. Value must be greater than zero
        /// </summary>
        [Tooltip("Used to limit the maximum velocity magnitude. Defines a core region in which the velocity magnitude is a constant equal to the velocity magnitude at the core radius. Value must be greater than zero")]
        public float coreRadius = 0.01f;

        /// <summary>
        /// Length of line source in z direction. Centre is located at the transform origin
        /// </summary>
        [Tooltip("Length of line source in z direction. Centre is located at the transform origin")]
        public float length = 1f;

        /// <summary>
        /// How the velocity fades with radius. For physically conserving flow from a line source use distance linear
        /// </summary>
        [Tooltip("How the velocity fades with radius. For physically conserving flow from a line source use distance linear")]
        public Fade fade = Fade.DistanceLinear;

        /// <summary>
        /// Should the velocity inside the core radius be zero or the full strength of the source/sink.
        /// </summary>
        [Tooltip("Should the velocity inside the core radius be zero or the full strength of the source/sink.")]
        public bool velocityIsZeroInCore = false;

        float magnitude, radius;
        Vector3 cylindricalPosition, localPosition;

        public override Vector3 VelocityFunction(Vector3 position)
        {
            localPosition = transform.InverseTransformPoint(position);

            //check if within cylinder length
            if (localPosition.z > length / 2 || localPosition.z < -length / 2)
            {
                return Vector3.zero;
            }

            cylindricalPosition.x = localPosition.x;
            cylindricalPosition.y = localPosition.y;
            cylindricalPosition.z = 0;

            radius = cylindricalPosition.magnitude;
            if (radius <= coreRadius)
            {
                if (velocityIsZeroInCore)
                {
                    return Vector3.zero;
                }
                else
                {
                    radius = coreRadius;
                }
            }

            switch (fade)
            {
                case Fade.Constant:
                    magnitude = sourceStrength;
                    break;
                case Fade.DistanceLinear:
                    magnitude = sourceStrength / radius;
                    break;
                case Fade.DistanceSquared:
                    magnitude = sourceStrength / (radius * radius);
                    break;
            }

            return transform.TransformDirection(strengthScale * magnitude * cylindricalPosition.normalized);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireSphere(new Vector3(0, 0, length / 2), coreRadius);
            Gizmos.DrawWireSphere(new Vector3(0, 0, -length / 2), coreRadius);
            Gizmos.DrawLine(new Vector3(0, 0, length / 2), new Vector3(0, 0, -length / 2));
        }

        private void OnValidate()
        {
            coreRadius = Mathf.Max(coreRadius, float.Epsilon);
        }
    }
}
