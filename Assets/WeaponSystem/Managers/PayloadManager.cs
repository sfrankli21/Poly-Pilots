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

    public enum SelectedPylonSlot
    {
        Pylon0,
        Pylon1,
        Pylon2,
        Pylon3,
        Pylon4,
        Pylon5,
        Pylon6,
        Pylon7,
        Pylon8,
        Pylon9,
        Pylon10,
        Pylon11,
        Pylon12,
        Pylon13,
        Pylon14,
        Pylon15,
        Pylon16,
        Pylon17,
        Pylon18,
        Pylon19
    }

    public PylonElement[] Pylons;

    public SelectedPylonSlot CurrentSelectedPylon;
    public AircraftWeaponData.SelectedWeaponType CurrentSelectedPylonWeapon;

    public GameObject AIM120C5Prefab;
    public GameObject AIM9MPrefab;
    public GameObject AGM88Prefab;
    public GameObject GBU32Prefab;
    public GameObject MK84Prefab;
    public GameObject FUELPODPrefab;
    public GameObject ZUNIPrefab;

    [SerializeField] InputRouter inputRouter;

    WeaponReleaseListener[] spawnedWeaponListeners = new WeaponReleaseListener[20];
    GameObject[] spawnedWeaponObjects = new GameObject[20];
    bool lastReleaseWeapon;
    bool lastNextPylon;
    bool lastPreviousPylon;

    void Start()
    {
        ClampSelectedPylonToValidRange();

        if (aircraftWeaponData == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateTrajectoryPreviewVisibility();
            return;
        }

        if (aircraftWeaponData.Pylons == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateTrajectoryPreviewVisibility();
            return;
        }

        if (Pylons == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateTrajectoryPreviewVisibility();
            return;
        }

        JetMechanics jetMechanics = GetComponent<JetMechanics>();

        int count = Mathf.Min(aircraftWeaponData.Pylons.Length, Pylons.Length);

        for (int i = 0; i < count; i++)
        {
            if (aircraftWeaponData.Pylons[i] == null) continue;

            if (aircraftWeaponData.Pylons[i].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.FUELPOD)
            {
                if (jetMechanics != null)
                {
                    jetMechanics.fuelQuantity += 5000f;
                }
            }

            if (Pylons[i] == null) continue;
            if (Pylons[i].pylonTransform == null) continue;

            GameObject prefabToSpawn = GetPrefabForWeapon(aircraftWeaponData.Pylons[i].SelectedWeapon);
            if (prefabToSpawn == null) continue;

            GameObject spawned = Instantiate(prefabToSpawn, Pylons[i].pylonTransform);
            spawned.transform.localPosition = aircraftWeaponData.Pylons[i].PylonLocalXYZ;
            spawned.transform.localRotation = Quaternion.identity;
            spawnedWeaponObjects[i] = spawned;

            WeaponReleaseListener listener = spawned.GetComponent<WeaponReleaseListener>();
            if (listener != null)
            {
                listener.Setup(i, aircraftWeaponData.Pylons[i].SelectedWeapon);
                spawnedWeaponListeners[i] = listener;
            }
        }

        ClampToNearestArmedPylon();
        UpdateSelectedPylonWeaponDisplay();
        UpdateTrajectoryPreviewVisibility();
    }

    void Update()
    {
        ClampSelectedPylonToValidRange();
        HandlePylonSelectionInput();
        HandleReleaseInput();
        UpdateSelectedPylonWeaponDisplay();
        UpdateTrajectoryPreviewVisibility();
    }

    void HandlePylonSelectionInput()
    {
        bool nextPressed = inputRouter != null && inputRouter.NextPylon;
        bool previousPressed = inputRouter != null && inputRouter.PreviousPylon;

        if (nextPressed && !lastNextPylon)
        {
            SelectNextValidPylon();
        }

        if (previousPressed && !lastPreviousPylon)
        {
            SelectPreviousValidPylon();
        }

        lastNextPylon = nextPressed;
        lastPreviousPylon = previousPressed;
    }

    void SelectNextValidPylon()
    {
        if (aircraftWeaponData == null) return;
        if (aircraftWeaponData.Pylons == null) return;

        int validCount = Mathf.Min(aircraftWeaponData.Pylons.Length, 20);
        if (validCount <= 0) return;

        int currentIndex = Mathf.Clamp((int)CurrentSelectedPylon, 0, validCount - 1);

        for (int step = 1; step <= validCount; step++)
        {
            int index = (currentIndex + step) % validCount;

            if (IsPylonSelectable(index))
            {
                CurrentSelectedPylon = (SelectedPylonSlot)index;
                return;
            }
        }
    }

    void SelectPreviousValidPylon()
    {
        if (aircraftWeaponData == null) return;
        if (aircraftWeaponData.Pylons == null) return;

        int validCount = Mathf.Min(aircraftWeaponData.Pylons.Length, 20);
        if (validCount <= 0) return;

        int currentIndex = Mathf.Clamp((int)CurrentSelectedPylon, 0, validCount - 1);

        for (int step = 1; step <= validCount; step++)
        {
            int index = currentIndex - step;
            if (index < 0)
            {
                index += validCount;
            }

            if (IsPylonSelectable(index))
            {
                CurrentSelectedPylon = (SelectedPylonSlot)index;
                return;
            }
        }
    }

    bool IsPylonSelectable(int index)
    {
        if (aircraftWeaponData == null) return false;
        if (aircraftWeaponData.Pylons == null) return false;
        if (index < 0 || index >= aircraftWeaponData.Pylons.Length) return false;
        if (aircraftWeaponData.Pylons[index] == null) return false;
        if (aircraftWeaponData.Pylons[index].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.None) return false;

        return true;
    }

    void ClampToNearestArmedPylon()
    {
        if (aircraftWeaponData == null) return;
        if (aircraftWeaponData.Pylons == null) return;

        int validCount = Mathf.Min(aircraftWeaponData.Pylons.Length, 20);
        if (validCount <= 0)
        {
            CurrentSelectedPylon = SelectedPylonSlot.Pylon0;
            return;
        }

        int currentIndex = Mathf.Clamp((int)CurrentSelectedPylon, 0, validCount - 1);

        if (IsPylonSelectable(currentIndex))
        {
            return;
        }

        for (int i = 0; i < validCount; i++)
        {
            if (IsPylonSelectable(i))
            {
                CurrentSelectedPylon = (SelectedPylonSlot)i;
                return;
            }
        }

        CurrentSelectedPylon = SelectedPylonSlot.Pylon0;
    }

    void HandleReleaseInput()
    {
        bool releasePressed = inputRouter != null && inputRouter.ReleaseWeapon;

        if (releasePressed && !lastReleaseWeapon)
        {
            TryReleaseSelectedPylonWeapon();
        }

        lastReleaseWeapon = releasePressed;
    }

    void TryReleaseSelectedPylonWeapon()
    {
        if (aircraftWeaponData == null) return;
        if (aircraftWeaponData.Pylons == null) return;

        int pylonIndex = (int)CurrentSelectedPylon;

        if (pylonIndex < 0 || pylonIndex >= aircraftWeaponData.Pylons.Length) return;
        if (pylonIndex >= spawnedWeaponListeners.Length) return;

        WeaponReleaseListener listener = spawnedWeaponListeners[pylonIndex];
        if (listener == null) return;

        AircraftWeaponData.SelectedWeaponType weaponType = listener.weaponType;

        if (weaponType == AircraftWeaponData.SelectedWeaponType.MK84)
        {
            listener.TriggerReleaseApproved();
            spawnedWeaponListeners[pylonIndex] = null;
            spawnedWeaponObjects[pylonIndex] = null;
            aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon = AircraftWeaponData.SelectedWeaponType.None;
            ClampToNearestArmedPylon();
            UpdateSelectedPylonWeaponDisplay();
            UpdateTrajectoryPreviewVisibility();
        }
    }

    void ClampSelectedPylonToValidRange()
    {
        if (aircraftWeaponData == null || aircraftWeaponData.Pylons == null || aircraftWeaponData.Pylons.Length == 0)
        {
            CurrentSelectedPylon = SelectedPylonSlot.Pylon0;
            return;
        }

        int maxValidIndex = Mathf.Clamp(aircraftWeaponData.Pylons.Length - 1, 0, 19);
        int currentIndex = (int)CurrentSelectedPylon;

        if (currentIndex > maxValidIndex)
        {
            CurrentSelectedPylon = (SelectedPylonSlot)maxValidIndex;
        }
    }

    void UpdateSelectedPylonWeaponDisplay()
    {
        if (aircraftWeaponData == null)
        {
            CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
            return;
        }

        if (aircraftWeaponData.Pylons == null)
        {
            CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
            return;
        }

        if (aircraftWeaponData.Pylons.Length == 0)
        {
            CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
            return;
        }

        int pylonIndex = (int)CurrentSelectedPylon;

        if (pylonIndex < 0 || pylonIndex >= aircraftWeaponData.Pylons.Length)
        {
            CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
            return;
        }

        if (aircraftWeaponData.Pylons[pylonIndex] == null)
        {
            CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
            return;
        }

        CurrentSelectedPylonWeapon = aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon;
    }

    void UpdateTrajectoryPreviewVisibility()
    {
        for (int i = 0; i < spawnedWeaponObjects.Length; i++)
        {
            GameObject weaponObject = spawnedWeaponObjects[i];

            if (weaponObject == null)
            {
                continue;
            }

            MK84TrajectoryPreview preview = weaponObject.GetComponent<MK84TrajectoryPreview>();
            if (preview == null)
            {
                continue;
            }

            bool shouldShow = false;

            if (aircraftWeaponData != null && aircraftWeaponData.Pylons != null)
            {
                if (i >= 0 && i < aircraftWeaponData.Pylons.Length)
                {
                    if (aircraftWeaponData.Pylons[i] != null)
                    {
                        shouldShow =
                            i == (int)CurrentSelectedPylon &&
                            aircraftWeaponData.Pylons[i].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.MK84;
                    }
                }
            }

            preview.SetPreviewVisible(shouldShow);
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