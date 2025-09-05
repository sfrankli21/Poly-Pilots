using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{

    /// <summary>
    /// Adds graphics to inspector view.
    /// </summary>
    [CustomEditor(typeof(Quadcopter))]
    public class QuadcopterEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            Texture banner = Resources.Load("Quadcopter graphic") as Texture;
            GUILayout.Box(banner, GUILayout.ExpandWidth(true));
            DrawDefaultInspector();
        }
    }
}