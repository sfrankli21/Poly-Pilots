using UnityEngine;

namespace AerodynamicObjects.Control
{
    /// <summary>
    /// Creates an arrow with scale and direction proportional to the thrust force produced by a Simple Thruster.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Control/Simple Thrust Arrow")]
    public class SimpleThrustArrow : ArrowComponent
    {
        Arrow arrow;
        public Vector3 thrust;
        SimpleThruster thruster;

        public Color colour = new Color(78f / 255f, 224f / 255f, 62f / 255f, 0.5f);

        void OnEnable()
        {
            arrow = new Arrow(colour, "Simple Thrust Arrow");
            thruster = GetComponent<SimpleThruster>();
        }

        void FixedUpdate()
        {
            if (thruster != null)
            {
                SetArrowPositionAndRotationFromVector(arrow, thruster.thrustVector, transform.position);
            }
            else
            {
                SetArrowPositionAndRotationFromVector(arrow, thrust, transform.position);
            }
        }

        public override void CleanUp()
        {
            arrow.DestroyArrow();
        }
    }
}
