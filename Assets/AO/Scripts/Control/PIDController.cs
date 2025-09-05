namespace AerodynamicObjects.Control
{
    /// <summary>
    /// A simple PID controller which can be used to produce control signals based on a target input and a measured input.
    /// </summary>
    [System.Serializable]
    public class PIDController
    {
        public float P, I, D;
        public float targetValue;
        public float error;
        public float proportionalTerm, integralTerm, derivativeTerm;

        float integralTermSum = 0f;
        float previousError = 0f;

        public float GetControl(float currentValue, float timeSinceUpdate)
        {
            error = targetValue - currentValue;

            // Proportional
            proportionalTerm = P * error;

            // Integral
            // Adding the terms like this prevents a large spike in the integral term when changing the I value
            integralTermSum += I * error * timeSinceUpdate;
            integralTerm = integralTermSum;

            // Derivative
            derivativeTerm = D * (error - previousError) / timeSinceUpdate;
            previousError = error;

            return proportionalTerm + integralTerm + derivativeTerm;
        }

        public void UpdatePIDValues(float p, float i, float d)
        {
            P = p;
            I = i;
            D = d;
        }
    }
}
