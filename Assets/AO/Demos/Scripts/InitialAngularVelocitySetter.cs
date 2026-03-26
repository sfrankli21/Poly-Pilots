using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Gives a rigid body a prescribed angular velocity in the provided local axis.
    /// </summary>
    public class InitialAngularVelocitySetter : MonoBehaviour
    {
        public float spinRate;
        public Vector3 localAxis = new Vector3(0, 1, 0);
        Rigidbody rb;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            rb.angularVelocity = transform.TransformDirection(spinRate * localAxis);
        }
    }
}
