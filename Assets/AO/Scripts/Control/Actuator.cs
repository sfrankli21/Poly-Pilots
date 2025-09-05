using UnityEngine;

namespace AerodynamicObjects.Control
{
    /// <summary>
    /// A base actuator class which is used to create any controllable components.
    /// At the moment this will be overkill, however, it paves the way for more complex control systems in future updates.
    /// </summary>
    public class Actuator : MonoBehaviour
    {
        /// <summary>
        /// The display name for the actuator.
        /// </summary>
        [Tooltip("The display name for the actuator.")]
        public string ActuatorName = "Actuator";

        /// <summary>
        /// Input Value is the target provided by a control input.
        /// </summary>
        [Tooltip("Input Value is the target provided by a control input.")]
        public float InputValue = 0f;

        /// <summary>
        /// Target Value is the processed input value, usually clamped to be between min and max.
        /// </summary>
        [Tooltip("Target Value is the processed input value, usually clamped to be between min and max.")]
        public float TargetValue = 0f;

        /// <summary>
        /// The current value of the controlled object.
        /// </summary>
        [Tooltip("The current value of the controlled object.")]
        public float CurrentValue = 0f;

        /// <summary>
        /// The maximum rate of change of the controlled value.
        /// </summary>
        [Tooltip("The maximum rate of change of the controlled value.")]
        public float ValueRate = 10f;

        public float MinValue = -15f;
        public float MaxValue = 15f;

        public virtual void UpdateValue(float deltaTime)
        {
            TargetValue = Mathf.Clamp(InputValue, MinValue, MaxValue);

            // Can use infinity to instantly move actuator
            if (ValueRate == Mathf.Infinity)
            {
                CurrentValue = InputValue;
            }
            else
            {
                CurrentValue = Mathf.MoveTowards(CurrentValue, InputValue, ValueRate * deltaTime);
            }
        }

        public virtual void ApplyControlSignal(float signal)
        {
            InputValue = GetScaledInput(signal);
        }

        /// <summary>
        /// Scales the input linearly such that -1 becomes the min travel of the actuator and +1 becomes the max travel.
        /// </summary>
        /// <param name="input">Input signal to be scaled</param>
        /// <returns>Scaled input, the target travel of the actuator</returns>
        public float GetScaledInput(float input)
        {
            // This is y = mx + c where the x values are (-1, 1) and the y values are (minValue, maxValue)
            return (input * (MaxValue - MinValue) / 2f) + ((MaxValue + MinValue) / 2f);
        }

        /// <summary>
        /// Used for drawing the actuator handles in the unity editor
        /// </summary>
        /// <returns>The position to draw the actuator's number in the scene</returns>
        public virtual Vector3 Position()
        {
            return transform.position;
        }
    }
}
