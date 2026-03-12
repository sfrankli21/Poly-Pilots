using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary to inspector view.
    /// </summary>
    [CustomEditor(typeof(BalloonController))]
    public class BalloonControllerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField("A simple lighter-than-air balloon attached to a ballast weight with a simple string model. The balloon has an aerodynamic model with Buoyancy and Drag components. The environment uses a FluidZone to set the density of the surrounding air and a simple wind model. The balloon has a custom BallloonController script running on it that allows the user to inflate or deflate the balloon and hence change the buoyancy force. The user can also change the wind speed and change the length of the string during the game.", textStyle);
            DrawDefaultInspector();
        }
    }
}
