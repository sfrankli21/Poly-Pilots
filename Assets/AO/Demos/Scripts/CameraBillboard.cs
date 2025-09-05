using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to align an object with the main camera.
    /// </summary>
    public class CameraBillboard : MonoBehaviour
    {
        // Update is called once per frame
        void Update()
        {
            transform.forward = Camera.main.transform.forward;
        }
    }
}
