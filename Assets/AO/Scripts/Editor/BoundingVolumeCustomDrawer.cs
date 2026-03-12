using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Custom drawer for the bounding volume component. Hides unnecessary values depending on the type of shape used.
    /// </summary>
    [CustomPropertyDrawer(typeof(BoundingVolume))]
    public class BoundingVolumeCustomDrawer : PropertyDrawer
    {
        private SerializedProperty shape;

        private SerializedProperty boxSize;
        private SerializedProperty sphereRadius;
        private SerializedProperty capsuleRadius;
        private SerializedProperty capsuleHeight;

        void GetProperties(SerializedProperty property)
        {
            shape = property.FindPropertyRelative("shape");
            boxSize = property.FindPropertyRelative("boxSize");
            sphereRadius = property.FindPropertyRelative("sphereRadius");
            capsuleRadius = property.FindPropertyRelative("capsuleRadius");
            capsuleHeight = property.FindPropertyRelative("capsuleHeight");
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            GetProperties(property);

            switch (shape.enumValueIndex)
            {
                case (int)BoundingVolume.Shape.Global:
                    return 1 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                case (int)BoundingVolume.Shape.Box:
                    return 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                case (int)BoundingVolume.Shape.Sphere:
                    return 2 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                case (int)BoundingVolume.Shape.Capsule:
                    return 3 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                case (int)BoundingVolume.Shape.Mesh:
                    return 1 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
                default:
                    return 1 * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
            }
        }

        //public override bool CanCacheInspectorGUI(SerializedProperty property)
        //{
        //    return false;
        //}

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Using BeginProperty / EndProperty on the parent property means that
            // prefab override logic works on the entire property.
            EditorGUI.BeginProperty(position, label, property);

            // Store variables
            GetProperties(property);

            Rect rect = new Rect(position.x, position.y, position.width, EditorGUI.GetPropertyHeight(property));

            EditorGUI.PropertyField(rect, shape);
            rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

            switch (shape.enumValueIndex)
            {
                case (int)BoundingVolume.Shape.Global:
                    break;

                case (int)BoundingVolume.Shape.Box:
                    EditorGUI.PropertyField(rect, boxSize);
                    break;

                case (int)BoundingVolume.Shape.Sphere:
                    EditorGUI.PropertyField(rect, sphereRadius);
                    break;

                case (int)BoundingVolume.Shape.Capsule:
                    EditorGUI.PropertyField(rect, capsuleRadius);
                    rect.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                    EditorGUI.PropertyField(rect, capsuleHeight);
                    break;

                case (int)BoundingVolume.Shape.Mesh:
                    break;
                default:
                    break;
            }

            EditorGUI.EndProperty();
        }
    }
}