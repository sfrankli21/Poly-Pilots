using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides flow velocity from a point at the position of the source.
    /// Can be used with positive or negative strength to simulate a source or sink respectively.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Point Source")]
    public class PointSource : FlowPrimitive
    {
        /// <summary>
        /// The volume flow rate produced by the source in m^3/s. A negative value changes the source into a sink
        /// </summary>
        [Tooltip("The volume flow rate produced by the source in m^3/s. A negative value changes the source into a sink")]
        public float sourceStrength = 1f;

        /// <summary>
        /// Controls the angular extent of the source. A default value of 180 degrees creates a spherical source (flow in all directions). 90 degrees creates a hemispherical source.
        /// </summary>
        [Tooltip("Controls the angular extent of the source. A default value of 180 degrees creates a spherical source (flow in all directions). 90 degrees creates a hemispherical source.")]
        [Range(0, 180)]
        public float semiAngle = 180;

        /// <summary>
        /// Used to limit the maximum velocity magnitude. Defines a core region in which the velocity magnitude is a constant equal to the velocity magnitude at the core radius. Value must be greater than zero 
        /// </summary>
        [Tooltip("Used to limit the maximum velocity magnitude. Defines a core region in which the velocity magnitude is a constant equal to the velocity magnitude at the core radius. Value must be greater than zero ")]
        public float coreRadius = 0.1f;

        /// <summary>
        /// How the velocity fades with radius. For physically conserving flow from a point source use distance squared
        /// </summary>
        [Tooltip("How the velocity fades with radius. For physically conserving flow from a point source use distance squared")]
        public Fade fade = Fade.DistanceSquared;

        float magnitude, radius;
        Vector3 direction, localPosition;
        float cosSemiAngle;

        public override void Awake()
        {
            base.Awake();
            cosSemiAngle = Mathf.Cos(semiAngle * Mathf.Deg2Rad);

        }

        public override Vector3 VelocityFunction(Vector3 position)
        {
            localPosition = transform.InverseTransformPoint(position);

            radius = localPosition.magnitude;
            if (radius <= coreRadius)
            {
                radius = coreRadius;
            }

            direction = localPosition.normalized;

            if (Vector3.Dot(transform.up, direction) > cosSemiAngle)
            {
                return Vector3.zero;
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

            return transform.TransformDirection(strengthScale * magnitude * direction);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, coreRadius);
        }

        private void OnValidate()
        {
            coreRadius = Mathf.Max(coreRadius, float.Epsilon);
            cosSemiAngle = Mathf.Cos(semiAngle * Mathf.Deg2Rad);
        }
    }
}
