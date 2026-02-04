using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MFDMenuPreset
{
    public string presetName;

    [Header("Auto Toggle (Optional)")]
    public GameObject presetRootObject;

    public UnityEvent onActivatePreset;
    public UnityEvent onDeactivatePreset;

    public void Activate()
    {
        if (presetRootObject != null) presetRootObject.SetActive(true);
        onActivatePreset?.Invoke();
    }

    public void Deactivate()
    {
        if (presetRootObject != null) presetRootObject.SetActive(false);
        onDeactivatePreset?.Invoke();
    }
}

public class MFDManager : MonoBehaviour
{
    [Header("Initial Preset")]
    public string initialPresetName;

    [Header("Menu Presets")]
    public List<MFDMenuPreset> menuPresets = new();

    [Header("Current State (Read Only)")]
    [SerializeField]
    private string currentMenuName = "";
    private MFDMenuPreset currentPreset = null;

    void Start()
    {
        if (!string.IsNullOrEmpty(initialPresetName))
        {
            SelectMenuByName(initialPresetName);
        }
    }

    public void SelectMenuByName(string presetName)
    {
        if (string.IsNullOrEmpty(presetName)) return;
        if (currentPreset != null && currentPreset.presetName == presetName) return;

        if (currentPreset != null)
        {
            currentPreset.Deactivate();
        }

        var newPreset = menuPresets.Find(p => p.presetName == presetName);
        if (newPreset != null)
        {
            currentMenuName = presetName;
            currentPreset = newPreset;
            newPreset.Activate();
        }
    }

    public string GetCurrentPresetName()
    {
        return currentMenuName;
    }
}
