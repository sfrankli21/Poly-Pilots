using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using FMODUnity;

public class NewRadar : MonoBehaviour
{
    public enum RadarMode
    {
        TWS_30X30,
        TWS_10X10,
        TWS_20X20,
        TWS_50X20,
        HMD_10X10
    }

    public enum RadarRange
    {
        Range_5000,
        Range_7500,
        Range_10000
    }

    [SerializeField, InspectorName("Gimble")]
    GameObject gimble;

    [SerializeField, InspectorName("Radar Mode")]
    RadarMode radarMode;

    [SerializeField, InspectorName("Radar Range")]
    RadarRange radarRange;

    [SerializeField, InspectorName("HMD Controlling Radar")]
    bool HMDControllingRadar = false;

    [SerializeField, InspectorName("Gimble Min Y")]
    float gimbleMinY = -85f;

    [SerializeField, InspectorName("Gimble Max Y")]
    float gimbleMaxY = 85f;

    [SerializeField, InspectorName("Gimble Min X")]
    float gimbleMinX = -85f;

    [SerializeField, InspectorName("Gimble Max X")]
    float gimbleMaxX = 85f;

    [SerializeField, InspectorName("Recenter Delay")]
    float recenterDelay = 0.1f;

    [SerializeField, InspectorName("Gizmo Segments")]
    int gizmoSegments = 32;

    [SerializeField, InspectorName("RCS Tag")]
    string rcsTag = "RCS";

    [SerializeField, InspectorName("Scan Interval")]
    float scanInterval = 0.1f;

    [SerializeField, InspectorName("Debug Rays")]
    bool debugRays = true;

    [SerializeField, InspectorName("Track Ray Gizmos")]
    bool rayGizmos = true;

    [SerializeField, InspectorName("Max Track Rays")]
    int maxRayGizmos = 128;

    [SerializeField, InspectorName("Track Ray Hit Radius")]
    float trackRayHitRadius = 0.1f;

    [SerializeField, InspectorName("Radar Contacts")]
    List<GameObject> radarContacts = new List<GameObject>();

    [SerializeField, InspectorName("Radar Display Tag")]
    string radarDisplayTag = "RadarDisplay";

    [SerializeField, InspectorName("Radar Blip Prefab")]
    GameObject radarBlipPrefab;

    [SerializeField, InspectorName("Clamp To Screen")]
    bool clampToScreen = true;

    [SerializeField, InspectorName("Distance Suffix")]
    string distanceSuffix = "";

    [SerializeField, InspectorName("Display Refresh Interval")]
    float displayRefreshInterval = 0.25f;

    [SerializeField, InspectorName("HUD Radar Blip Prefab")]
    GameObject hudRadarBlipPrefab;

    [SerializeField, InspectorName("Player Camera")]
    Transform playerCamera;

    [SerializeField, InspectorName("HUD Min Scale")]
    Vector3 hudMinScale = new Vector3(15f, 15f, 15f);

    [SerializeField, InspectorName("HUD Max Scale")]
    Vector3 hudMaxScale = new Vector3(200f, 200f, 200f);

    [SerializeField, InspectorName("HUD Min Scale Distance")]
    float hudMinScaleDistance = 0f;

    [SerializeField, InspectorName("HUD Max Scale Distance")]
    float hudMaxScaleDistance = 10000f;

    [SerializeField, InspectorName("Clamp HUD To Segment")]
    bool clampHudToSegment = true;

    [SerializeField, InspectorName("Next Contact Input")]
    InputActionReference nextContactInput;

    [SerializeField, InspectorName("Previous Contact Input")]
    InputActionReference previousContactInput;

    [SerializeField, InspectorName("Selected Contact Index")]
    int selectedContactIndex = -1;

    [SerializeField, InspectorName("Selected Contact")]
    GameObject selectedContact;

    [SerializeField, InspectorName("Selected Marker Child Name")]
    string selectedMarkerChildName = "RadarContactSelectedMarker";

    [SerializeField, InspectorName("Radar Setting Display Tag")]
    string radarSettingDisplayTag = "RadarSettingDisplay";

