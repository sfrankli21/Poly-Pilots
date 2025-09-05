using UnityEngine;

namespace AerodynamicObjects
{
    [CreateAssetMenu(fileName = "Fluid Properties", menuName = "Aerodynamic Objects/Fluid Properties", order = 100)]
    public class FluidProperties : ScriptableObject
    {
        /// <summary>
        /// Density of the fluid. (kg/m^3)
        /// </summary>
        [Tooltip("Density of the fluid. (kg/m^3)")]
        public float density = 1.23f;

        /// <summary>
        /// Pressure in the fluid. (N/m^2)
        /// </summary>
        [Tooltip("Pressure in the fluid. (N/m^2)")]
        public float pressure = 101325f;

        /// <summary>
        /// Dynamic viscocity of the fluid. (Ns/m^2)
        /// </summary>
        [Tooltip("Dynamic viscocity of the fluid. (Ns/m^2)")]
        public float dynamicViscosity = 1.8e-5f;
    }
}
