using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Rotates a transform about a local axis in the fixed update thread.
    /// </summary>
    public class TransformRotator : MonoBehaviour
    {
        public float angularSpeed;
        public Vector3 axis = Vector3.up;

        // Update is called once per frame
        void FixedUpdate()
        {
            transform.Rotate(angularSpeed * Time.fixedDeltaTime * axis, Space.Self);
        }
    }
}
