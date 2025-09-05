using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(TransportAircraft))]
    public class TransportAircraftEditor : Editor
    {
        readonly string summary = "A four-engined transport aircraft loosely based on an A400M but with lower wing loading so that it flies relatively slowly. Propellers are modelled with Aerodynamic Object panels that spin around a hub to produce thrust. The pitch and diameter of the propeller blades can be varied in the editor to model propellers with different characteristics. Can you take off, complete a circuit and land again?";

        public override void OnInspectorGUI()
        {
            Texture banner = Resources.Load("Transport aircraft graphic") as Texture;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(summary, textStyle);

            DrawDefaultInspector();
        }
    }
}