    [SerializeField, InspectorName("Setting Display Refresh Interval")]
    float settingDisplayRefreshInterval = 0.25f;

    [SerializeField, InspectorName("New Contact Event")]
    EventReference newContactEvent;

    HashSet<GameObject> contactsSet = new HashSet<GameObject>();

    Vector3[] rayStarts;
    Vector3[] rayEnds;
    Color[] rayColors;
    int rayCount;

    float scanTimer;
    float displayRefreshTimer;
    float settingDisplayTimer;
    float recenterTimer;

    RadarMode previousRadarMode;

    struct BlipInfo
    {
        public RectTransform rt;
        public TMP_Text text;
        public GameObject selectedMarker;
    }

    struct HudBlipInfo
    {
        public Transform t;
        public GameObject selectedMarker;
    }

    readonly List<RectTransform> activeScreens = new List<RectTransform>(16);
    readonly Dictionary<RectTransform, Dictionary<GameObject, BlipInfo>> screenBlips = new Dictionary<RectTransform, Dictionary<GameObject, BlipInfo>>(16);
    readonly Dictionary<GameObject, HudBlipInfo> hudBlips = new Dictionary<GameObject, HudBlipInfo>(64);

    readonly List<TMP_Text> settingDisplays = new List<TMP_Text>(16);

    RadarMode lastMode;
    RadarRange lastRange;

    public GameObject SelectedContactObject => selectedContact;
    public Transform SelectedContactTransform => selectedContact != null ? selectedContact.transform : null;

    void Awake()
    {
        AllocateRayBuffers();
        RebuildContactSetFromList();
        RefreshScreens();
        SyncHudBlips();
        ValidateSelection();
        RefreshSettingDisplays();
        UpdateSettingDisplays(true);

        lastMode = radarMode;
        lastRange = radarRange;
        previousRadarMode = radarMode;
        ApplyRadarModeState(true);
    }

    void OnEnable()
    {
        RefreshScreens();
        SyncHudBlips();
        ValidateSelection();
        RefreshSettingDisplays();
        UpdateSettingDisplays(true);
        previousRadarMode = radarMode;
        ApplyRadarModeState(true);

        if (nextContactInput != null && nextContactInput.action != null)
        {
            nextContactInput.action.performed += OnNextContactPerformed;
            nextContactInput.action.Enable();
        }

        if (previousContactInput != null && previousContactInput.action != null)
        {
            previousContactInput.action.performed += OnPreviousContactPerformed;
            previousContactInput.action.Enable();
        }
    }

    void OnDisable()
    {
        if (nextContactInput != null && nextContactInput.action != null)
        {
            nextContactInput.action.performed -= OnNextContactPerformed;
            nextContactInput.action.Disable();
        }

        if (previousContactInput != null && previousContactInput.action != null)
        {
            previousContactInput.action.performed -= OnPreviousContactPerformed;
            previousContactInput.action.Disable();
        }
    }

    void OnValidate()
    {
        AllocateRayBuffers();
        if (trackRayHitRadius < 0f) trackRayHitRadius = 0f;
        if (scanInterval < 0f) scanInterval = 0f;
        if (displayRefreshInterval < 0f) displayRefreshInterval = 0f;
        if (settingDisplayRefreshInterval < 0f) settingDisplayRefreshInterval = 0f;
        if (recenterDelay < 0f) recenterDelay = 0f;
        if (gizmoSegments < 3) gizmoSegments = 3;
        if (selectedMarkerChildName == null) selectedMarkerChildName = "";
        if (radarSettingDisplayTag == null) radarSettingDisplayTag = "";
        if (hudMaxScaleDistance < hudMinScaleDistance) hudMaxScaleDistance = hudMinScaleDistance;
        if (gimbleMaxY < gimbleMinY) gimbleMaxY = gimbleMinY;
        if (gimbleMaxX < gimbleMinX) gimbleMaxX = gimbleMinX;
        RebuildContactSetFromList();
        ValidateSelection();
    }

