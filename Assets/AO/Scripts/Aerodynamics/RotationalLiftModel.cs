using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// A model used to determine the lift force produced due to rotation of the object.
    /// This is often referred to as the Magnus Effect. The current model is experimental and can produce
    /// unrealistic results.
    /// </summary>
    [System.Serializable]
    public class RotationalLiftModel : IAerodynamicModel
    {
        public Vector3 dimensionScale;
        Vector3 vr;

        /// <summary>
        /// Calculate the lift force due to rotation of the aerodynamic object.
        /// </summary>
        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {

            vr = Vector3.Scale(ao.localAngularVelocity, dimensionScale);

            return new AerodynamicLoad
            {
                force = ao.fluid.density * Vector3.Cross(vr, ao.localRelativeVelocity),
                moment = Vector3.zero
            };

        }

        public void UpdateDimensionValues(AeroObject ao)
        {
            if (ao.dimensions.x > ao.dimensions.y)
            {
                // Yes I know this can be simplified - leaving it for now so I know where it's coming from
                // We need the radius for the speed at the surface and then we're using the thickness to chord ratio
                // to scale so that something squashed won't act like a cylinder
                dimensionScale.z = 2f * Mathf.PI * 0.25f * ao.dimensions.x * ao.dimensions.x * ao.dimensions.y / ao.dimensions.x;
            }
            else
            {
                dimensionScale.z = 2f * Mathf.PI * 0.25f * ao.dimensions.y * ao.dimensions.y * ao.dimensions.x / ao.dimensions.y;
            }

            if (ao.dimensions.z > ao.dimensions.y)
            {
                dimensionScale.x = 2f * Mathf.PI * 0.25f * ao.dimensions.z * ao.dimensions.z * ao.dimensions.y / ao.dimensions.z;
            }
            else
            {
                dimensionScale.x = 2f * Mathf.PI * 0.25f * ao.dimensions.y * ao.dimensions.y * ao.dimensions.z / ao.dimensions.y;
            }

            if (ao.dimensions.x > ao.dimensions.z)
            {
                dimensionScale.y = 2f * Mathf.PI * 0.25f * ao.dimensions.x * ao.dimensions.x * ao.dimensions.z / ao.dimensions.x;
            }
            else
            {
                dimensionScale.y = 2f * Mathf.PI * 0.25f * ao.dimensions.z * ao.dimensions.z * ao.dimensions.x / ao.dimensions.z;
            }

            // Scale by the axis dimension as the formula being used is for lift per unit span
            dimensionScale.Scale(ao.dimensions);
        }
    }
}
