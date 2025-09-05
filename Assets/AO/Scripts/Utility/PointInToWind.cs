using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Sets the transform rotation to align the object so that it faces into the measured wind velocity.
    /// </summary>
    public class PointInToWind : MonoBehaviour
    {
        public FlowSensor flowSensor;
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {
            // ============================ Removed the minus sign here! ============================
            transform.rotation = Quaternion.LookRotation(flowSensor.relativeVelocity);

        }
    }
}
