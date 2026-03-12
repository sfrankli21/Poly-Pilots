using System;
using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// Calculates a simple buoyancy force based on the volume of the body and the density of the fluid
    /// around the object. Uses the volume of an ellipsoid or a cuboid depending on the reference area for the object.
    /// </summary>
    [Serializable]
    public class BuoyancyModel : IAerodynamicModel
    {
        private float aoVolume;

        /// <summary>
        /// Calculate the buoyant force acting on the object. Assuming that the object is fully submerged in the relevant fluid.
        /// </summary>
        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            return new AerodynamicLoad
            {
                moment = Vector3.zero,
                force = ao.transform.InverseTransformDirection(-ao.fluid.density * aoVolume * Physics.gravity)
            };
        }

        public void UpdateDimensionValues(AeroObject ao)
        {
            switch (ao.referenceAreaShape)
            {
                case AeroObject.ReferenceAreaShape.Ellipse:
                    aoVolume = 4f / 3f * Mathf.PI * ao.dimensions.x * ao.dimensions.y * ao.dimensions.z * 0.125f;
                    break;
                case AeroObject.ReferenceAreaShape.Rectangle:
                    aoVolume = ao.dimensions.x * ao.dimensions.y * ao.dimensions.z;
                    break;
                default:
                    break;
            }
        }
    }
}
