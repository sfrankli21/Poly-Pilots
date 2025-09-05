using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(Airship))]
    public class AirshipEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Texture banner = (Texture)Resources.Load("Airship graphic", typeof(Texture));
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField("This airship model is built from an ellipsoid Aerodynamic Object body with lift, drag and buoyancy models. There is a simple cruciform tail that provides directional stability and pitch and yaw control when the airship is moving forwards. There are two bi-directional thrusters that can provide fore and aft thrust or differential thrust to turn. These thrusters can also be tilted to provide vertical thrust to control height. Start up the engines using w then tilt upwards using +. Turn left and right using a and d. The buoyancy force is the orange arrow and the weight force is the yellow and black arrow.");
            DrawDefaultInspector();
        }
    }
}