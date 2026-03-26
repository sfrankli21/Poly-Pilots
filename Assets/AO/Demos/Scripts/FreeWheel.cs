using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Applies a small torque to wheel colliders to allow them to move freely.
    /// </summary>
    public class FreeWheel : MonoBehaviour
    {
        WheelCollider wheelCollider;
        // Start is called before the first frame update
        void Start()
        {
            wheelCollider = GetComponent<WheelCollider>();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            wheelCollider.motorTorque = 0.001f;
        }
    }
}
