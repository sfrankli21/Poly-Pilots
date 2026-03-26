using AerodynamicObjects.Flow;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to vary the wind speed of a Uniform Flow primitive in a sinusoidal fashion.
    /// </summary>
    public class WindSpeedController : MonoBehaviour
    {
        /// <summary>
        /// Reference to Uniform Flow Primitive if using wind speed control
        /// </summary>
        [Tooltip("Reference to Uniform Flow Primitive if using wind speed control")]
        public UniformFlow uniformFlow;

        [Tooltip("The maximum value of the speed during variation.")]
        public float amplitude = 1;

        [Tooltip("How quickly the speed varies.")]
        public float frequency = 0.1f;

        void FixedUpdate()
        {
            uniformFlow.strengthScale = amplitude * Mathf.Cos(frequency * Time.fixedTime);
        }
    }
}