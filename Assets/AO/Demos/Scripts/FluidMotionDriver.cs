using AerodynamicObjects.Flow;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to vary the speed and azimuth (horizontal direction) of a uniform flow primitive.
    /// </summary>
    public class FluidMotionDriver : MonoBehaviour
    {
        UniformFlow uniformFlow;
        /// <summary>
        /// Scale magnitude of speed variation with time, []
        /// </summary>
        [Tooltip("Scale magnitude of speed variation with time, []")]
        public float speedAmplitude;
        /// <summary>
        /// Angular frequency of speed magnitude variation, deg/s
        /// </summary>
        [Tooltip("Angular frequency of speed magnitude variation, deg/s")]
        public float speedFrequency;
        /// <summary>
        /// Speed variation is always positive
        /// </summary>
        [Tooltip("Speed variation is always positive")]
        public bool isSingleSided;
        /// <summary>
        /// Rate at which wind azimuth changes with time, deg/s
        /// </summary>
        [Tooltip("Rate at which wind azimuth changes with time, deg/s")]
        public float azimuthRate;

        // Start is called before the first frame update
        void Start()
        {
            uniformFlow = GetComponent<UniformFlow>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (isSingleSided)
            {
                uniformFlow.strengthScale = 0.5f * speedAmplitude * (1 + Mathf.Cos(speedFrequency / 57.4f * Time.time));
            }
            else
            {
                uniformFlow.strengthScale = speedAmplitude * Mathf.Cos(speedFrequency / 57.4f * Time.fixedDeltaTime);
            }

            uniformFlow.windVelocity.azimuth += azimuthRate * Time.deltaTime;
        }
    }
}
