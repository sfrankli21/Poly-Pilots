using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Adds update button inspector view.
    /// </summary>
    [CustomEditor(typeof(PropellerEngine))]
    public class PropellerEngineEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            PropellerEngine propellerEngine = (PropellerEngine)target;
            if (GUILayout.Button("Update propeller geometry"))
            {
                propellerEngine.UpdatePropellerGeometry();
            }
        }
    }
}