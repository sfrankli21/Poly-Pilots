using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using System.Collections.Generic;
#endif

[CreateAssetMenu(fileName = "AircraftWeaponData", menuName = "Aircraft/Aircraft Weapon Data")]
public class AircraftWeaponData : ScriptableObject
{
    public Pylon[] Pylons;

    public enum SelectedWeaponType
    {
        None,
        AIM120C5,
        AIM9M,
        AGM88,
        GBU32,
        MK84,
        FUELPOD,
        ZUNI
    }

    [System.Serializable]
    public class Pylon
    {
        public bool allowAIM120C5;
        public bool allowAIM9M;
        public bool allowAGM88;
        public bool allowGBU32;
        public bool allowMK84;
        public bool allowFUELPOD;
        public bool allowZUNI;

        public SelectedWeaponType SelectedWeapon = SelectedWeaponType.None;

        public bool IsAllowed(SelectedWeaponType weapon)
        {
            switch (weapon)
            {
                case SelectedWeaponType.AIM120C5: return allowAIM120C5;
                case SelectedWeaponType.AIM9M: return allowAIM9M;
                case SelectedWeaponType.AGM88: return allowAGM88;
                case SelectedWeaponType.GBU32: return allowGBU32;
                case SelectedWeaponType.MK84: return allowMK84;
                case SelectedWeaponType.FUELPOD: return allowFUELPOD;
                case SelectedWeaponType.ZUNI: return allowZUNI;
                default: return true;
            }
        }

        public void EnforceSelection()
        {
            if (SelectedWeapon != SelectedWeaponType.None && !IsAllowed(SelectedWeapon))
                SelectedWeapon = SelectedWeaponType.None;
        }
    }

    void OnValidate()
    {
        if (Pylons == null) return;

        for (int i = 0; i < Pylons.Length; i++)
        {
            if (Pylons[i] == null) continue;
            Pylons[i].EnforceSelection();
        }
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(AircraftWeaponData))]
public class AircraftWeaponDataEditor : Editor
{
    SerializedProperty pylonsProp;

    void OnEnable()
    {
        pylonsProp = serializedObject.FindProperty("Pylons");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(pylonsProp, true);

        var data = (AircraftWeaponData)target;

        if (data.Pylons != null)
        {
            for (int i = 0; i < data.Pylons.Length; i++)
            {
                var pylon = data.Pylons[i];
                if (pylon == null) continue;

                pylon.EnforceSelection();
                DrawFilteredSelectedWeapon(pylon, i);
            }
        }

        if (GUI.changed)
            EditorUtility.SetDirty(target);

        serializedObject.ApplyModifiedProperties();
    }

    void DrawFilteredSelectedWeapon(AircraftWeaponData.Pylon pylon, int index)
    {
        var allowed = new List<AircraftWeaponData.SelectedWeaponType> { AircraftWeaponData.SelectedWeaponType.None };

        if (pylon.allowAIM120C5) allowed.Add(AircraftWeaponData.SelectedWeaponType.AIM120C5);
        if (pylon.allowAIM9M) allowed.Add(AircraftWeaponData.SelectedWeaponType.AIM9M);
        if (pylon.allowAGM88) allowed.Add(AircraftWeaponData.SelectedWeaponType.AGM88);
        if (pylon.allowGBU32) allowed.Add(AircraftWeaponData.SelectedWeaponType.GBU32);
        if (pylon.allowMK84) allowed.Add(AircraftWeaponData.SelectedWeaponType.MK84);
        if (pylon.allowFUELPOD) allowed.Add(AircraftWeaponData.SelectedWeaponType.FUELPOD);
        if (pylon.allowZUNI) allowed.Add(AircraftWeaponData.SelectedWeaponType.ZUNI);

        if (pylon.SelectedWeapon != AircraftWeaponData.SelectedWeaponType.None && !allowed.Contains(pylon.SelectedWeapon))
            pylon.SelectedWeapon = AircraftWeaponData.SelectedWeaponType.None;

        string[] display = new string[allowed.Count];
        for (int i = 0; i < allowed.Count; i++)
            display[i] = allowed[i].ToString();

        int currentIndex = allowed.IndexOf(pylon.SelectedWeapon);
        if (currentIndex < 0) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup($"Pylons [{index}] Selected Weapon", currentIndex, display);

        if (newIndex >= 0 && newIndex < allowed.Count)
            pylon.SelectedWeapon = allowed[newIndex];
    }
}
#endif
