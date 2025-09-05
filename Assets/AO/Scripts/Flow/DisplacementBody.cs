using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Models the flow displaced by an ellipsoid body using a three-component doublet. See https://en.wikipedia.org/wiki/Potential_flow
    /// The strength of the doublet is scaled with the free stream speed of the flow so that the flow boundary ellipsoid defined by the transform scale remains constant.
    /// The solution is exact according to potential flow theory for equal scaling (ellipsoid is a sphere). For unequal scaling the flow at the surface is correct but the outer flow becomes less realistic with great asymetry. Axis scale ratios of a factor of 2 remain reasonable.
    /// </summary>
    [RequireComponent(typeof(FlowSensor))]
    [AddComponentMenu("Aerodynamic Objects/Flow/Displacement Body")]
    public class DisplacementBody : FlowPrimitive
    {
        FlowSensor flowSensor;
        Vector3 localRelativeFluidVelocity;
        float u, v, x, q4, r, r2, x2;
        Vector3 localPosition;
        Vector3 radialVector, _velocity, axialVector;

        public override void Awake()
        {
            base.Awake();
            flowSensor = GetComponent<FlowSensor>();
            flowSensor.IgnoreInteraction(this); // aerobody should ignore velocity induced by the displacement body
        }

        public override Vector3 VelocityFunction(Vector3 position)
        {
            //Get local position relative to transform origin, scaled using the transform scale and rotation. This transforms an elipsoid space with non equal scaling to a unit sphere
            localPosition = transform.InverseTransformPoint(position) / strengthScale; // Dividing by the strength scale allows fine tuning of the displacement effect by the user. A larger value of strength means the flow boundary created by the object is at an increased radius.

            if (localPosition.magnitude < 0.5f / strengthScale) // dont return velocities inside the body. 
            {
                return Vector3.zero;
            }

            //Get the relative fluid velocity in body axes from the attached aero object
            localRelativeFluidVelocity = -flowSensor.localRelativeVelocity;

            _velocity = Vector3.zero; // reset the velocity accumulator

            for (int axis = 0; axis < 3; axis++)  // Sum the influence of doublet on each axis of body. x=0, y=1, z=2
            {
                _velocity += GetDoubletVelocity(axis);
            }

            return transform.TransformDirection(_velocity);
        }

        Vector3 GetDoubletVelocity(int doubletAxis)
        {
            x = localPosition[doubletAxis];
            radialVector = localPosition;
            radialVector[doubletAxis] = 0;
            axialVector = new Vector3(0, 0, 0);
            axialVector[doubletAxis] = 1;

            x2 = x * x;
            r2 = radialVector.sqrMagnitude;

            if (r2 < 1e-5f)
            {
                return new Vector3(0, 0, 0);
            }

            r = Mathf.Sqrt(r2);

            q4 = (x2 + r2) * (x2 + r2);

            // Few optimisations here in the calculations
            u = (r2 - x2) / q4;
            v = -2f * x / q4; // removed r from here as we would normalise the radial vector - essentially dividing by r again
            return localRelativeFluidVelocity[doubletAxis] * 0.25f * ((u * axialVector) + (v * radialVector));

        }
    }
}
