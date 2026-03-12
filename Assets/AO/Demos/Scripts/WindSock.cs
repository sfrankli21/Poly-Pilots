using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manages the string of rigid bodies and line renderers in a wind sock.
    /// </summary>
    public class WindSock : MonoBehaviour
    {
        // Scales the mass and string attachment length of windsock with the scale of the transform. Default values are for a scale of 1.
        public ConfigurableJoint element1joint;
        SoftJointLimit jointLimit;
        LineRenderer[] stringLineRenders;

        Rigidbody[] rigidbodies;

        void Start()
        {
            jointLimit = element1joint.linearLimit;
            jointLimit.limit = 0.25f * transform.localScale.x; // This sets the string length on the first section, Default is 0.25m for 2 m high pole, 0.8m length sock
            element1joint.linearLimit = jointLimit; // set the values on teh joint

            rigidbodies = GetComponentsInChildren<Rigidbody>();
            foreach (Rigidbody rigidbody in rigidbodies)
            {
                rigidbody.mass = 0.005f * Mathf.Pow(transform.localScale.magnitude, 2); // mass per element for default sock (0.8m length) is 0.005g
            }
            // set string thickness
            stringLineRenders = GetComponentsInChildren<LineRenderer>();
            foreach (LineRenderer lr in stringLineRenders)
            {
                lr.startWidth = 0.005f * transform.localScale.x;
            }
        }
    }
}
