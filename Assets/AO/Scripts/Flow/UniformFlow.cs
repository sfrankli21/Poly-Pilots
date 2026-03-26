using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides a prevailing wind which does not vary with position. Includes an optional perlin noise turbulence model which varies the wind with position and time.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Uniform Flow")]
    public class UniformFlow : FlowPrimitive
    {
        public DirectionalVelocity windVelocity;

        public bool enableTurbulence = false;
        /// <summary>
        /// Magnitude of turblent velocity in m/s
        /// </summary>
        [Tooltip("Magnitude of turblent velocity in m/s")]
        public float turbulenceMagnitude = 1f;

        /// <summary>
        /// Controls how quickly turbulence changes with distance. Use a larger value for higher spatial frequency. Scaled with respect to the size of the fluid zone.
        /// </summary>
        [Tooltip("Controls how quickly turbulence changes with distance. Use a larger value for higher spatial frequency. Scaled with respect to the size of the fluid zone.")]
        public float turbulenceLengthScale = 1f;

        /// <summary>
        /// Controls how quickly turbulence changes with time. Use a larger value for higher temporal frequency. Unscaled.
        /// </summary>
        [Tooltip("Controls how quickly turbulence changes with time. Use a larger value for higher temporal frequency. Unscaled.")]
        public float turbulenceTimeScale = 1f;

        /// <summary>
        /// Restricts turbulence to be in the xy plane only.
        /// </summary>
        [Tooltip("Restricts turbulence to be in the xy plane only.")]
        public bool xyTurbulenceOnly;
        private float zMultiplier;
        private float referenceLength = 1f;

        private void OnValidate()
        {
            turbulenceMagnitude = Mathf.Max(0, turbulenceMagnitude);
            turbulenceLengthScale = Mathf.Max(0, turbulenceLengthScale);
            turbulenceTimeScale = Mathf.Max(0, turbulenceTimeScale);
        }

        private void Update()
        {
            referenceLength = transform.lossyScale.magnitude;
        }

        Vector3 uniformFluidVelocity;
        float time;
        public override Vector3 VelocityFunction(Vector3 position)
        {
            // I'm no longer trying to make these functions thread safe - they need to be optimised as much as possible!
            uniformFluidVelocity = strengthScale * windVelocity.GetVelocity();

            if (!enableTurbulence)
            {
                return uniformFluidVelocity;
            }

            position *= turbulenceLengthScale / referenceLength;

            if (xyTurbulenceOnly)
            {
                zMultiplier = 0;
            }
            else
            {
                zMultiplier = 1;
            }

            time = Time.time * turbulenceTimeScale;

            return new Vector3(uniformFluidVelocity.x + (strengthScale * 2f * turbulenceMagnitude * (Mathf.PerlinNoise(position.y + time, position.z + time) - 0.5f)),
                               uniformFluidVelocity.y + (strengthScale * 2f * turbulenceMagnitude * (Mathf.PerlinNoise(position.x + time, position.z + time) - 0.5f)),
                               uniformFluidVelocity.z + (zMultiplier * (strengthScale * 2f * turbulenceMagnitude * (Mathf.PerlinNoise(position.x + time, position.y + time) - 0.5f))));
        }

#if UNITY_EDITOR
        Vector3 arrowCentre;
        float arrowLength;
        Vector3 velocity;
        float handleSize = 0.5f;
        Color colour = new Color(35f / 255f, 211f / 255f, 251f / 255f, 0.5f);
        private void OnDrawGizmos()
        {
            velocity = VelocityFunction(transform.position);

            if (velocity.sqrMagnitude == 0)
            {
                return;
            }

            arrowLength = handleSize * HandleUtility.GetHandleSize(transform.position);
            arrowCentre = transform.position - (0.5f * arrowLength * velocity.normalized);
            Handles.color = colour;
            Handles.ArrowHandleCap(0, arrowCentre, Quaternion.LookRotation(velocity, Vector3.up), arrowLength, EventType.Repaint);
        }
    }
#endif
}
