using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(WrightFlyer))]
    public class WrightFlyerEditor : Editor
    {
        readonly string summary = "This aircraft has a layout loosely based on the Wright Flyer. It has a foreplane at the front for longitudinal balance and pitch control and fins at the back for directional stability and yaw control. For this demo, the pitch and yaw inputs physically move the foreplane and rudders rather than changing the camber.  The aircraft is relatively easy to fly.";

        public override void OnInspectorGUI()
        {
            Texture banner = Resources.Load("Wright Flyer graphic") as Texture;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(summary, textStyle);

            DrawDefaultInspector();
        }
    }
}