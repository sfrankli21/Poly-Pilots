using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Editor script for control surfaces. Also provides functionality to add moveable graphics for the control surface.
    /// </summary>
    [CustomEditor(typeof(ControlSurface))]
    public class ControlSurfaceEditor : Editor
    {
        SerializedProperty axis;
        private void OnEnable()
        {
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            axis = serializedObject.FindProperty("axis");
            ControlSurface controlSurface = (ControlSurface)target;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(axis);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                controlSurface.UpdateAxes();
            }
            //controlSurface.Axis = (ControlSurface.ControlSurfaceAxis)EditorGUILayout.EnumPopup(controlSurface.Axis);

            GUI.enabled = false;
            EditorGUILayout.FloatField("Deflection Angle (degrees)", controlSurface.deflectionAngle * Mathf.Rad2Deg);
            GUI.enabled = true;

            serializedObject.ApplyModifiedProperties();
        }

        [MenuItem("CONTEXT/ControlSurface/Create Control Surface Graphics")]
        static void CreateControlSurfaceGraphics(MenuCommand command)
        {
            ControlSurface controlSurface = (ControlSurface)command.context;

            controlSurface.UpdateAxes();

            if (controlSurface.transform.Find("Main element graphic"))
            {
                DestroyImmediate(controlSurface.transform.Find("Main element graphic").gameObject);
            }

            if (controlSurface.transform.Find("Control surface"))
            {
                DestroyImmediate(controlSurface.transform.Find("Control surface").gameObject);
            }

            // This should work for any axis
            float chord = Vector3.Scale(controlSurface.forwardAxis, controlSurface.transform.lossyScale).magnitude;

            GameObject mainBodyGO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            mainBodyGO.name = "Main element graphic";
            mainBodyGO.transform.parent = controlSurface.transform;
            DestroyImmediate(mainBodyGO.GetComponent<BoxCollider>());
            mainBodyGO.GetComponent<MeshRenderer>().material = Resources.Load<Material>("AO white");
            Transform mainBodyTransform = mainBodyGO.transform;
            Transform unscale = new GameObject("Control surface").transform;
            unscale.parent = controlSurface.transform;
            unscale.localPosition = Vector3.zero;
            unscale.localScale = new Vector3(1 / controlSurface.transform.lossyScale.x, 1 / controlSurface.transform.lossyScale.y, 1 / controlSurface.transform.lossyScale.z); // invert the scale of the parent aero object
            unscale.localRotation = Quaternion.identity;
            Transform controlSurfaceHinge = new GameObject("Control surface hinge").transform;
            controlSurfaceHinge.parent = unscale;
            controlSurfaceHinge.localScale = new Vector3(controlSurface.transform.lossyScale.x, controlSurface.transform.lossyScale.y, controlSurface.transform.lossyScale.z); // revert to the scale of the parent aero object

            GameObject controlSurfaceGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            controlSurfaceGo.name = "Control surface graphic";
            controlSurfaceGo.transform.parent = controlSurfaceHinge;
            DestroyImmediate(controlSurfaceGo.GetComponent<BoxCollider>());
            Transform controlSurfaceGraphic = controlSurfaceGo.transform;
            controlSurfaceGo.GetComponent<MeshRenderer>().material = Resources.Load<Material>("AO white");

            mainBodyTransform.localPosition = 0.5f * controlSurface.surfaceChordRatio * controlSurface.forwardAxis;
            mainBodyTransform.localScale = (-controlSurface.surfaceChordRatio * VectorAbs(controlSurface.forwardAxis)) + new Vector3(1, 1, 1);
            mainBodyTransform.localRotation = Quaternion.identity;

            controlSurfaceHinge.localPosition = chord * (controlSurface.surfaceChordRatio - 0.5f) * controlSurface.forwardAxis;
            controlSurfaceHinge.localRotation = Quaternion.Euler(-Mathf.Rad2Deg * controlSurface.deflectionAngle * controlSurface.rotationAxis);

            controlSurfaceGraphic.localPosition = -0.5f * controlSurface.surfaceChordRatio * controlSurface.forwardAxis;
            // This needs fixing
            controlSurfaceGraphic.localScale = new Vector3(1, 1, 1) - VectorAbs(controlSurface.forwardAxis) + (controlSurface.surfaceChordRatio * VectorAbs(controlSurface.forwardAxis));//
            controlSurfaceGraphic.localRotation = Quaternion.identity;

            //set hinge transform reference in the controlsurface component
            controlSurface.controlSurfaceHinge = controlSurfaceHinge;
        }

        static Vector3 VectorAbs(Vector3 v)
        {
            v.x = Mathf.Abs(v.x);
            v.y = Mathf.Abs(v.y);
            v.z = Mathf.Abs(v.z);
            return v;
        }
    }
}