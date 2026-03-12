using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Drone-like follow camera which can be controlled in the game window using the mouse, similar to camera controls for Unity's scene view camera.
    /// </summary>
    public class OrbitalCamera : MonoBehaviour
    {
        public Transform target;

        [Tooltip("Increase this value to add smooth motion to the camera follow. A value of zero will snap the camera's position and create a hard follow, increased values will introduce lag to the follow behaviour.")]
        public float cameraSmoothing = 0.1f;

        //[Tooltip("The maximum speed of the follow camera. Helpful to simulate a drone trying to keep up. (m/s)")]
        //public float maxSpeed = Mathf.Infinity;

        /// <summary>
        /// How fast the camera stand off distance changes when scrolling the mouse.
        /// </summary>
        [Tooltip("How fast the camera stand off distance changes when scrolling the mouse.")]
        public float scrollRate = 0.1f;

        ///// <summary>
        ///// Look rotation speed affects how quickly the camera rotates to look at the target. Higher values will keep the target in the centre of the camera's view.
        ///// </summary>
        //[Tooltip("Look rotation speed affects how quickly the camera rotates to look at the target. Higher values will keep the target in the centre of the camera's view. (deg/s)")]
        //public float lookRotationSpeed = 360f;

        /// <summary>
        /// How fast the elevation angle changes when dragging the mouse vertically.
        /// </summary>
        [Tooltip("How fast the elevation angle changes when dragging the mouse vertically. (deg/s)")]
        public float elevationSpeed = 120f;

        /// <summary>
        /// How fast the heading angle changes when dragging the mouse horizontally.
        /// </summary>
        [Tooltip("How fast the heading angle changes when dragging the mouse horizontally. (deg/s)")]
        public float headingSpeed = 120f;

        Vector3 mouseDelta;

        /// <summary>
        /// The elevation angle of the orbital camera (deg).
        /// </summary>
        [Tooltip("The elevation angle of the orbital camera (deg).")]
        public float elevation = 10f;
        float elevationRad;

        /// <summary>
        /// The heading angle of the orbital camera (deg).
        /// </summary>
        [Tooltip("The heading angle of the orbital camera (deg).")]
        public float heading = 0f;
        float headingRad;

        /// <summary>
        /// How far away the camera should be from the target (m).
        /// </summary>
        [Tooltip("How far away the camera should be from the target (m).")]
        public float distance = 10f;

        /// <summary>
        /// Should the camera auto orbit around the the target?
        /// </summary>
        [Tooltip("Should the camera auto orbit around the the target?")]
        public bool autoOrbit = true;
        /// <summary>
        /// How fast the camera should auto orbit if enabled, degrees per second.
        /// </summary>
        [Tooltip("How fast the camera should auto orbit if enabled, degrees per second.")]
        public float autoOrbitRate = 20;

        void Awake()
        {
            if (target == null)
            {
                return;
            }

            if (target.TryGetComponent(out Rigidbody rb))
            {
                if (rb.interpolation != RigidbodyInterpolation.None)
                {
                    Debug.LogWarning("Orbit camera works best with NO interpolation on the target rigid body.");
                    rb.interpolation = RigidbodyInterpolation.None;
                }
            }

            transform.LookAt(target);
        }

        Vector3 GetMousePosition()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.position.ReadValue();
            }
            else
            {
                return Vector3.zero;
            }
        }

        Vector3 GetMouseDelta()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.delta.ReadValue();
            }
            else
            {
                return Vector3.zero;
            }
        }

        float GetMouseScroll()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.scroll.ReadValue()[1] / 120f;
            }
            else
            {
                return 0;
            }
        }

        bool GetMouseLeftClick()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                return mouse.leftButton.ReadValue() == 1f;
            }
            else
            {
                return false;
            }
        }

        Vector3 cameraVelocity = Vector3.zero;
        Vector3 cameraNewPosition;
        Vector3 offsetDirection;

        private void FixedUpdate()
        {
            if (target == null)
            {
                return;
            }
            // Use left click to drag the camera around
            if (GetMouseLeftClick())
            {
                // Get mouse input
                mouseDelta = GetMouseDelta();
                elevation -= mouseDelta.y * elevationSpeed * Time.fixedDeltaTime;
                heading += mouseDelta.x * headingSpeed * Time.fixedDeltaTime;
            }

            if (autoOrbit)
            {
                heading += autoOrbitRate * Time.fixedDeltaTime;
            }

            distance -= GetMouseScroll() * scrollRate;
            if (distance < 0)
            {
                distance = 0;
            }

            // Get the target position for the camera based on the elevation, heading and distance
            elevationRad = Mathf.Deg2Rad * elevation;
            headingRad = Mathf.Deg2Rad * heading;
            offsetDirection = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(headingRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(headingRad)).normalized;
            cameraTargetPosition = target.position + (distance * offsetDirection);
            cameraNewPosition = Vector3.SmoothDamp(transform.position, cameraTargetPosition, ref cameraVelocity, cameraSmoothing, Mathf.Infinity, Time.fixedDeltaTime);

            transform.position = cameraNewPosition;
            transform.LookAt(target);
        }

        Vector3 cameraTargetPosition;

        // This allows the user to tweak the initial conditions and set up the camera how they want
        private void OnValidate()
        {
            cameraSmoothing = Mathf.Max(0, cameraSmoothing);
            if (Application.isPlaying || target == null)
            {
                return;
            }

            // Get the target position for the camera based on the elevation, heading and distance
            elevationRad = Mathf.Deg2Rad * elevation;
            headingRad = Mathf.Deg2Rad * heading;
            Vector3 direction = new Vector3(Mathf.Cos(elevationRad) * Mathf.Sin(headingRad), Mathf.Sin(elevationRad), Mathf.Cos(elevationRad) * Mathf.Cos(headingRad)).normalized;
            transform.position = target.position + (distance * direction);
            transform.LookAt(target);
        }
    }
}
