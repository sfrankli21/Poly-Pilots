using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Stores the aerodynamic load acting on an object, including the force vector and the moment.
    /// Typically stored in the local frame of reference for the object.
    /// </summary>
    [System.Serializable]
    public struct AerodynamicLoad
    {
        /// <summary>
        /// The direction and magnitude of the force. (N)
        /// </summary>
        public Vector3 force;// = Vector3.zero;

        /// <summary>
        /// The moment, or torque, which accompanies the force. (Nm)
        /// </summary>
        public Vector3 moment;// = Vector3.zero;
    }
}
