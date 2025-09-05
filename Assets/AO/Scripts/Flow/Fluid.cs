using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Contains the properties and state of a fluid.
    /// </summary>
    [System.Serializable]
    public class Fluid
    {
        /// <summary>
        /// The velocity of the fluid in the global (earth) frame of reference.
        /// This is independent of the object's velocity. (m/s) (Read Only)
        /// </summary>
        public Vector3 globalVelocity = Vector3.zero;

        /// <summary>
        /// The velocity of the fluid in the object's frame of reference.
        /// This is independent of the object's velocity. (m/s) (Read Only)
        /// </summary>
        public Vector3 localVelocity = Vector3.zero;

        /// <summary>
        /// The pressure of the fluid around the object.
        /// Default value is 101,325 Pa. The same as 1 atmosphere of pressure. (Pa)
        /// </summary>
        public float pressure = 101325f;

        /// <summary>
        /// The density of the fluid. Assuming incompressible flow.
        /// Typical values include 1.23 for air and 1000 for water. (kg/m^3)
        /// </summary>
        public float density = 1.23f;

        /// <summary>
        /// Dynamic viscosity of the fluid. Default value is 1.8e-5 for air. (Nm/s)
        /// </summary>
        public float dynamicViscosity = 1.8e-5f;

        /// <summary>
        /// Copies the properties from the property class. We haven't included the class in this script for brevity.
        /// </summary>
        public void CopyProperties(FluidProperties properties)
        {
            pressure = properties.pressure;
            density = properties.density;
            dynamicViscosity = properties.dynamicViscosity;
        }
    }
}
