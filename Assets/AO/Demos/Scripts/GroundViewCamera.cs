using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// A camera controller which remains in place in the scene and looks at the target.
    /// The camera's field of view is adjusted to create a zoom effect based on the target's distance from the camera.
    /// </summary>
    public class GroundViewCamera : MonoBehaviour
    {
        public Transform target;
        public float defaultFov = 60f;
        public float defaultDistance = 10f;
        public float zoomRate = 5f;
        public float lookSpeed = 2f;

        float fixedSize;
        float targetAngle;
        Camera cam;

        // Start is called before the first frame update
        void Start()
        {
            cam = GetComponent<Camera>();
        }

        float angleError;
        Vector3 lookDirection;
        void LateUpdate()
        {
            // Zoom
            fixedSize = defaultDistance * Mathf.Tan(0.5f * Mathf.Deg2Rad * defaultFov);
            targetAngle = 2f * Mathf.Rad2Deg * Mathf.Atan(fixedSize / Vector3.Distance(transform.position, target.position));

            // Proportional control for the zoom
            angleError = targetAngle - cam.fieldOfView;
            cam.fieldOfView += zoomRate * angleError * Time.deltaTime;

            // Make sure we can't overshoot!
            if (angleError > 0)
            {
                if (cam.fieldOfView > targetAngle)
                {
                    cam.fieldOfView = targetAngle;
                }
            }
            else
            {
                if (cam.fieldOfView < targetAngle)
                {
                    cam.fieldOfView = targetAngle;
                }
            }

            // Look at target
            lookDirection = Vector3.Normalize(target.position - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDirection), lookSpeed * Time.deltaTime);
        }
    }
}
