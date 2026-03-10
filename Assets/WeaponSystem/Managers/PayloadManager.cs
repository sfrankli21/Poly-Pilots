using System;
using UnityEngine;

public class PayloadManager : MonoBehaviour
{
    public AircraftWeaponData aircraftWeaponData;

    [Serializable]
    public class PylonElement
    {
        public Transform pylonTransform;
    }

    public PylonElement[] Pylons;

    public GameObject AIM120C5Prefab;
    public GameObject AIM9MPrefab;
    public GameObject AGM88Prefab;
    public GameObject GBU32Prefab;
    public GameObject MK84Prefab;
    public GameObject FUELPODPrefab;
    public GameObject ZUNIPrefab;

    void Start()
    {
        if (aircraftWeaponData == null) return;
        if (aircraftWeaponData.Pylons == null) return;
        if (Pylons == null) return;

        int count = Mathf.Min(aircraftWeaponData.Pylons.Length, Pylons.Length);

        for (int i = 0; i < count; i++)
        {
            if (Pylons[i] == null) continue;
            if (Pylons[i].pylonTransform == null) continue;

            GameObject prefabToSpawn = GetPrefabForWeapon(aircraftWeaponData.Pylons[i].SelectedWeapon);
            if (prefabToSpawn == null) continue;

            GameObject spawned = Instantiate(prefabToSpawn, Pylons[i].pylonTransform);
            spawned.transform.localPosition = aircraftWeaponData.Pylons[i].PylonLocalXYZ;
            spawned.transform.localRotation = Quaternion.identity;
        }
    }

    GameObject GetPrefabForWeapon(AircraftWeaponData.SelectedWeaponType weaponType)
    {
        switch (weaponType)
        {
            case AircraftWeaponData.SelectedWeaponType.AIM120C5:
                return AIM120C5Prefab;
            case AircraftWeaponData.SelectedWeaponType.AIM9M:
                return AIM9MPrefab;
            case AircraftWeaponData.SelectedWeaponType.AGM88:
                return AGM88Prefab;
            case AircraftWeaponData.SelectedWeaponType.GBU32:
                return GBU32Prefab;
            case AircraftWeaponData.SelectedWeaponType.MK84:
                return MK84Prefab;
            case AircraftWeaponData.SelectedWeaponType.FUELPOD:
                return FUELPODPrefab;
            case AircraftWeaponData.SelectedWeaponType.ZUNI:
                return ZUNIPrefab;
            default:
                return null;
        }
    }
}