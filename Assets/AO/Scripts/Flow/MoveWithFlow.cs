using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Moves the object according to the flow velocity measured at the transform position this script is attached to.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Move With Flow", -22)]
    public class MoveWithFlow : FlowAffected
    {
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            transform.position += fluid.globalVelocity * Time.fixedDeltaTime;
        }
    }
}
