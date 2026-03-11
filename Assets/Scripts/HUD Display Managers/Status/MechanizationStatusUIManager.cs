using UnityEngine;
using TMPro;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class MechanizationStatusUIManager : MonoBehaviour
{
    [System.Serializable]
    public class FlapsSection
    {
        public string FlapsStatusTextDisplayReferenceTag;
        public List<TMP_Text> FlapsStatusTextDisplay = new List<TMP_Text>();
    }

    [System.Serializable]
    public class LandingGearSection
    {
        public string GearStatusTextDisplayReferenceTag;
        public List<TMP_Text> GearStatusTextDisplay = new List<TMP_Text>();
    }

    [System.Serializable]
    public class AirBrakeSection
    {
        public string AirBrakeStatusTextDisplayReferenceTag;
        public List<TMP_Text> AirBrakeStatusTextDisplay = new List<TMP_Text>();
    }

    [SerializeField] JetMechanics jetMechanics;

    [SerializeField] bool enableFlapsDisplay;
    [SerializeField] bool enableLandingGearDisplay;
    [SerializeField] bool enableAirBrakeDisplay;

    [SerializeField] FlapsSection flaps;
    [SerializeField] LandingGearSection landingGear;
    [SerializeField] AirBrakeSection airBrake;

    void Start()
    {
        EstablishReferences();
    }

    void Update()
    {
        if (jetMechanics == null)
        {
            return;
        }

        if (enableFlapsDisplay)
        {
            UpdateFlapsStatusTextDisplay();
        }

        if (enableLandingGearDisplay)
        {
            UpdateGearStatusTextDisplay();
        }

        if (enableAirBrakeDisplay)
        {
            UpdateAirBrakeStatusTextDisplay();
        }
    }

    void EstablishReferences()
    {
        if (enableFlapsDisplay)
        {
            PopulateTMPList(flaps.FlapsStatusTextDisplayReferenceTag, flaps.FlapsStatusTextDisplay);
        }

        if (enableLandingGearDisplay)
        {
            PopulateTMPList(landingGear.GearStatusTextDisplayReferenceTag, landingGear.GearStatusTextDisplay);
        }

        if (enableAirBrakeDisplay)
        {
            PopulateTMPList(airBrake.AirBrakeStatusTextDisplayReferenceTag, airBrake.AirBrakeStatusTextDisplay);
        }
    }

    void PopulateTMPList(string referenceTag, List<TMP_Text> targetList)
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
            if (taggedObjects[i] == null)
            {
                continue;
            }

            TMP_Text text = taggedObjects[i].GetComponent<TMP_Text>();

            if (text != null)
            {
                targetList.Add(text);
            }
        }
    }

    void UpdateFlapsStatusTextDisplay()
    {
        if (flaps.FlapsStatusTextDisplay == null)
        {
            return;
        }

        string statusText = jetMechanics.FlapsDeployed ? "Landing" : "Raised";

        for (int i = 0; i < flaps.FlapsStatusTextDisplay.Count; i++)
        {
            if (flaps.FlapsStatusTextDisplay[i] == null)
            {
                continue;
            }

            flaps.FlapsStatusTextDisplay[i].text = statusText;
        }
    }

    void UpdateGearStatusTextDisplay()
    {
        if (landingGear.GearStatusTextDisplay == null)
        {
            return;
        }

        string statusText = jetMechanics.GearDeployed ? "Deployed" : "Retracted";

        for (int i = 0; i < landingGear.GearStatusTextDisplay.Count; i++)
        {
            if (landingGear.GearStatusTextDisplay[i] == null)
            {
                continue;
            }

            landingGear.GearStatusTextDisplay[i].text = statusText;
        }
    }

    void UpdateAirBrakeStatusTextDisplay()
    {
        if (airBrake.AirBrakeStatusTextDisplay == null)
        {
            return;
        }

        string statusText = jetMechanics.AirbrakeDeployed ? "Deployed" : "Retracted";

        for (int i = 0; i < airBrake.AirBrakeStatusTextDisplay.Count; i++)
        {
            if (airBrake.AirBrakeStatusTextDisplay[i] == null)
            {
                continue;
            }

            airBrake.AirBrakeStatusTextDisplay[i].text = statusText;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(MechanizationStatusUIManager))]
public class MechanizationStatusUIManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty jetMechanics = serializedObject.FindProperty("jetMechanics");
        SerializedProperty enableFlapsDisplay = serializedObject.FindProperty("enableFlapsDisplay");
        SerializedProperty enableLandingGearDisplay = serializedObject.FindProperty("enableLandingGearDisplay");
        SerializedProperty enableAirBrakeDisplay = serializedObject.FindProperty("enableAirBrakeDisplay");
        SerializedProperty flaps = serializedObject.FindProperty("flaps");
        SerializedProperty landingGear = serializedObject.FindProperty("landingGear");
        SerializedProperty airBrake = serializedObject.FindProperty("airBrake");

        EditorGUILayout.PropertyField(jetMechanics);

        EditorGUILayout.PropertyField(enableFlapsDisplay, new GUIContent("Enable Flaps Display"));
        if (enableFlapsDisplay.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Flaps", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(flaps, true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(enableLandingGearDisplay, new GUIContent("Enable Landing Gear Display"));
        if (enableLandingGearDisplay.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Landing Gear", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(landingGear, true);
        }

        EditorGUILayout.Space();
        EditorGUILayout.PropertyField(enableAirBrakeDisplay, new GUIContent("Enable Air Brake Display"));
        if (enableAirBrakeDisplay.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Air Brake", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(airBrake, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif