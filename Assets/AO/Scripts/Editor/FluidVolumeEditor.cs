using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    [CustomEditor(typeof(FluidVolume))]
    public class FluidVolumeEditor : Editor
    {
        SerializedProperty shape;
        SerializedProperty boxSize;
        SerializedProperty sphereRadius;
        SerializedProperty capsuleRadius;
        SerializedProperty capsuleHeight;

        SerializedProperty fluidProperties;
        SerializedProperty flowPrimitives;

        private void OnEnable()
        {
            shape = serializedObject.FindProperty("shape");
            boxSize = serializedObject.FindProperty("boxSize");
            sphereRadius = serializedObject.FindProperty("sphereRadius");
            capsuleRadius = serializedObject.FindProperty("capsuleRadius");
            capsuleHeight = serializedObject.FindProperty("capsuleHeight");

            fluidProperties = serializedObject.FindProperty("fluidProperties");
            flowPrimitives = serializedObject.FindProperty("flowPrimitives");
        }

        public override void OnInspectorGUI()
        {
            EditorGUILayout.PropertyField(fluidProperties);

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.PropertyField(shape);

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                SetColliderEditor((FluidVolume)target);
            }

            EditorGUI.BeginChangeCheck();

            switch ((FluidVolume.Shape)shape.enumValueIndex)
            {
                case FluidVolume.Shape.Box:
                    EditorGUILayout.PropertyField(boxSize);
                    break;

                case FluidVolume.Shape.Sphere:
                    EditorGUILayout.PropertyField(sphereRadius);
                    break;
                case FluidVolume.Shape.Capsule:
                    EditorGUILayout.PropertyField(capsuleRadius);
                    EditorGUILayout.PropertyField(capsuleHeight);
                    break;
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                ((FluidVolume)target).ApplyColliderProperties();
            }

            EditorGUILayout.PropertyField(flowPrimitives);
            serializedObject.ApplyModifiedProperties();

        }

        public static void SetColliderEditor(FluidVolume fluidVolume)
        {
            Collider collider = fluidVolume.GetComponent<Collider>();

            switch (fluidVolume.shape)
            {
                case FluidVolume.Shape.Box:
                    if (collider.GetType() != typeof(BoxCollider))
                    {
                        DestroyImmediate(collider);
                        collider = fluidVolume.gameObject.AddComponent<BoxCollider>();
                    }

                    break;
                case FluidVolume.Shape.Sphere:
                    if (collider.GetType() != typeof(SphereCollider))
                    {
                        DestroyImmediate(collider);
                        collider = fluidVolume.gameObject.AddComponent<SphereCollider>();
                    }

                    break;
                case FluidVolume.Shape.Capsule:
                    if (collider.GetType() != typeof(CapsuleCollider))
                    {
                        DestroyImmediate(collider);
                        collider = fluidVolume.gameObject.AddComponent<CapsuleCollider>();
                    }

                    break;
                case FluidVolume.Shape.Mesh:
                    if (collider.GetType() != typeof(MeshCollider))
                    {
                        DestroyImmediate(collider);
                        collider = fluidVolume.gameObject.AddComponent<MeshCollider>();
                    }

                    break;
                default:
                    break;
            }

            fluidVolume.ApplyColliderProperties();
        }
    }
}