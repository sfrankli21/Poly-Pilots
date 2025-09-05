using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to vary the size of an object in a sinusoidal fashion.
    /// </summary>
    public class SizeController : MonoBehaviour
    {
        [Tooltip("The maximum size the object will reach.")]
        public float maxSize = 2f;

        [Tooltip("The minimum size the object will reach.")]
        public float minSize = 0.2f;

        [Tooltip("How quickly the size varies.")]
        public float frequency = 0.2f;

        void FixedUpdate()
        {
            transform.localScale = (minSize + (0.5f * (maxSize - minSize) * (1f + Mathf.Sin(frequency * Time.fixedTime)))) * Vector3.one;
        }
    }
}