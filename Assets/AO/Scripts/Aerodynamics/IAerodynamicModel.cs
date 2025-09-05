namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// Interface used by aerodynamic models, such as: Lift, Drag, Rotational Lift, Rotational Drag.
    /// This interface allows for additional models to be developed and used by aerodynamic objects.
    /// </summary>
    public interface IAerodynamicModel
    {
        /// <summary>
        /// Compute the aerodynamic load acting on the aerodynamic object.
        /// </summary>
        /// <param name="ao">The aerodynamic object we want to compute the aerodynamic load for.</param>
        /// <returns>The resulting aerodynamic load.</returns>
        AerodynamicLoad GetAerodynamicLoad(AeroObject ao);

        /// <summary>
        /// Calculate and store any values which only depend on the object's dimensions. This reduces overhead for
        /// objects whose dimensions don't change often.
        /// </summary>
        /// <param name="ao">The aero object we are using for calculations.</param>
        void UpdateDimensionValues(AeroObject ao);
    }
}
