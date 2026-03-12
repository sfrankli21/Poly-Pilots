using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// An example flow primitive implementation which uses potential flow theory to simulate the flow around a cylinder.
    /// </summary>
    public class LiftingFlowAroundACylinder : FlowPrimitive
    {
        //Computes the flow around a cylinder with lift due to circulation.  The model here uses potential flow theory https://en.wikipedia.org/wiki/Potential_flow and makes use of the compact form of flow primitives specified using 'complex analsyis', that is using complex numbers, see https://en.wikipedia.org/wiki/Complex_analysis. Flow primitives elsewhere within AO generally use 'real' functions. The answers are the same, but the 'complex' models are more compact in code. It is not clear if the overhead of using complex numbers makes these models faster or slower to execute than their 'real' counterparts.
        //For this example, flow is in xy plane with the free stream flow vector acting in the +ve x direction for +ve values of flow speed
        //The circulation strength is defined by the user and is effectively the rate at which the cylinder is spinning.
        public float circulationStrength;
        Complex i, complexPosition, complexVelocity;
        Vector3 localPosition;
        float R = 0.5f, freeStreamSpeed;
        FlowSensor flowSensor;

        public void Start()
        {
            flowSensor = GetComponent<FlowSensor>();
            i = new Complex(0, 1);
        }

        public override Vector3 VelocityFunction(Vector3 position)
        {
            localPosition = transform.InverseTransformPoint(position);
            if (localPosition.magnitude < R)
            {
                return Vector3.zero;
            }

            complexPosition = new Complex(localPosition.x, localPosition.y);
            complexVelocity = (i * circulationStrength / (2 * Mathf.PI * complexPosition)) - (freeStreamSpeed * R * R / (complexPosition * complexPosition));
            return new Vector3((float)complexVelocity.Real, -(float)complexVelocity.Imaginary, 0);

        }
        private void FixedUpdate()
        {

            freeStreamSpeed = flowSensor.relativeVelocity.magnitude;

        }
    }
}
