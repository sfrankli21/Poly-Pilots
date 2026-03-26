using UnityEngine;
using UnityEngine.InputSystem;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to switch between the orbital, chase, and ground view cameras in demo scenes.
    /// </summary>
    public class CameraManager : MonoBehaviour
    {
        public Camera orbitalCamera;
        public Camera groundViewCamera;
        public Camera chaseCamera;

        public enum CameraType
        {
            Orbital,
            GroundView,
            Chase
        }

        public CameraType defaultCamera = CameraType.GroundView;

        // Start is called before the first frame update
        void Start()
        {
            groundViewCamera.enabled = false;
            chaseCamera.enabled = false;
            orbitalCamera.enabled = false;

            switch (defaultCamera)
            {
                case CameraType.Orbital:
                    orbitalCamera.enabled = true;
                    break;
                case CameraType.GroundView:
                    groundViewCamera.enabled = true;
                    break;
                case CameraType.Chase:
                    chaseCamera.enabled = true;
                    break;
                default:
                    break;
            }
        }

        // Update is called once per frame
        void Update()
        {
            // F1 key enables the ground view
            if (Keyboard.current.f1Key.wasPressedThisFrame)
            {
                groundViewCamera.enabled = true;
                chaseCamera.enabled = false;
                orbitalCamera.enabled = false;
            }

            // F2 key enables the orbital camera
            if (Keyboard.current.f2Key.wasPressedThisFrame)
            {
                groundViewCamera.enabled = false;
                chaseCamera.enabled = false;
                orbitalCamera.enabled = true;
            }

            // F3 key enables the chase camera
            if (Keyboard.current.f3Key.wasPressedThisFrame)
            {
                groundViewCamera.enabled = false;
                chaseCamera.enabled = true;
                orbitalCamera.enabled = false;
            }
        }
    }
}
