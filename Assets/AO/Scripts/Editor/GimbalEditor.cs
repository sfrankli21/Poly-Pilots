using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Adds a button to the gimbal component to update the rotation of an object held on the gimbal.
    /// </summary>
    [CustomEditor(typeof(Gimbal))]
    public class GimbalEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Set Rotation"))
            {
                ((Gimbal)target).SetRotation();
            }
        }
    }
}