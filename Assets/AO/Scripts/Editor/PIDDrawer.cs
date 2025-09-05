using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Control
{
    /// <summary>
    /// Draws a cleaned up interface for a PID controller component.
    /// </summary>
    [CustomPropertyDrawer(typeof(PIDController))]
    public class PIDDrawer : PropertyDrawer
    {

        private SerializedProperty p, i, d;
        private SerializedProperty pTerm, iTerm, dTerm;
        private SerializedProperty error, targetValue;
        private SerializedProperty integralSteps;

        void GetProperties(SerializedProperty property)
        {
            // I'm just gonna assume if this property is null then they all will be
            if (p == null)
            {
                p = property.FindPropertyRelative("P");
                i = property.FindPropertyRelative("I");
                d = property.FindPropertyRelative("D");

                pTerm = property.FindPropertyRelative("proportionalTerm");
                iTerm = property.FindPropertyRelative("integralTerm");
                dTerm = property.FindPropertyRelative("derivativeTerm");

                error = property.FindPropertyRelative("error");
                targetValue = property.FindPropertyRelative("targetValue");

                integralSteps = property.FindPropertyRelative("integralSteps");
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (property.isExpanded)
            {
                return 10f * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }

            return EditorGUIUtility.singleLineHeight;
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

            property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label);

            // Store the variables for the PID class
            GetProperties(property);

            // Draw label
            //position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

            if (property.isExpanded)
            {

                // Don't make child fields be indented
                //var indent = EditorGUI.indentLevel;
                //EditorGUI.indentLevel = 0;

                Rect rect = new Rect(position.x, position.y + EditorGUIUtility.singleLineHeight, position.width, EditorGUI.GetPropertyHeight(p));

                EditorGUI.PropertyField(rect, p);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, i);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, d);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, integralSteps);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.BeginDisabledGroup(true);

                EditorGUI.PropertyField(rect, targetValue);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, error);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, pTerm);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, iTerm);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.PropertyField(rect, dTerm);
                rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

                EditorGUI.EndDisabledGroup();

                // Set indent back to what it was
                //EditorGUI.indentLevel = indent;
            }

            EditorGUI.EndProperty();
        }
    }
}