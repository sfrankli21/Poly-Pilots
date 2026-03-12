using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Creates an arrow with scale and direction proportional to the wind velocity measured by any Flow Sensor deriving object, including AeroObjects.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Visualisation/Wind Arrow")]
    public class WindArrow : ArrowComponent
    {
        FlowSensor flowSensor;
        Arrow arrow;
        public Color colour = new Color(35f / 255f, 211f / 255f, 251f / 255f, 0.5f);

        private void OnEnable()
        {
            if (!TryGetComponent(out flowSensor))
            {
                Debug.LogError("No Flow Sensor component found on " + gameObject.name);
            }

            arrow = new Arrow(colour, "Wind Arrow");
        }

        private void Reset()
        {
            // For wind we need the arrow head to point at the position instead of coming out of the position
            HeadAimsAtPoint = true;
        }

        Vector3 velocity;

        void Update()
        {
            velocity = -flowSensor.relativeVelocity;

            if (UseCoefficientForScale)
            {
                SetArrowPositionAndRotationFromVector(arrow, velocity.normalized, flowSensor.transform.position);
            }
            else
            {
                // Wind just uses normalised vector instead when in coefficient mode
                SetArrowPositionAndRotationFromVector(arrow, velocity, flowSensor.transform.position);
            }
        }

        public override void CleanUp()
        {
            arrow?.DestroyArrow();
        }
    }
}
