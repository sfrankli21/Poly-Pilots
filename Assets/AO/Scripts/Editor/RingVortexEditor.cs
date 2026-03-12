using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides an update button in the inspector for the Ring Vortex class.
    /// </summary>
    [CustomEditor(typeof(RingVortex))]
    public class RingVortexEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Update"))
            {
                ((RingVortex)target).Initialise();
            }
        }
    }
}