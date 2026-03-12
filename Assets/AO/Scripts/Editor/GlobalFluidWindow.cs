using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Provides an editor window for setting the default global fluid properties in an Aerodynamic Objects Unity project.
    /// </summary>
    public class GlobalFluidWindow : EditorWindow
    {
        FluidProperties globalFluidProperties;

        [MenuItem("Window/Aerodynamic Objects/Global Fluid Properties")]
        static void Init()
        {
            // Get existing open window or if none, make a new one:
            GlobalFluidWindow window = (GlobalFluidWindow)GetWindow(typeof(GlobalFluidWindow));
            window.titleContent.text = "Global Fluid Properties";
            window.Show();
        }

        private void OnGUI()
        {
            if (globalFluidProperties == null)
            {
                globalFluidProperties = Resources.Load("GlobalFluidProperties") as FluidProperties;
                if (globalFluidProperties == null)
                {
                    Debug.LogError("No GlobalFluidProperties resource could be found. Please create a new one and place it in a Resources folder.");
                }
            }

            EditorGUI.BeginChangeCheck();
            globalFluidProperties.density = EditorGUILayout.FloatField("Density (kg/m^3)", globalFluidProperties.density);
            globalFluidProperties.pressure = EditorGUILayout.FloatField("Pressure (Pa)", globalFluidProperties.pressure);
            globalFluidProperties.dynamicViscosity = EditorGUILayout.FloatField("Dynamic Viscocity (Pa s)", globalFluidProperties.dynamicViscosity);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(globalFluidProperties);
            }
        }
    }
}