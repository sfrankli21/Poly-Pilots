using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Custom drawer for a directional velocity component. Adds sliders for the azimuth and elevation angles.
    /// </summary>
    [CustomPropertyDrawer(typeof(DirectionalVelocity))]
    public class DirectionalVelocityDrawer : PropertyDrawer
    {
        private SerializedProperty speed, azimuth, elevation;

        void GetProperties(SerializedProperty property)
        {
            if (speed == null)
            {
                speed = property.FindPropertyRelative("speed");
                azimuth = property.FindPropertyRelative("azimuth");
                elevation = property.FindPropertyRelative("elevation");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return (3f * EditorGUIUtility.singleLineHeight) + (2f * EditorGUIUtility.standardVerticalSpacing);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Store the variables for the PID class
            GetProperties(property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(speed));

            EditorGUI.PropertyField(rect, speed);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.Slider(rect, azimuth, -180f, 180f);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            EditorGUI.Slider(rect, elevation, -180f, 180f);
        }
    }
}