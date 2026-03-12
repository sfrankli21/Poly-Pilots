using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(Helicopter))]
    public class HelicopterEditor : Editor
    {
        readonly string summary = "This helicopter is a simple mechanically controlled helicopter with a main rotor and tail rotor. Each rotor blade is made up of one Aerodynamic Object panel. The helicopter is controlled by changing blade pitch angle. Because the main rotor blades are attached using flapping hinges, they move up and down depending on the load on them. This allows the rotor disc to tilt in response to cyclic blade pitch inputs and is used to control vehicle pitch and roll. Yaw comes from controlling the thrust of the tail rotor. It is a challenge to fly using keyboard input, but possible with practice. Start with around 80% throttle and use very small control inputs. To start off with, try to take off, hover just above the landing pad then land again. Then try the same at a different yaw angle (a/d). The Aerial Chase Cam view (F3) makes this easier to start with. ";

        public override void OnInspectorGUI()
        {
            Texture banner = Resources.Load("Helicopter graphic") as Texture;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(summary, textStyle);

            DrawDefaultInspector();
        }
    }
}