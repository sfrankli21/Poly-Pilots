using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class ROMask : MonoBehaviour
{
    [System.Flags]
    public enum RevealLayers
    {
        None = 0,
        GameObject = 1 << 0,
        Image = 1 << 1,
        Text = 1 << 2
    }

    [Header("Mask Object Type")]
    public bool Image;
    public bool GameObject;

    public Image imageComponent;
    public MeshRenderer meshRendererComponent;

    [Header("Mask Material")]
    public Material baseMaskMaterial;

    [Header("Reveal Layers")]
    public RevealLayers revealLayers = RevealLayers.GameObject;

    [SerializeField, HideInInspector] Material instantiatedMaskMaterial;
    [SerializeField, HideInInspector] Material lastBaseMaskMaterial;

    public int GetRevealMaskValue()
    {
        return (int)revealLayers;
    }

    public Component GetAssignedMaskComponent()
    {
        if (Image)
        {
            return imageComponent;
        }

        if (GameObject)
        {
            return meshRendererComponent;
        }

        return null;
    }

    void OnEnable()
    {
        ApplyMaskMaterial();
    }

    void OnValidate()
    {
        if (Image && GameObject)
        {
            GameObject = false;
        }

        ApplyMaskMaterial();
    }

    void OnDestroy()
    {
        if (instantiatedMaskMaterial != null)
        {
            if (Application.isPlaying)
            {
                Destroy(instantiatedMaskMaterial);
            }
            else
            {
                DestroyImmediate(instantiatedMaskMaterial);
            }
        }
    }

    void ApplyMaskMaterial()
    {
        if (baseMaskMaterial == null)
        {
            return;
        }

        if (instantiatedMaskMaterial == null || lastBaseMaskMaterial != baseMaskMaterial)
        {
            if (instantiatedMaskMaterial != null)
            {
                if (Application.isPlaying)
                {
                    Destroy(instantiatedMaskMaterial);
                }
                else
                {
                    DestroyImmediate(instantiatedMaskMaterial);
                }
            }

            instantiatedMaskMaterial = new Material(baseMaskMaterial);
            instantiatedMaskMaterial.name = baseMaskMaterial.name + " (RO Instance)";
            lastBaseMaskMaterial = baseMaskMaterial;
        }
        else
        {
            instantiatedMaskMaterial.CopyPropertiesFromMaterial(baseMaskMaterial);
        }

        int maskValue = (int)revealLayers;

        instantiatedMaskMaterial.SetFloat("_Stencil", maskValue);
        instantiatedMaskMaterial.SetFloat("_StencilWriteMask", maskValue);

        if (Image && imageComponent != null)
        {
            imageComponent.material = instantiatedMaskMaterial;
        }

        if (GameObject && meshRendererComponent != null)
        {
            meshRendererComponent.sharedMaterial = instantiatedMaskMaterial;
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(ROMask))]
public class ROMaskEditor : Editor
{
    SerializedProperty imageBool;
    SerializedProperty gameObjectBool;
    SerializedProperty imageComponent;
    SerializedProperty meshRendererComponent;
    SerializedProperty baseMaskMaterial;
    SerializedProperty revealLayers;

    void OnEnable()
    {
        imageBool = serializedObject.FindProperty("Image");
        gameObjectBool = serializedObject.FindProperty("GameObject");
        imageComponent = serializedObject.FindProperty("imageComponent");
        meshRendererComponent = serializedObject.FindProperty("meshRendererComponent");
        baseMaskMaterial = serializedObject.FindProperty("baseMaskMaterial");
        revealLayers = serializedObject.FindProperty("revealLayers");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("Mask Object Type", EditorStyles.boldLabel);

        bool newImage = EditorGUILayout.Toggle("Image", imageBool.boolValue);
        bool newGameObject = EditorGUILayout.Toggle("GameObject", gameObjectBool.boolValue);

        if (newImage && newGameObject)
        {
            if (newImage != imageBool.boolValue)
            {
                newGameObject = false;
            }
            else if (newGameObject != gameObjectBool.boolValue)
            {
                newImage = false;
            }
            else
            {
                newGameObject = false;
            }
        }

        imageBool.boolValue = newImage;
        gameObjectBool.boolValue = newGameObject;

        if (imageBool.boolValue)
        {
            EditorGUILayout.PropertyField(imageComponent, new GUIContent("Image Component"));
        }

        if (gameObjectBool.boolValue)
        {
            EditorGUILayout.PropertyField(meshRendererComponent, new GUIContent("Mesh Renderer Component"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Mask Material", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(baseMaskMaterial, new GUIContent("Base Mask Material"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Reveal Layers", EditorStyles.boldLabel);
        revealLayers.intValue = (int)(ROMask.RevealLayers)EditorGUILayout.EnumFlagsField("Layers", (ROMask.RevealLayers)revealLayers.intValue);

        serializedObject.ApplyModifiedProperties();

        ROMask mask = (ROMask)target;
        if (!Application.isPlaying)
        {
            mask.SendMessage("ApplyMaskMaterial", SendMessageOptions.DontRequireReceiver);
        }
    }
}
#endif