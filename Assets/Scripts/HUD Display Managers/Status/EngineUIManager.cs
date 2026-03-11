using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class EngineUIManager : MonoBehaviour
{
    public enum LocalRotationAxis
    {
        X,
        Y,
        Z
    }

    [System.Serializable]
    public class LeftEngineSection
    {
        public string LeftEngineOutputNeedlesReferenceTag;
        public List<Transform> LeftEngineOutputNeedles = new List<Transform>();
        public LocalRotationAxis localRotationAxis;
        public float minRotation;
        public float maxRotation;
    }

    [System.Serializable]
    public class RightEngineSection
    {
        public string RightEngineOutputNeedlesReferenceTag;
        public List<Transform> RightEngineOutputNeedles = new List<Transform>();
        public LocalRotationAxis localRotationAxis;
        public float minRotation;
        public float maxRotation;
    }

    [SerializeField] InputRouter inputRouter;
    [SerializeField] bool enableLeftEngineDisplay;
    [SerializeField] bool enableRightEngineDisplay;
    [SerializeField] LeftEngineSection leftEngine;
    [SerializeField] RightEngineSection rightEngine;

    void Start()
    {
        EstablishReferences();
    }

    void Update()
    {
        if (inputRouter == null)
        {
            return;
        }

        if (enableLeftEngineDisplay)
        {
            UpdateNeedleRotations(leftEngine.LeftEngineOutputNeedles, leftEngine.localRotationAxis, leftEngine.minRotation, leftEngine.maxRotation, inputRouter.LeftEngines);
        }

        if (enableRightEngineDisplay)
        {
            UpdateNeedleRotations(rightEngine.RightEngineOutputNeedles, rightEngine.localRotationAxis, rightEngine.minRotation, rightEngine.maxRotation, inputRouter.RightEngines);
        }
    }

    void EstablishReferences()
    {
        if (enableLeftEngineDisplay)
        {
            PopulateTransformList(leftEngine.LeftEngineOutputNeedlesReferenceTag, leftEngine.LeftEngineOutputNeedles);
        }

        if (enableRightEngineDisplay)
        {
            PopulateTransformList(rightEngine.RightEngineOutputNeedlesReferenceTag, rightEngine.RightEngineOutputNeedles);
        }
    }

    void PopulateTransformList(string referenceTag, List<Transform> targetList)
    {
        if (targetList == null)
        {
            return;
        }

        targetList.Clear();

        if (string.IsNullOrEmpty(referenceTag))
        {
            return;
        }

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(referenceTag);

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            if (taggedObjects[i] != null)
            {
                targetList.Add(taggedObjects[i].transform);
            }
        }
    }

    void UpdateNeedleRotations(List<Transform> targetList, LocalRotationAxis rotationAxis, float minRotation, float maxRotation, float engineValue)
    {
        if (targetList == null)
        {
            return;
        }

        float normalizedValue = Mathf.InverseLerp(-1f, 1f, Mathf.Clamp(engineValue, -1f, 1f));
        float targetRotation = Mathf.Lerp(minRotation, maxRotation, normalizedValue);

        for (int i = 0; i < targetList.Count; i++)
        {
            if (targetList[i] == null)
            {
                continue;
            }

            Vector3 localEuler = targetList[i].localEulerAngles;

            switch (rotationAxis)
            {
                case LocalRotationAxis.X:
                    localEuler.x = targetRotation;
                    break;
                case LocalRotationAxis.Y:
                    localEuler.y = targetRotation;
                    break;
                case LocalRotationAxis.Z:
                    localEuler.z = targetRotation;
                    break;
            }

            targetList[i].localEulerAngles = localEuler;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(EngineUIManager))]
public class EngineUIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty inputRouter = serializedObject.FindProperty("inputRouter");
        SerializedProperty enableLeftEngineDisplay = serializedObject.FindProperty("enableLeftEngineDisplay");
        SerializedProperty enableRightEngineDisplay = serializedObject.FindProperty("enableRightEngineDisplay");
        SerializedProperty leftEngine = serializedObject.FindProperty("leftEngine");
        SerializedProperty rightEngine = serializedObject.FindProperty("rightEngine");

        EditorGUILayout.PropertyField(inputRouter);
        EditorGUILayout.PropertyField(enableLeftEngineDisplay, new GUIContent("Enable Left Engine Display"));

        if (enableLeftEngineDisplay.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Left Engine", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(leftEngine, true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(enableRightEngineDisplay, new GUIContent("Enable Right Engine Display"));

        if (enableRightEngineDisplay.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Right Engine", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(rightEngine, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif