using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// A follow camera which maintains a fixed heading relative to the target. The heading offset is set on Awake using the relative position of the camera and target.
    /// The user can scroll to adjust distance from the target.
    /// </summary>
    public class ChaseCamera : MonoBehaviour
    {
        public Transform target;

        [Tooltip("Increase this value to add smooth motion to the camera follow. A value of zero will snap the camera's position and create a hard follow, increased values will introduce lag to the follow behaviour.")]
        public float cameraSmoothing = 0.1f;

        /// <summary>
        /// How fast the camera stand off distance changes when scrolling the mouse.
        /// </summary>
        [Tooltip("How fast the camera stand off distance changes when scrolling the mouse.")]
        public float scrollSpeed = 1f;

        /// <summary>
        /// Look rotation speed affects how quickly the camera rotates to look at the target. Higher values will keep the target in the centre of the camera's view.
        /// </summary>
        [Tooltip("Look rotation speed affects how quickly the camera rotates to look at the target. Higher values will keep the target in the centre of the camera's view.")]
        public float lookRotationSpeed = 0.1f;

        Vector3 directionForOffset;
        float distance = 0f;
        float initialHeadingAngle;
        Vector3 cameraTargetPosition;

        void Awake()
        {
            if (target == null)
            {
                Destroy(this);
            }

            // The position of the camera relative to the target, used to keep an offset between the two
            directionForOffset = Vector3.Normalize(transform.position - target.position);
            distance = Vector3.Distance(transform.position, target.position);
            initialHeadingAngle = target.eulerAngles.y;
            transform.LookAt(target);
        }

        Vector3 toTarget;
        Vector3 movementToTarget;
        Vector3 cameraNewPosition;
        Vector3 targetLookDirection;
        Quaternion lookRotation;
        Vector3 cameraVelocity;

        private void FixedUpdate()
        {
            // Allow the user to scroll the distance
            distance -= GetMouseScroll() * scrollSpeed;

            if (distance < 0)
            {
                distance = 0;
            }

            // Get the target position for the camera by rotating the offset by the heading angle of the target
            cameraTargetPosition = target.position + (Quaternion.Euler(0, target.eulerAngles.y - initialHeadingAngle, 0) * (distance * directionForOffset));

            cameraNewPosition = Vector3.SmoothDamp(transform.position, cameraTargetPosition, ref cameraVelocity, cameraSmoothing, Mathf.Infinity, Time.fixedDeltaTime);

            //// Get the direction we should be looking to see the target
            //targetLookDirection = Vector3.Normalize(target.position - cameraNewPosition);

            //// Slerp the current camera rotation towards the rotation it should be at to see the target
            //lookRotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(targetLookDirection), lookRotationSpeed);

            //// Set position and rotation of the camera
            //transform.SetPositionAndRotation(cameraNewPosition, lookRotation);
            transform.position = cameraNewPosition;
            transform.LookAt(target);
        }

        Mouse mouse;
        float GetMouseScroll()
        {
            mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.scroll.ReadValue()[1] / 120f;
            }
            else
            {
                return 0;
            }
        }
    }
}