    void AllocateRayBuffers()
    {
        if (maxRayGizmos < 0) maxRayGizmos = 0;
        rayStarts = maxRayGizmos == 0 ? null : new Vector3[maxRayGizmos];
        rayEnds = maxRayGizmos == 0 ? null : new Vector3[maxRayGizmos];
        rayColors = maxRayGizmos == 0 ? null : new Color[maxRayGizmos];
        rayCount = 0;
    }

    void RebuildContactSetFromList()
    {
        contactsSet.Clear();
        if (radarContacts == null) radarContacts = new List<GameObject>();

        for (int i = radarContacts.Count - 1; i >= 0; i--)
        {
            GameObject go = radarContacts[i];
            if (go == null)
            {
                radarContacts.RemoveAt(i);
                continue;
            }

            if (!contactsSet.Add(go))
            {
                radarContacts.RemoveAt(i);
            }
        }
    }

    void Update()
    {
        if (previousRadarMode != radarMode)
        {
            ApplyRadarModeState(false);
            previousRadarMode = radarMode;
        }

        scanTimer += Time.deltaTime;
        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanRCS();
        }

        displayRefreshTimer += Time.deltaTime;
        if (displayRefreshTimer >= displayRefreshInterval)
        {
            displayRefreshTimer = 0f;
            RefreshScreens();
        }

        settingDisplayTimer += Time.deltaTime;
        if (settingDisplayTimer >= settingDisplayRefreshInterval)
        {
            settingDisplayTimer = 0f;
            RefreshSettingDisplays();
        }

        ValidateSelection();
        UpdateGimbleTracking();
        UpdateAllScreens();
        SyncHudBlips();
        UpdateHudBlips();

