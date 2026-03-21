using System;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;

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
    [SerializeField] NewRadar radar;
    [SerializeField] Rigidbody aircraftRigidbody;

    public Transform RadarSelectedContactTransform;
    public bool AIM120TargetEstablished;

    public Transform AimPointOrigin;
    public Transform MuzzlePoint;
    public LayerMask hitMask;
    public GameObject bulletPrefab;
    public float fireRateRPM;
    public int ammoCapacity;
    public int currentAmmo;
    public int poolSize = 100;
    public EventReference fireLoopEvent;
    public string fireExitParameterName;
    public float fireExitParameterValue = 1f;
    public List<TMP_Text> TextDisplays = new List<TMP_Text>();
    public Transform BulletPool;
    public bool AutoRefill;
    public float autoRefillDelay;
    public int trajectoryStepCount = 120;
    public float trajectoryTimeStep = 0.02f;
    public GameObject closestHitPrefab;
    public Transform closestHitBillboardTarget;
    public Vector3 closestHitMinScale = new Vector3(15f, 15f, 15f);
    public Vector3 closestHitMaxScale = new Vector3(200f, 200f, 200f);
    public float closestHitMinScaleDistance = 0f;
    public float closestHitMaxScaleDistance = 10000f;

    public string SelectedPylonTextTargetTag;
    public List<TMP_Text> SelectedPylonText = new List<TMP_Text>();

    public string AIM120C5Prefix;
    public string AIM9MPrefix;
    public string AGM88Prefix;
    public string GBU32Prefix;
    public string MK84Prefix;
    public string FUELPODPrefix;
    public string ZUNIPrefix;
    public string NonePrefix;

    WeaponReleaseListener[] spawnedWeaponListeners = new WeaponReleaseListener[20];
    GameObject[] spawnedWeaponObjects = new GameObject[20];
    bool lastReleaseWeapon;
    bool lastNextPylon;
    bool lastPreviousPylon;

    float fireAccumulator;
    float refillTimer;
    bool refillCountdownActive;
    bool wasTryingToFireLastFrame;
    readonly List<GameObject> bulletPool = new List<GameObject>();
    EventInstance fireLoopInstance;
    bool fireLoopInstanceCreated;
    bool fireLoopExiting;
    GameObject closestHitInstance;
    bulletLogic bulletData;

    void Start()
    {
        if (aircraftRigidbody == null)
        {
            aircraftRigidbody = GetComponent<Rigidbody>();
            if (aircraftRigidbody == null)
            {
                aircraftRigidbody = GetComponentInParent<Rigidbody>();
            }
        }

        PopulateSelectedPylonTextFromTag();

        currentAmmo = ammoCapacity;

        if (bulletPrefab != null)
        {
            for (int i = 0; i < poolSize; i++)
            {
                GameObject bullet = Instantiate(bulletPrefab, BulletPool);
                bullet.SetActive(false);
                bulletPool.Add(bullet);
            }
        }

        UpdateAmmoDisplays();

        ClampSelectedPylonToValidRange();

        if (aircraftWeaponData == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateSelectedPylonTextDisplay();
            UpdateTrajectoryPreviewVisibility();
            UpdateRadarSelectedContact();
            UpdateAIM120TargetEstablished();
            return;
        }

        if (aircraftWeaponData.Pylons == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateSelectedPylonTextDisplay();
            UpdateTrajectoryPreviewVisibility();
            UpdateRadarSelectedContact();
            UpdateAIM120TargetEstablished();
            return;
        }

        if (Pylons == null)
        {
            UpdateSelectedPylonWeaponDisplay();
            UpdateSelectedPylonTextDisplay();
            UpdateTrajectoryPreviewVisibility();
            UpdateRadarSelectedContact();
            UpdateAIM120TargetEstablished();
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
        UpdateSelectedPylonTextDisplay();
        UpdateTrajectoryPreviewVisibility();
        UpdateRadarSelectedContact();
        UpdateAIM120TargetEstablished();
    }

    void Update()
    {
        ClampSelectedPylonToValidRange();
        HandlePylonSelectionInput();
        HandleReleaseInput();
        UpdateSelectedPylonWeaponDisplay();
        UpdateSelectedPylonTextDisplay();
        UpdateTrajectoryPreviewVisibility();
        UpdateRadarSelectedContact();
        UpdateAIM120TargetEstablished();
        UpdateGunSystem();
    }

    void LateUpdate()
    {
        UpdateClosestHitVisuals();
    }

    void UpdateRadarSelectedContact()
    {
        if (radar == null)
        {
            RadarSelectedContactTransform = null;
            return;
        }

        RadarSelectedContactTransform = radar.SelectedContactTransform;
    }

    void UpdateAIM120TargetEstablished()
    {
        AIM120TargetEstablished = false;

        for (int i = 0; i < spawnedWeaponObjects.Length; i++)
        {
            GameObject loopWeaponObject = spawnedWeaponObjects[i];
            if (loopWeaponObject == null)
            {
                continue;
            }

            AIM120GuidanceLogic aim120Loop = loopWeaponObject.GetComponent<AIM120GuidanceLogic>();
            if (aim120Loop != null)
            {
                aim120Loop.SetPreReleaseSeekerActive(false);
            }
        }

        if (CurrentSelectedPylonWeapon != AircraftWeaponData.SelectedWeaponType.AIM120C5)
        {
            return;
        }

        if (RadarSelectedContactTransform == null)
        {
            return;
        }

        int pylonIndex = (int)CurrentSelectedPylon;
        if (pylonIndex < 0 || pylonIndex >= spawnedWeaponObjects.Length)
        {
            return;
        }

        GameObject selectedWeaponObject = spawnedWeaponObjects[pylonIndex];
        if (selectedWeaponObject == null)
        {
            return;
        }

        AIM120GuidanceLogic aim120 = selectedWeaponObject.GetComponent<AIM120GuidanceLogic>();
        if (aim120 == null)
        {
            return;
        }

        aim120.SetPreReleaseSeekerActive(true);
        aim120.SetTarget(RadarSelectedContactTransform);
        AIM120TargetEstablished = aim120.targetLocked;
    }

    void PopulateSelectedPylonTextFromTag()
    {
        SelectedPylonText.Clear();

        if (string.IsNullOrEmpty(SelectedPylonTextTargetTag))
        {
            return;
        }

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(SelectedPylonTextTargetTag);

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            if (taggedObjects[i] == null)
            {
                continue;
            }

            TMP_Text textComponent = taggedObjects[i].GetComponent<TMP_Text>();

            if (textComponent != null)
            {
                SelectedPylonText.Add(textComponent);
            }
        }
    }

    void UpdateSelectedPylonTextDisplay()
    {
        string displayText = GetSelectedPylonDisplayText();

        for (int i = 0; i < SelectedPylonText.Count; i++)
        {
            if (SelectedPylonText[i] != null)
            {
                SelectedPylonText[i].text = displayText;
            }
        }
    }

    string GetSelectedPylonDisplayText()
    {
        string prefix = GetSelectedPylonPrefix();
        string suffix = GetSelectedPylonSuffixText();

        if (string.IsNullOrEmpty(prefix))
        {
            return suffix;
        }

        if (string.IsNullOrEmpty(suffix))
        {
            return prefix;
        }

        return prefix + ": " + suffix;
    }

    string GetSelectedPylonPrefix()
    {
        switch (CurrentSelectedPylonWeapon)
        {
            case AircraftWeaponData.SelectedWeaponType.AIM120C5:
                return AIM120C5Prefix;
            case AircraftWeaponData.SelectedWeaponType.AIM9M:
                return AIM9MPrefix;
            case AircraftWeaponData.SelectedWeaponType.AGM88:
                return AGM88Prefix;
            case AircraftWeaponData.SelectedWeaponType.GBU32:
                return GBU32Prefix;
            case AircraftWeaponData.SelectedWeaponType.MK84:
                return MK84Prefix;
            case AircraftWeaponData.SelectedWeaponType.FUELPOD:
                return FUELPODPrefix;
            case AircraftWeaponData.SelectedWeaponType.ZUNI:
                return ZUNIPrefix;
            default:
                return NonePrefix;
        }
    }

    string GetSelectedPylonSuffixText()
    {
        switch (CurrentSelectedPylonWeapon)
        {
            case AircraftWeaponData.SelectedWeaponType.AIM120C5:
                return CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType.AIM120C5).ToString();
            case AircraftWeaponData.SelectedWeaponType.AIM9M:
                return CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType.AIM9M).ToString();
            case AircraftWeaponData.SelectedWeaponType.AGM88:
                return CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType.AGM88).ToString();
            case AircraftWeaponData.SelectedWeaponType.GBU32:
                return CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType.GBU32).ToString();
            case AircraftWeaponData.SelectedWeaponType.MK84:
                return CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType.MK84).ToString();
            case AircraftWeaponData.SelectedWeaponType.ZUNI:
                return CountTotalRemainingZuniRockets().ToString();
            default:
                return string.Empty;
        }
    }

    int CountWeaponsOnPylons(AircraftWeaponData.SelectedWeaponType weaponType)
    {
        if (aircraftWeaponData == null || aircraftWeaponData.Pylons == null)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < aircraftWeaponData.Pylons.Length; i++)
        {
            if (aircraftWeaponData.Pylons[i] == null)
            {
                continue;
            }

            if (aircraftWeaponData.Pylons[i].SelectedWeapon == weaponType)
            {
                count++;
            }
        }

        return count;
    }

    int CountTotalRemainingZuniRockets()
    {
        int total = 0;

        for (int i = 0; i < spawnedWeaponObjects.Length; i++)
        {
            GameObject weaponObject = spawnedWeaponObjects[i];

            if (weaponObject == null)
            {
                continue;
            }

            ZuniPodRelease zuniPod = weaponObject.GetComponent<ZuniPodRelease>();
            if (zuniPod == null)
            {
                continue;
            }

            total += zuniPod.GetRemainingRocketCount();
        }

        return total;
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

        if (aircraftWeaponData.Pylons[index].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.ZUNI)
        {
            if (index >= 0 && index < spawnedWeaponObjects.Length)
            {
                GameObject weaponObject = spawnedWeaponObjects[index];
                if (weaponObject != null)
                {
                    ZuniPodRelease zuniPod = weaponObject.GetComponent<ZuniPodRelease>();
                    if (zuniPod != null && !zuniPod.HasAmmo())
                    {
                        return false;
                    }
                }
            }
        }

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

    void SelectNextSameWeaponPylon(AircraftWeaponData.SelectedWeaponType weaponType, int previousPylonIndex)
    {
        if (aircraftWeaponData == null || aircraftWeaponData.Pylons == null)
        {
            ClampToNearestArmedPylon();
            return;
        }

        int validCount = Mathf.Min(aircraftWeaponData.Pylons.Length, 20);
        if (validCount <= 0)
        {
            CurrentSelectedPylon = SelectedPylonSlot.Pylon0;
            return;
        }

        int startIndex = Mathf.Clamp(previousPylonIndex, 0, validCount - 1);

        for (int step = 1; step <= validCount; step++)
        {
            int index = (startIndex + step) % validCount;

            if (index < 0 || index >= aircraftWeaponData.Pylons.Length)
            {
                continue;
            }

            if (aircraftWeaponData.Pylons[index] == null)
            {
                continue;
            }

            if (aircraftWeaponData.Pylons[index].SelectedWeapon != weaponType)
            {
                continue;
            }

            if (!IsPylonSelectable(index))
            {
                continue;
            }

            CurrentSelectedPylon = (SelectedPylonSlot)index;
            return;
        }

        ClampToNearestArmedPylon();
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
        GameObject weaponObject = spawnedWeaponObjects[pylonIndex];

        if (weaponType == AircraftWeaponData.SelectedWeaponType.AIM120C5)
        {
            if (!AIM120TargetEstablished) return;
            if (RadarSelectedContactTransform == null) return;
            if (weaponObject == null) return;

            AIM120GuidanceLogic aim120 = weaponObject.GetComponent<AIM120GuidanceLogic>();
            if (aim120 == null) return;

            aim120.SetTarget(RadarSelectedContactTransform);

            if (aircraftRigidbody != null)
            {
                aim120.SetLaunchVelocity(aircraftRigidbody.linearVelocity);
            }

            listener.TriggerReleaseApproved();

            spawnedWeaponListeners[pylonIndex] = null;
            spawnedWeaponObjects[pylonIndex] = null;
            aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon = AircraftWeaponData.SelectedWeaponType.None;
            SelectNextSameWeaponPylon(AircraftWeaponData.SelectedWeaponType.AIM120C5, pylonIndex);
            UpdateSelectedPylonWeaponDisplay();
            UpdateSelectedPylonTextDisplay();
            UpdateTrajectoryPreviewVisibility();
            UpdateAIM120TargetEstablished();
            return;
        }

        if (weaponType == AircraftWeaponData.SelectedWeaponType.MK84)
        {
            listener.TriggerReleaseApproved();
            spawnedWeaponListeners[pylonIndex] = null;
            spawnedWeaponObjects[pylonIndex] = null;
            aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon = AircraftWeaponData.SelectedWeaponType.None;
            SelectNextSameWeaponPylon(AircraftWeaponData.SelectedWeaponType.MK84, pylonIndex);
            UpdateSelectedPylonWeaponDisplay();
            UpdateSelectedPylonTextDisplay();
            UpdateTrajectoryPreviewVisibility();
            UpdateAIM120TargetEstablished();
            return;
        }

        if (weaponType == AircraftWeaponData.SelectedWeaponType.ZUNI)
        {
            if (weaponObject == null) return;

            ZuniPodRelease zuniPod = weaponObject.GetComponent<ZuniPodRelease>();
            if (zuniPod == null) return;
            if (!zuniPod.HasAmmo()) return;

            listener.TriggerReleaseApproved();

            UpdateSelectedPylonTextDisplay();

            if (!zuniPod.HasAmmo())
            {
                aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon = AircraftWeaponData.SelectedWeaponType.None;
                spawnedWeaponListeners[pylonIndex] = null;
                SelectNextSameWeaponPylon(AircraftWeaponData.SelectedWeaponType.ZUNI, pylonIndex);
                UpdateSelectedPylonWeaponDisplay();
                UpdateSelectedPylonTextDisplay();
                UpdateTrajectoryPreviewVisibility();
                UpdateAIM120TargetEstablished();
            }
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

        if (aircraftWeaponData.Pylons[pylonIndex].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.ZUNI)
        {
            if (pylonIndex >= 0 && pylonIndex < spawnedWeaponObjects.Length)
            {
                GameObject weaponObject = spawnedWeaponObjects[pylonIndex];
                if (weaponObject != null)
                {
                    ZuniPodRelease zuniPod = weaponObject.GetComponent<ZuniPodRelease>();
                    if (zuniPod != null && !zuniPod.HasAmmo())
                    {
                        CurrentSelectedPylonWeapon = AircraftWeaponData.SelectedWeaponType.None;
                        return;
                    }
                }
            }
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

            MK84TrajectoryPreview mk84Preview = weaponObject.GetComponent<MK84TrajectoryPreview>();
            if (mk84Preview != null)
            {
                bool showMk84 = false;

                if (aircraftWeaponData != null && aircraftWeaponData.Pylons != null)
                {
                    if (i >= 0 && i < aircraftWeaponData.Pylons.Length)
                    {
                        if (aircraftWeaponData.Pylons[i] != null)
                        {
                            showMk84 =
                                i == (int)CurrentSelectedPylon &&
                                aircraftWeaponData.Pylons[i].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.MK84;
                        }
                    }
                }

                mk84Preview.SetPreviewVisible(showMk84);
            }

            ZUNITrajectoryPreview zuniPreview = weaponObject.GetComponent<ZUNITrajectoryPreview>();
            if (zuniPreview != null)
            {
                bool showZuni = false;

                if (aircraftWeaponData != null && aircraftWeaponData.Pylons != null)
                {
                    if (i >= 0 && i < aircraftWeaponData.Pylons.Length)
                    {
                        if (aircraftWeaponData.Pylons[i] != null)
                        {
                            showZuni =
                                i == (int)CurrentSelectedPylon &&
                                aircraftWeaponData.Pylons[i].SelectedWeapon == AircraftWeaponData.SelectedWeaponType.ZUNI;
                        }
                    }
                }

                zuniPreview.SetPreviewVisible(showZuni);
            }
        }
    }

    void UpdateGunSystem()
    {
        if (AimPointOrigin == null || MuzzlePoint == null)
        {
            DestroyClosestHitPrefab();
            return;
        }

        UpdateAimFromTrajectory();
        HandleAutoRefill();
        UpdateFireAudio();

        if (inputRouter == null || !inputRouter.ShootGun || fireRateRPM <= 0f || currentAmmo <= 0 || bulletPrefab == null)
        {
            UpdateAmmoDisplays();
            return;
        }

        float secondsPerShot = 60f / fireRateRPM;
        fireAccumulator += Time.deltaTime;

        while (fireAccumulator >= secondsPerShot && currentAmmo > 0)
        {
            GameObject bullet = GetPooledBullet();
            if (bullet == null)
            {
                break;
            }

            bullet.transform.SetParent(null, true);
            bullet.transform.SetPositionAndRotation(MuzzlePoint.position, MuzzlePoint.rotation);
            bullet.SetActive(true);

            currentAmmo--;
            fireAccumulator -= secondsPerShot;
            UpdateAmmoDisplays();

            if (currentAmmo <= 0)
            {
                break;
            }
        }

        UpdateAmmoDisplays();
    }

    void UpdateAimFromTrajectory()
    {
        if (bulletPrefab == null)
        {
            ResetMuzzleYawAndHitPrefab();
            return;
        }

        if (bulletData == null || bulletData.gameObject != bulletPrefab)
        {
            bulletData = bulletPrefab.GetComponent<bulletLogic>();
        }

        if (bulletData == null)
        {
            ResetMuzzleYawAndHitPrefab();
            return;
        }

        Vector3 currentPosition = AimPointOrigin.position;
        Vector3 currentVelocity = AimPointOrigin.forward * bulletData.speed;
        Vector3 gravity = Physics.gravity;

        float maxLifeTime = bulletData.lifeTime;
        int maxStepsFromLifetime = trajectoryTimeStep > 0f ? Mathf.CeilToInt(maxLifeTime / trajectoryTimeStep) : trajectoryStepCount;
        int finalStepCount = Mathf.Max(2, Mathf.Min(trajectoryStepCount, maxStepsFromLifetime));

        List<Vector3> points = new List<Vector3>();
        points.Add(currentPosition);

        RaycastHit firstHit = default;
        bool foundHit = false;

        for (int i = 0; i < finalStepCount; i++)
        {
            Vector3 nextPosition = currentPosition + currentVelocity * trajectoryTimeStep + 0.5f * gravity * trajectoryTimeStep * trajectoryTimeStep;
            Vector3 nextVelocity = currentVelocity + gravity * trajectoryTimeStep;
            Vector3 segment = nextPosition - currentPosition;
            float segmentDistance = segment.magnitude;

            if (segmentDistance > 0f)
            {
                if (Physics.Raycast(currentPosition, segment.normalized, out RaycastHit hit, segmentDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    firstHit = hit;
                    foundHit = true;
                    points.Add(hit.point);
                    break;
                }
            }

            points.Add(nextPosition);
            currentPosition = nextPosition;
            currentVelocity = nextVelocity;
        }

        Vector3 localEuler = MuzzlePoint.localEulerAngles;

        if (foundHit)
        {
            Vector3 worldDirectionToHit = firstHit.point - MuzzlePoint.position;

            if (MuzzlePoint.parent != null)
            {
                Vector3 localDirectionToHit = MuzzlePoint.parent.InverseTransformDirection(worldDirectionToHit.normalized);
                float yAngle = Mathf.Atan2(localDirectionToHit.x, localDirectionToHit.z) * Mathf.Rad2Deg;
                localEuler.y = yAngle;
            }
            else
            {
                float yAngle = Mathf.Atan2(worldDirectionToHit.x, worldDirectionToHit.z) * Mathf.Rad2Deg;
                localEuler.y = yAngle;
            }

            UpdateClosestHitPrefab(firstHit.point);
        }
        else
        {
            localEuler.y = 0f;
            DestroyClosestHitPrefab();
        }

        MuzzlePoint.localEulerAngles = localEuler;
    }

    void ResetMuzzleYawAndHitPrefab()
    {
        Vector3 localEuler = MuzzlePoint.localEulerAngles;
        localEuler.y = 0f;
        MuzzlePoint.localEulerAngles = localEuler;
        DestroyClosestHitPrefab();
    }

    void UpdateClosestHitPrefab(Vector3 hitPoint)
    {
        if (closestHitPrefab == null)
        {
            return;
        }

        if (closestHitInstance == null)
        {
            closestHitInstance = Instantiate(closestHitPrefab, hitPoint, Quaternion.identity);
        }
        else
        {
            closestHitInstance.transform.position = hitPoint;
        }

        UpdateClosestHitVisuals();
    }

    void UpdateClosestHitVisuals()
    {
        if (closestHitInstance == null)
        {
            return;
        }

        if (closestHitBillboardTarget != null)
        {
            Vector3 directionToTarget = closestHitBillboardTarget.position - closestHitInstance.transform.position;

            if (directionToTarget.sqrMagnitude > 0.0001f)
            {
                closestHitInstance.transform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            }

            float distanceToTarget = Vector3.Distance(closestHitBillboardTarget.position, closestHitInstance.transform.position);
            float scaleT = closestHitMaxScaleDistance <= closestHitMinScaleDistance
                ? 1f
                : Mathf.InverseLerp(closestHitMinScaleDistance, closestHitMaxScaleDistance, distanceToTarget);

            closestHitInstance.transform.localScale = Vector3.Lerp(closestHitMinScale, closestHitMaxScale, scaleT);
        }
    }

    void DestroyClosestHitPrefab()
    {
        if (closestHitInstance != null)
        {
            Destroy(closestHitInstance);
            closestHitInstance = null;
        }
    }

    void HandleAutoRefill()
    {
        if (!AutoRefill)
        {
            refillCountdownActive = false;
            refillTimer = 0f;
            return;
        }

        if (currentAmmo > 0)
        {
            refillCountdownActive = false;
            refillTimer = 0f;
            return;
        }

        if (!refillCountdownActive)
        {
            refillCountdownActive = true;
            refillTimer = autoRefillDelay;
        }

        refillTimer -= Time.deltaTime;

        if (refillTimer <= 0f)
        {
            currentAmmo = ammoCapacity;
            refillCountdownActive = false;
            refillTimer = 0f;
            UpdateAmmoDisplays();
        }
    }

    void UpdateFireAudio()
    {
        bool wantsToFire = inputRouter != null && inputRouter.ShootGun && currentAmmo > 0;
        bool fireStartedThisFrame = wantsToFire && !wasTryingToFireLastFrame;

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

            PLAYBACK_STATE playbackState;
            fireLoopInstance.getPlaybackState(out playbackState);

            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                fireLoopInstance.release();
                fireLoopInstanceCreated = false;
                fireLoopExiting = false;
            }
        }

        if (wantsToFire)
        {
            if (fireStartedThisFrame)
            {
                RestartFireLoop();
            }
            else if (!fireLoopInstanceCreated)
            {
                StartFireLoop();
            }
        }
        else
        {
            ExitFireLoop();
        }

        wasTryingToFireLastFrame = wantsToFire;
    }

    void StartFireLoop()
    {
        if (fireLoopEvent.IsNull)
        {
            return;
        }

        if (fireLoopInstanceCreated)
        {
            PLAYBACK_STATE playbackState;
            fireLoopInstance.getPlaybackState(out playbackState);

            if (playbackState != PLAYBACK_STATE.STOPPED)
            {
                return;
            }

            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }

        fireLoopInstance = RuntimeManager.CreateInstance(fireLoopEvent);
        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, 0f);
        }

        fireLoopInstance.start();
        fireLoopInstanceCreated = true;
        fireLoopExiting = false;
    }

    void RestartFireLoop()
    {
        if (fireLoopEvent.IsNull)
        {
            return;
        }

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }

        fireLoopInstance = RuntimeManager.CreateInstance(fireLoopEvent);
        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, 0f);
        }

        fireLoopInstance.start();
        fireLoopInstanceCreated = true;
        fireLoopExiting = false;
    }

    void ExitFireLoop()
    {
        if (!fireLoopInstanceCreated || fireLoopExiting)
        {
            return;
        }

        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, fireExitParameterValue);
        }
        else
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        fireLoopExiting = true;
    }

    GameObject GetPooledBullet()
    {
        for (int i = 0; i < bulletPool.Count; i++)
        {
            if (!bulletPool[i].activeInHierarchy)
            {
                return bulletPool[i];
            }
        }

        if (bulletPrefab == null)
        {
            return null;
        }

        GameObject bullet = Instantiate(bulletPrefab, BulletPool);
        bullet.SetActive(false);
        bulletPool.Add(bullet);
        return bullet;
    }

    void UpdateAmmoDisplays()
    {
        string ammoText = currentAmmo.ToString();

        for (int i = 0; i < TextDisplays.Count; i++)
        {
            if (TextDisplays[i] != null)
            {
                TextDisplays[i].text = ammoText;
            }
        }
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        if (bullet == null)
        {
            return;
        }

        bullet.SetActive(false);

        if (BulletPool != null)
        {
            bullet.transform.SetParent(BulletPool, false);
        }
        else
        {
            bullet.transform.SetParent(transform, false);
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

    void OnDisable()
    {
        wasTryingToFireLastFrame = false;
        DestroyClosestHitPrefab();

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }
    }

    void OnDestroy()
    {
        DestroyClosestHitPrefab();

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }
    }
}