using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds summary and graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(ControllableModelGlider))]
    public class ConfigurableModelGliderEditor : Editor
    {
        readonly string summary = "The Model Glider is a simple aircraft modelled with two Aerodynamic Object panels for the wings and one each for the tailplane and fin. There is no engine. ";

        public override void OnInspectorGUI()
        {
            Texture banner = (Texture)Resources.Load("Model glider graphic", typeof(Texture));
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));

            GUIStyle textStyle = EditorStyles.label;
            textStyle.wordWrap = true;
            EditorGUILayout.LabelField(summary, textStyle);

            DrawDefaultInspector();
        }
    }
}