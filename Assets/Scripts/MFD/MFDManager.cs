using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[System.Serializable]
public class MFDMenuPreset
{
    public string presetName;
    public UnityEvent onActivatePreset;
    public UnityEvent onDeactivatePreset;
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

        if (currentPreset != null && currentPreset.presetName != presetName)
        {
            currentPreset.onDeactivatePreset?.Invoke();
        }

        var newPreset = menuPresets.Find(p => p.presetName == presetName);
        if (newPreset != null)
        {
            currentMenuName = presetName;
            currentPreset = newPreset;
            newPreset.onActivatePreset?.Invoke();
        }
    }

    public string GetCurrentPresetName()
    {
        return currentMenuName;
    }
}
