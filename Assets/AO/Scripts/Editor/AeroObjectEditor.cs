using AerodynamicObjects.Aerodynamics;
using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Implements the inspector interface for the AeroObject class.
    /// </summary>
    [CustomEditor(typeof(AeroObject)), CanEditMultipleObjects]
    public class AeroObjectEditor : Editor
    {
        AeroObject ao;

        SerializedProperty hasDrag;
        SerializedProperty hasLift;
        SerializedProperty hasRotationalDamping;
        SerializedProperty hasRotationalLift;
        SerializedProperty hasBuoyancy;
        SerializedProperty velocitySource;
        SerializedProperty referenceAreaShape;
        SerializedProperty camber;
        SerializedProperty rb;
        SerializedProperty dimensions;
        SerializedProperty group;
        SerializedProperty updateDimensionsInRuntime;

        Texture banner;

        public void OnEnable()
        {
            banner = (Texture)Resources.Load("ao logo");
            ao = (AeroObject)target;
            ao.Initialise();
            serializedObject.Update();

            hasDrag = serializedObject.FindProperty("hasDrag");
            hasLift = serializedObject.FindProperty("hasLift");
            hasRotationalDamping = serializedObject.FindProperty("hasRotationalDamping");
            hasRotationalLift = serializedObject.FindProperty("hasRotationalLift");
            hasBuoyancy = serializedObject.FindProperty("hasBuoyancy");
            velocitySource = serializedObject.FindProperty("velocitySource");
            referenceAreaShape = serializedObject.FindProperty("referenceAreaShape");
            camber = serializedObject.FindProperty("camber");
            group = serializedObject.FindProperty("myGroup");

            rb = serializedObject.FindProperty("rb");
            dimensions = serializedObject.FindProperty("relativeDimensions");
            updateDimensionsInRuntime = serializedObject.FindProperty("updateDimensionsInRuntime");
        }

        bool showFlowSensingInfo = false;

        public override void OnInspectorGUI()
        {
            // ========= BANNER =================
            if (banner)
            {
                GUILayout.Box(banner, GUILayout.ExpandWidth(true));
            }
            // ==================================

            serializedObject.Update();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Models", EditorStyles.boldLabel);

            EditorGUILayout.PropertyField(hasDrag, new GUIContent("Drag"));
            EditorGUILayout.PropertyField(hasLift, new GUIContent("Lift"));
            EditorGUILayout.PropertyField(hasBuoyancy, new GUIContent("Buoyancy"));
            EditorGUILayout.PropertyField(hasRotationalDamping, new GUIContent("Rotational Damping"));
            EditorGUILayout.PropertyField(hasRotationalLift, new GUIContent("Rotational Lift"));

            if (EditorGUI.EndChangeCheck())
            {
                UpdateModels();
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Flow Sensing and Force Application", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(velocitySource, new GUIContent("Body Velocity Source"));
            EditorGUILayout.PropertyField(rb, new GUIContent("Rigid Body"));

            showFlowSensingInfo = EditorGUILayout.Foldout(showFlowSensingInfo, "Flow Info");

            if (showFlowSensingInfo)
            {
                EditorGUILayout.LabelField("Relative Velocity", ao.relativeVelocity.ToString());
                EditorGUILayout.LabelField("Local Relative Velocity", ao.localRelativeVelocity.ToString());
            }

            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Geometry", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(updateDimensionsInRuntime, new GUIContent("Update Dimensions In Runtime"));
            EditorGUILayout.PropertyField(referenceAreaShape, new GUIContent("Reference Area Shape"));
            EditorGUILayout.PropertyField(dimensions, new GUIContent("Relative Dimensions"));
            EditorGUILayout.PropertyField(camber, new GUIContent("Body Camber"));
            EditorGUILayout.PropertyField(group, new GUIContent("Group"));

            // Debugging labels?
            //EditorGUILayout.LabelField("Group Dimensions", ao.groupDimensions.ToString());
            //EditorGUILayout.LabelField("Aspect Ratio", ao.GetModel<LiftModel>().aspectRatio.ToString());
            //EditorGUILayout.LabelField("Resolved Span", ao.GetModel<LiftModel>().resolvedSpan.ToString());
            //EditorGUILayout.LabelField("Resolved Camber", ao.GetModel<LiftModel>().resolvedCamber.ToString());
            //EditorGUILayout.LabelField("Resolved Chord", ao.GetModel<LiftModel>().resolvedChord.ToString());
            //EditorGUILayout.LabelField("Angle of attack", ao.GetModel<LiftModel>().angleOfAttack.ToString());
            //EditorGUILayout.LabelField("Stall angle", (ao.GetModel<LiftModel>().stallAngle * Mathf.Rad2Deg).ToString());
            //EditorGUILayout.LabelField("Damping Torque", ao.GetModel<RotationalDampingModel>().dampingTorque.ToString());
            //EditorGUILayout.LabelField("Surface area", ao.GetModel<RotationalDampingModel>().yModel.surfaceArea.ToString());
            //EditorGUILayout.LabelField("Pressure area", ao.GetModel<RotationalDampingModel>().yModel.pressureArea.ToString());
            //EditorGUILayout.LabelField("Cf", ao.GetModel<RotationalDampingModel>().yModel.Cf.ToString());
            //EditorGUILayout.LabelField("Cf", ao.GetModel<DragModel>().Cf.ToString());
            //EditorGUILayout.LabelField("Re", ao.GetModel<DragModel>().reynoldsNumber.ToString());
            //EditorGUILayout.LabelField("Adjusted thickness", ao.GetModel<DragModel>().zAxisModel.adjustedThickness.ToString());
            //EditorGUILayout.LabelField("CDS", ao.GetModel<DragModel>().CDS.ToString());
            //EditorGUILayout.LabelField("CDS z", ao.GetModel<DragModel>().zAxisModel.CDS.ToString());
            //EditorGUILayout.LabelField("Alpha 0", ao.GetModel<LiftModel>().alpha_0.ToString());
            //EditorGUILayout.LabelField("Re", ao.GetModel<RotationalDampingModel>().yModel.Re.ToString());
            //EditorGUILayout.LabelField("Pressure Torque", ao.GetModel<RotationalDampingModel>().yModel.pressureTorque.ToString());
            //EditorGUILayout.LabelField("Skin Friction Torque", ao.GetModel<RotationalDampingModel>().yModel.skinFrictionTorque.ToString());
            //EditorGUILayout.LabelField("Net Torque", ao.GlobalNetTorque().ToString());

            serializedObject.ApplyModifiedProperties();
        }

        private void UpdateModels()
        {
            // There MUST be a better way to do this...
            if (hasDrag.boolValue)
            {
                ao.AddModel<DragModel>();
            }
            else
            {
                ao.RemoveModel<DragModel>();
            }

            if (hasLift.boolValue)
            {
                ao.AddModel<LiftModel>();
            }
            else
            {
                ao.RemoveModel<LiftModel>();
            }

            if (hasRotationalDamping.boolValue)
            {
                ao.AddModel<RotationalDampingModel>();
            }
            else
            {
                ao.RemoveModel<RotationalDampingModel>();
            }

            if (hasRotationalLift.boolValue)
            {
                ao.AddModel<RotationalLiftModel>();
            }
            else
            {
                ao.RemoveModel<RotationalLiftModel>();
            }

            if (hasBuoyancy.boolValue)
            {
                ao.AddModel<BuoyancyModel>();
            }
            else
            {
                ao.RemoveModel<BuoyancyModel>();
            }
        }
    }
}