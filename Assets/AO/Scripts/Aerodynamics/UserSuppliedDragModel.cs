using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// A drag model which uses drag coefficients provided, instead of the default drag model's method of approximating the drag coefficients based on the object's geometry.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    public class UserSuppliedDragModel : MonoBehaviour, IAerodynamicModel
    {
        /// <summary>
        /// Drag coefficients for each axis when the flow direction is 180 degress to that axis.
        /// </summary>
        [Tooltip("Drag coefficients for each axis when the flow direction is 180 degress to that axis.")]
        public Vector3 forwardDragCoefficients = Vector3.one;
        /// <summary>
        /// Drag coefficients for each axis when the flow direction is aligned with that axis.
        /// </summary>
        [Tooltip("Drag coefficients for each axis when the flow direction is aligned with that axis.")]
        public Vector3 reverseDragCoefficients = Vector3.one;
        Vector3 localVelocity, dragCoefficients;
        public float CD;
        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            localVelocity = ao.localRelativeVelocity;
            for (int i = 0; i < 3; i++)
            {
                if (localVelocity[i] > 0)
                {
                    dragCoefficients[i] = reverseDragCoefficients[i];
                }
                else
                {
                    dragCoefficients[i] = forwardDragCoefficients[i];
                }
            }

            CD = Vector3.Scale(dragCoefficients, ao.localRelativeVelocity.normalized).magnitude;

            // The convention here is to compute the forces in coordinates local to the object as that is
            // generally how they will come - they can also then be used directly in most cases
            return new AerodynamicLoad
            {
                // No moment for drag as it acts at the centre of the object
                moment = Vector3.zero,

                // The force in the object's local frame

                // These need to be relative to the cross sectional area in the local direction, i.e. CD in Z uses the reference area of x and y dimensions
                force = -ao.dynamicPressure * CD * ao.dimensions.sqrMagnitude * Vector3.Normalize(ao.localRelativeVelocity)
            };

        }

        public void UpdateDimensionValues(AeroObject ao)
        {

        }

        void Awake()
        {
            AeroObject ao = GetComponent<AeroObject>();
            ao.AddMonoBehaviourModel<UserSuppliedDragModel>();
        }
    }
}
