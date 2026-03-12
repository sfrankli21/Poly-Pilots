using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(WindTurbineController))]
    public class WindTurbineEditor : Editor
    {
        readonly string summary = "A simple wind turbine made from three Aerodynamic Object blades and tail fin with Lift and Drag components. The rotor and tail fin assembly is free to rotate around the mast so that it naturally points into the wind. The user can vary the aspect ratio of the blades and blade pitch angle as well as the wind speed and direction. The turbine has a simple generator model that outputs power proportional to aerodynamic torque and rotation rate. The brightness of the light is proportional to the power generated. How much green energy can you make?";

        public override void OnInspectorGUI()
        {
            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(summary, textStyle);

            DrawDefaultInspector();
        }
    }
}