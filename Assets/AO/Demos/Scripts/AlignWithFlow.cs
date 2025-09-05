using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Points the forward direction of the transform in the direction of the relative flow velocity measured by a given flow sensor.
    /// </summary>
    public class AlignWithFlow : MonoBehaviour
    {
        public FlowSensor flowSensor;

        private void FixedUpdate()
        {
            if (flowSensor.relativeVelocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            transform.forward = flowSensor.relativeVelocity.normalized;
        }
    }
}
