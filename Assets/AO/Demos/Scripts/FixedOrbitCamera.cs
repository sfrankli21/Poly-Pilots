using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Orbits the camera around a target with no way for the player to adjust the camera's motion.
    /// </summary>
    public class FixedOrbitCamera : MonoBehaviour
    {
        public float radius, angularSpeed, radialSpeed, radialTimeDelay;
        public float angleOffset;
        public Transform target;

        void LateUpdate()
        {
            float angle = (-Time.time * angularSpeed) + angleOffset;

            if (Time.time > radialTimeDelay)
            {
                radius += Time.deltaTime * radialSpeed;
            }

            float x = radius * Mathf.Sin(angle);
            float z = radius * Mathf.Cos(angle);
            transform.position = new Vector3(x + target.position.x, transform.position.y, z + target.position.z);
            transform.LookAt(target);
        }
    }
}
