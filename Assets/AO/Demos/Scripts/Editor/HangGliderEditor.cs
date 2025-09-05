using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(HangGlider))]
    public class HangGliderEditor : Editor
    {
        public override void OnInspectorGUI()
        {

            Texture banner = Resources.Load("Hang Glider graphic") as Texture;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));
            DrawDefaultInspector();

        }
    }
}