        if (lastMode != radarMode || lastRange != radarRange)
        {
            UpdateSettingDisplays(true);
            lastMode = radarMode;
            lastRange = radarRange;
        }
    }

    bool IsHMDMode(RadarMode mode)
    {
        return mode.ToString().StartsWith("HMD_");
    }

    void ApplyRadarModeState(bool force)
    {
        bool isHMD = IsHMDMode(radarMode);
        HMDControllingRadar = isHMD;

        if (force)
        {
            if (!isHMD && gimble != null)
            {
                gimble.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            return;
        }

        if (!isHMD && gimble != null)
        {
            gimble.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
        }

        recenterTimer = 0f;
    }

    void UpdateGimbleTracking()
    {
        if (gimble == null) return;

        if (IsHMDMode(radarMode))
        {
            UpdateHMDGimbleTracking();
            return;
        }

        HMDControllingRadar = false;

        if (selectedContact == null)
        {
            recenterTimer += Time.deltaTime;
            if (recenterTimer >= recenterDelay)
            {
                gimble.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            }
            return;
        }

        recenterTimer = 0f;

        Transform parent = gimble.transform.parent != null ? gimble.transform.parent : transform;
        Vector3 toTargetWorld = selectedContact.transform.position - gimble.transform.position;

        if (toTargetWorld.sqrMagnitude <= 0.000001f) return;

        Vector3 localDir = parent.InverseTransformDirection(toTargetWorld.normalized);
        Quaternion targetLocalRotation = Quaternion.LookRotation(localDir, Vector3.up);

        Vector3 euler = targetLocalRotation.eulerAngles;
        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);

        x = Mathf.Clamp(x, gimbleMinX, gimbleMaxX);
        y = Mathf.Clamp(y, gimbleMinY, gimbleMaxY);

        gimble.transform.localRotation = Quaternion.Euler(x, y, 0f);
    }

    void UpdateHMDGimbleTracking()
    {
        HMDControllingRadar = true;
        recenterTimer = 0f;

        if (playerCamera == null)
        {
            gimble.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            return;
        }

        Transform parent = gimble.transform.parent != null ? gimble.transform.parent : transform;
        Quaternion localCameraRotation = Quaternion.Inverse(parent.rotation) * playerCamera.rotation;
        Vector3 euler = localCameraRotation.eulerAngles;

        float x = NormalizeAngle(euler.x);
        float y = NormalizeAngle(euler.y);

        x = Mathf.Clamp(x, gimbleMinX, gimbleMaxX);
        y = Mathf.Clamp(y, gimbleMinY, gimbleMaxY);

        gimble.transform.localRotation = Quaternion.Euler(x, y, 0f);
    }

    static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public void NextMode()
    {
        int count = System.Enum.GetValues(typeof(RadarMode)).Length;
        if (count <= 0) return;

        int i = (int)radarMode;
        i++;
        if (i >= count) i = 0;
        radarMode = (RadarMode)i;

        UpdateSettingDisplays(true);
        lastMode = radarMode;
    }

    public void BackMode()
    {
        int count = System.Enum.GetValues(typeof(RadarMode)).Length;
        if (count <= 0) return;

        int i = (int)radarMode;
        i--;
        if (i < 0) i = count - 1;
        radarMode = (RadarMode)i;

        UpdateSettingDisplays(true);
        lastMode = radarMode;
    }

    public void NextRange()
    {
        int count = System.Enum.GetValues(typeof(RadarRange)).Length;
        int i = (int)radarRange;
        i++;
        if (i >= count) i = 0;
        radarRange = (RadarRange)i;

        UpdateSettingDisplays(true);
        lastRange = radarRange;
    }

    public void BackRange()
    {
        int count = System.Enum.GetValues(typeof(RadarRange)).Length;
        int i = (int)radarRange;
        i--;
        if (i < 0) i = count - 1;
        radarRange = (RadarRange)i;

        UpdateSettingDisplays(true);
        lastRange = radarRange;
    }

    void RefreshSettingDisplays()
    {
        settingDisplays.Clear();

        if (string.IsNullOrEmpty(radarSettingDisplayTag)) return;

        GameObject[] gos = GameObject.FindGameObjectsWithTag(radarSettingDisplayTag);
        for (int i = 0; i < gos.Length; i++)
        {
            GameObject go = gos[i];
            if (go == null) continue;
            if (!go.activeInHierarchy) continue;

            TMP_Text t = go.GetComponent<TMP_Text>();
            if (t == null) t = go.GetComponentInChildren<TMP_Text>(true);
            if (t == null) continue;

            settingDisplays.Add(t);
        }
    }

    void UpdateSettingDisplays(bool force)
    {
        if (!force && lastMode == radarMode && lastRange == radarRange) return;

        string msg = radarMode.ToString() + " | " + GetRangeString(radarRange);

        for (int i = 0; i < settingDisplays.Count; i++)
        {
            TMP_Text t = settingDisplays[i];
            if (t == null) continue;
            t.text = msg;
        }
    }

    static string GetRangeString(RadarRange range)
    {
        string s = range.ToString();
        if (s.StartsWith("Range_")) s = s.Substring(6);
        return s;
    }

    void OnNextContactPerformed(InputAction.CallbackContext ctx)
    {
        SelectNextContact();
    }

    void OnPreviousContactPerformed(InputAction.CallbackContext ctx)
    {
        SelectPreviousContact();
    }

    public void SelectNextContact()
    {
        if (radarContacts == null || radarContacts.Count == 0)
        {
            selectedContactIndex = -1;
            selectedContact = null;
            return;
        }

        if (selectedContactIndex < 0) selectedContactIndex = 0;
        else selectedContactIndex = (selectedContactIndex + 1) % radarContacts.Count;

        selectedContact = radarContacts[selectedContactIndex];
    }

    public void SelectPreviousContact()
    {
        if (radarContacts == null || radarContacts.Count == 0)
        {
            selectedContactIndex = -1;
            selectedContact = null;
            return;
        }

        if (selectedContactIndex < 0) selectedContactIndex = 0;
        else
        {
            selectedContactIndex--;
            if (selectedContactIndex < 0) selectedContactIndex = radarContacts.Count - 1;
        }

        selectedContact = radarContacts[selectedContactIndex];
    }

    void ValidateSelection()
    {
        if (radarContacts == null || radarContacts.Count == 0)
        {
            selectedContactIndex = -1;
            selectedContact = null;
            return;
        }

        if (selectedContact != null)
        {
            int idx = radarContacts.IndexOf(selectedContact);
            if (idx >= 0)
            {
                selectedContactIndex = idx;
                return;
            }
        }

        if (selectedContactIndex < 0 || selectedContactIndex >= radarContacts.Count)
        {
            selectedContactIndex = 0;
        }

        selectedContact = radarContacts[selectedContactIndex];
    }

    void RefreshScreens()
    {
        activeScreens.Clear();

        GameObject[] screens = GameObject.FindGameObjectsWithTag(radarDisplayTag);
        for (int i = 0; i < screens.Length; i++)
        {
            GameObject go = screens[i];
            if (go == null) continue;
            if (!go.activeInHierarchy) continue;

            RectTransform rt = go.GetComponent<RectTransform>();
            if (rt == null) continue;

            activeScreens.Add(rt);

            if (!screenBlips.ContainsKey(rt))
            {
                screenBlips.Add(rt, new Dictionary<GameObject, BlipInfo>(64));
            }
        }

        List<RectTransform> removeScreens = null;

        foreach (var kvp in screenBlips)
        {
            RectTransform screen = kvp.Key;
            if (screen == null || !screen.gameObject.activeInHierarchy)
            {
                if (removeScreens == null) removeScreens = new List<RectTransform>();
                removeScreens.Add(screen);
            }
        }

        if (removeScreens != null)
        {
            for (int i = 0; i < removeScreens.Count; i++)
            {
                RectTransform s = removeScreens[i];
                if (s != null && screenBlips.TryGetValue(s, out var map))
                {
                    DestroyAllBlipsInMap(map);
                }
                screenBlips.Remove(s);
            }
        }
    }

    void UpdateAllScreens()
    {
        if (radarBlipPrefab == null) return;
        if (activeScreens.Count == 0) return;

        Transform g = transform;
        GetModeAngles(radarMode, out float hDeg, out float vDeg);
        float range = GetRange(radarRange);

        if (hDeg <= 0.0001f) hDeg = 0.0001f;
        if (vDeg <= 0.0001f) vDeg = 0.0001f;
        if (range <= 0.0001f) range = 0.0001f;

        for (int i = 0; i < activeScreens.Count; i++)
        {
            RectTransform screen = activeScreens[i];
            if (screen == null) continue;
            if (!screen.gameObject.activeInHierarchy) continue;

            if (!screenBlips.TryGetValue(screen, out var blipMap) || blipMap == null)
            {
                blipMap = new Dictionary<GameObject, BlipInfo>(64);
                screenBlips[screen] = blipMap;
            }

            SyncScreenBlips(screen, blipMap);
            UpdateScreenBlips(screen, blipMap, g, hDeg, range);
        }
    }

    void SyncScreenBlips(RectTransform screen, Dictionary<GameObject, BlipInfo> blipMap)
    {
        for (int i = 0; i < radarContacts.Count; i++)
        {
            GameObject c = radarContacts[i];
            if (c == null) continue;

            if (!blipMap.ContainsKey(c))
            {
                GameObject go = Instantiate(radarBlipPrefab, screen);
                RectTransform rt = go.GetComponent<RectTransform>();
                if (rt == null) rt = go.AddComponent<RectTransform>();

                TMP_Text t = go.GetComponentInChildren<TMP_Text>(true);

                GameObject marker = null;
                Transform m = FindChildByName(go.transform, selectedMarkerChildName);
                if (m != null) marker = m.gameObject;

                BlipInfo info = new BlipInfo { rt = rt, text = t, selectedMarker = marker };
                blipMap.Add(c, info);
            }
        }

        List<GameObject> remove = null;

        foreach (var kvp in blipMap)
        {
            GameObject contact = kvp.Key;
            if (contact == null)
            {
                if (remove == null) remove = new List<GameObject>();
                remove.Add(contact);
                continue;
            }

            bool stillContact = false;
            for (int i = 0; i < radarContacts.Count; i++)
            {
                if (radarContacts[i] == contact)
                {
                    stillContact = true;
                    break;
                }
            }

            if (!stillContact)
            {
                if (remove == null) remove = new List<GameObject>();
                remove.Add(contact);
            }
        }

        if (remove != null)
        {
            for (int i = 0; i < remove.Count; i++)
            {
                GameObject c = remove[i];
                if (blipMap.TryGetValue(c, out BlipInfo info))
                {
                    if (info.rt != null) Destroy(info.rt.gameObject);
                }
                blipMap.Remove(c);
            }
        }
    }

    void UpdateScreenBlips(RectTransform screen, Dictionary<GameObject, BlipInfo> blipMap, Transform g, float hDeg, float range)
    {
        Vector2 half = screen.rect.size * 0.5f;

        foreach (var kvp in blipMap)
        {
            GameObject contact = kvp.Key;
            BlipInfo info = kvp.Value;

            if (contact == null) continue;
            if (info.rt == null) continue;

            Vector3 toTarget = contact.transform.position - g.position;
            float dist = toTarget.magnitude;

            Vector3 dirWorld = dist > 0.0001f ? (toTarget / dist) : g.forward;
            Vector3 dirLocal = g.InverseTransformDirection(dirWorld);
            float yawDeg = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;

            float xNorm = yawDeg / hDeg;
            float y01 = dist / range;

            if (clampToScreen)
            {
                xNorm = Mathf.Clamp(xNorm, -1f, 1f);
                y01 = Mathf.Clamp01(y01);
            }

            float y = Mathf.Lerp(-half.y, half.y, y01);
            float x = xNorm * half.x;

            info.rt.anchoredPosition = new Vector2(x, y);

            if (info.text != null)
            {
                info.text.text = dist.ToString("0") + distanceSuffix;
            }

            if (info.selectedMarker != null)
            {
                info.selectedMarker.SetActive(contact == selectedContact);
            }
        }
    }

    void DestroyAllBlipsInMap(Dictionary<GameObject, BlipInfo> blipMap)
    {
        foreach (var kvp in blipMap)
        {
            BlipInfo info = kvp.Value;
            if (info.rt != null) Destroy(info.rt.gameObject);
        }
        blipMap.Clear();
    }

    void SyncHudBlips()
    {
        if (hudRadarBlipPrefab == null) return;

        for (int i = 0; i < radarContacts.Count; i++)
        {
            GameObject c = radarContacts[i];
            if (c == null) continue;

            if (!hudBlips.ContainsKey(c))
            {
                GameObject go = Instantiate(hudRadarBlipPrefab);

                GameObject marker = null;
                Transform m = FindChildByName(go.transform, selectedMarkerChildName);
                if (m != null) marker = m.gameObject;

                HudBlipInfo info = new HudBlipInfo { t = go.transform, selectedMarker = marker };
                hudBlips.Add(c, info);
            }
        }

        List<GameObject> remove = null;

        foreach (var kvp in hudBlips)
        {
            GameObject contact = kvp.Key;
            HudBlipInfo info = kvp.Value;

            if (contact == null || info.t == null)
            {
                if (remove == null) remove = new List<GameObject>();
                remove.Add(contact);
                continue;
            }

            bool stillContact = false;
            for (int i = 0; i < radarContacts.Count; i++)
            {
                if (radarContacts[i] == contact)
                {
                    stillContact = true;
                    break;
                }
            }

            if (!stillContact)
            {
                if (remove == null) remove = new List<GameObject>();
                remove.Add(contact);
            }
        }

        if (remove != null)
        {
            for (int i = 0; i < remove.Count; i++)
            {
                GameObject c = remove[i];
                if (c != null && hudBlips.TryGetValue(c, out HudBlipInfo info))
                {
                    if (info.t != null) Destroy(info.t.gameObject);
                }
                hudBlips.Remove(c);
            }
        }
    }

    void UpdateHudBlips()
    {
        if (playerCamera == null) return;
        if (hudBlips.Count == 0) return;

        foreach (var kvp in hudBlips)
        {
            GameObject contact = kvp.Key;
            HudBlipInfo info = kvp.Value;

            if (contact == null) continue;
            if (info.t == null) continue;

            info.t.position = contact.transform.position;

            Vector3 forward = info.t.position - playerCamera.position;
            if (forward.sqrMagnitude > 0.000001f)
            {
                info.t.rotation = Quaternion.LookRotation(forward.normalized, playerCamera.up);
            }

            float distanceToCamera = Vector3.Distance(playerCamera.position, info.t.position);
            float scaleT = hudMaxScaleDistance <= hudMinScaleDistance
                ? 1f
                : Mathf.InverseLerp(hudMinScaleDistance, hudMaxScaleDistance, distanceToCamera);

            info.t.localScale = Vector3.Lerp(hudMinScale, hudMaxScale, scaleT);

            if (info.selectedMarker != null)
            {
                info.selectedMarker.SetActive(contact == selectedContact);
            }
        }
    }

    void ScanRCS()
    {
        Transform g = transform;

        GetModeAngles(radarMode, out float horizontalAngleDeg, out float verticalAngleDeg);
        float maxDistance = GetRange(radarRange);

        rayCount = 0;

        GameObject[] rcsObjects = GameObject.FindGameObjectsWithTag(rcsTag);
        for (int i = 0; i < rcsObjects.Length; i++)
        {
            GameObject rcsObject = rcsObjects[i];
            if (rcsObject == null) continue;

            Transform rcsTransform = rcsObject.transform;

            Vector3 toTarget = rcsTransform.position - g.position;
            float targetDist = toTarget.magnitude;
            if (targetDist <= 0.0001f)
            {
                RemoveContact(rcsObject);
                continue;
            }

            Vector3 dirWorld = toTarget / targetDist;

            Vector3 dirLocal = g.InverseTransformDirection(dirWorld);
            float yawDeg = Mathf.Atan2(dirLocal.x, dirLocal.z) * Mathf.Rad2Deg;
            float pitchDeg = Mathf.Atan2(dirLocal.y, dirLocal.z) * Mathf.Rad2Deg;

            bool insideCone = Mathf.Abs(yawDeg) <= horizontalAngleDeg && Mathf.Abs(pitchDeg) <= verticalAngleDeg;

            bool hitSomething = Physics.Raycast(g.position, dirWorld, out RaycastHit hit, maxDistance, ~0, QueryTriggerInteraction.Ignore);

            bool unobstructedLos = false;
            if (hitSomething)
            {
                if (hit.transform == rcsTransform) unobstructedLos = true;
                else if (hit.transform.IsChildOf(rcsTransform)) unobstructedLos = true;
            }
            else
            {
                unobstructedLos = targetDist <= maxDistance;
            }

            bool isGreen = insideCone && unobstructedLos;

            if (isGreen) AddContact(rcsObject);
            else RemoveContact(rcsObject);

            Color c = isGreen ? Color.green : Color.red;

            float visualDist = Mathf.Min(targetDist, maxDistance);
            Vector3 visualEnd = g.position + dirWorld * visualDist;

            if (rayStarts != null && rayCount < rayStarts.Length)
            {
                rayStarts[rayCount] = g.position;
                rayEnds[rayCount] = visualEnd;
                rayColors[rayCount] = c;
                rayCount++;
            }

            if (debugRays)
            {
                Debug.DrawRay(g.position, dirWorld * visualDist, c, scanInterval);
            }
        }

        CleanupNullContacts();
    }

    void AddContact(GameObject rcsObject)
    {
        if (rcsObject == null) return;
        if (contactsSet.Add(rcsObject))
        {
            radarContacts.Add(rcsObject);

            if (!newContactEvent.IsNull)
            {
                RuntimeManager.PlayOneShot(newContactEvent, transform.position);
            }
        }
    }

    void RemoveContact(GameObject rcsObject)
    {
        if (rcsObject == null) return;
        if (contactsSet.Remove(rcsObject))
        {
            for (int i = radarContacts.Count - 1; i >= 0; i--)
            {
                if (radarContacts[i] == rcsObject)
                {
                    radarContacts.RemoveAt(i);
                    break;
                }
            }
        }
    }

    void CleanupNullContacts()
    {
        for (int i = radarContacts.Count - 1; i >= 0; i--)
        {
            if (radarContacts[i] == null)
            {
                radarContacts.RemoveAt(i);
            }
        }

        contactsSet.Clear();
        for (int i = 0; i < radarContacts.Count; i++)
        {
            GameObject go = radarContacts[i];
            if (go != null) contactsSet.Add(go);
        }
    }

    void OnDrawGizmos()
    {
        DrawDetectionCone();
        DrawTrackRays();
    }

    void DrawTrackRays()
    {
        if (!rayGizmos) return;
        if (rayStarts == null || rayEnds == null || rayColors == null) return;
        if (rayCount <= 0) return;

        for (int i = 0; i < rayCount; i++)
        {
            Gizmos.color = rayColors[i];
            Gizmos.DrawLine(rayStarts[i], rayEnds[i]);
            if (trackRayHitRadius > 0f) Gizmos.DrawSphere(rayEnds[i], trackRayHitRadius);
        }
    }

    void DrawDetectionCone()
    {
        Transform g = transform;

        GetModeAngles(radarMode, out float horizontalAngleDeg, out float verticalAngleDeg);
        float range = GetRange(radarRange);

        Vector3 o = g.position;
        Vector3 f = g.TransformDirection(Vector3.forward);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(o, o + f * range);

        int seg = gizmoSegments <= 2 ? 3 : gizmoSegments;

        Vector3 prev = Vector3.zero;
        bool hasPrev = false;

        for (int i = 0; i <= seg; i++)
        {
            float t = i / (float)seg;
            float a = t * Mathf.PI * 2f;

            float yaw = Mathf.Cos(a) * horizontalAngleDeg;
            float pitch = Mathf.Sin(a) * verticalAngleDeg;

            Vector3 localDir = Quaternion.Euler(pitch, yaw, 0f) * Vector3.forward;
            Vector3 worldDir = g.TransformDirection(localDir).normalized;

            Vector3 p = o + worldDir * range;

            if (hasPrev) Gizmos.DrawLine(prev, p);
            else hasPrev = true;

            prev = p;
        }

        float[] legYaw = new float[] { horizontalAngleDeg, -horizontalAngleDeg, 0f, 0f };
        float[] legPitch = new float[] { 0f, 0f, verticalAngleDeg, -verticalAngleDeg };

        for (int i = 0; i < 4; i++)
        {
            Vector3 localDir = Quaternion.Euler(legPitch[i], legYaw[i], 0f) * Vector3.forward;
            Vector3 worldDir = g.TransformDirection(localDir).normalized;
            Gizmos.DrawLine(o, o + worldDir * range);
        }
    }

    static Transform FindChildByName(Transform root, string childName)
    {
        if (root == null) return null;
        if (string.IsNullOrEmpty(childName)) return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform c = root.GetChild(i);
            if (c.name == childName) return c;

            Transform deep = FindChildByName(c, childName);
            if (deep != null) return deep;
        }

        return null;
    }

    static void GetModeAngles(RadarMode mode, out float horizontalAngleDeg, out float verticalAngleDeg)
    {
        string s = mode.ToString();
        int underscore = s.IndexOf('_');
        horizontalAngleDeg = 0f;
        verticalAngleDeg = 0f;

        if (underscore < 0 || underscore + 1 >= s.Length) return;

        string dims = s.Substring(underscore + 1);
        string[] parts = dims.Split('X');

        if (parts.Length != 2) return;

        float.TryParse(parts[0], out horizontalAngleDeg);
        float.TryParse(parts[1], out verticalAngleDeg);
    }

    static float GetRange(RadarRange range)
    {
        string s = range.ToString();
        if (s.StartsWith("Range_")) s = s.Substring(6);

        if (float.TryParse(s, out float v)) return v;
        return 0f;
    }
}