using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to pass the velocity functions of flow primitives without passing the entire instance.
    /// In hindsight this may be unnecessary, but it makes for slightly cleaner looking code.
    /// </summary>
    public delegate Vector3 VelocityEventHandler(Vector3 position);
}
