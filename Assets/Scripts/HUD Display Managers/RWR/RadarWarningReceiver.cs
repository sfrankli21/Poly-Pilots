using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RadarWarningReceiver : MonoBehaviour
{
    [System.Serializable]
    public class RWRContactUI
    {
        public DetectionRadar radar;
        public AIAIM9Guidance missile;
        public RectTransform icon;
        public Image iconImage;
        public TMP_Text label;
        public float lastDetectedTime;
    }

    public RectTransform rwrDisplay;
    public RectTransform centerReference;
    public GameObject contactPrefab;
    public float displayRadius = 120f;
    public float updateInterval = 0.1f;
    public float blipPersistTime = 3f;
    public bool scaleByDistance = false;
    public float minScale = 0.7f;
    public float maxScale = 1.2f;
    public bool useRadarRangeForDistanceNormalization = true;
    public float manualMaxDistance = 10000f;
    public bool showLabels = true;
    public bool drawDebugLines;

    public List<RWRContactUI> activeContacts = new List<RWRContactUI>();

    float updateTimer;
    readonly Dictionary<DetectionRadar, RWRContactUI> radarContactMap = new Dictionary<DetectionRadar, RWRContactUI>();
    readonly Dictionary<AIAIM9Guidance, RWRContactUI> missileContactMap = new Dictionary<AIAIM9Guidance, RWRContactUI>();
    readonly List<DetectionRadar> radarsBuffer = new List<DetectionRadar>();
    readonly List<AIAIM9Guidance> missilesBuffer = new List<AIAIM9Guidance>();

    void Update()
    {
        updateTimer += Time.deltaTime;

        if (updateTimer >= updateInterval)
        {
            updateTimer = 0f;
            RefreshContacts();
        }

        UpdateContactPositions();
    }

    void RefreshContacts()
    {
        RefreshRadarContacts();
        RefreshMissileContacts();
    }

    void RefreshRadarContacts()
    {
        radarsBuffer.Clear();
        DetectionRadar[] allRadars = FindObjectsByType<DetectionRadar>(FindObjectsSortMode.None);

        for (int i = 0; i < allRadars.Length; i++)
        {
            if (allRadars[i] != null && allRadars[i].transform != transform)
            {
                radarsBuffer.Add(allRadars[i]);
            }
        }

        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            RWRContactUI existing = activeContacts[i];

            if (existing == null || existing.radar == null)
            {
                continue;
            }

            if (!radarsBuffer.Contains(existing.radar))
            {
                RemoveRadarContact(existing.radar);
                continue;
            }

            if (RadarHasLockOnThisJet(existing.radar))
            {
                existing.lastDetectedTime = Time.time;
            }
            else if (Time.time - existing.lastDetectedTime > blipPersistTime)
            {
                RemoveRadarContact(existing.radar);
            }
        }

        for (int i = 0; i < radarsBuffer.Count; i++)
        {
            DetectionRadar radar = radarsBuffer[i];

            if (RadarHasLockOnThisJet(radar))
            {
                if (!radarContactMap.ContainsKey(radar))
                {
                    CreateRadarContact(radar);
                }
                else
                {
                    radarContactMap[radar].lastDetectedTime = Time.time;
                }
            }
        }
    }

    void RefreshMissileContacts()
    {
        missilesBuffer.Clear();
        AIAIM9Guidance[] allMissiles = FindObjectsByType<AIAIM9Guidance>(FindObjectsSortMode.None);

        for (int i = 0; i < allMissiles.Length; i++)
        {
            AIAIM9Guidance missile = allMissiles[i];

            if (missile == null)
            {
                continue;
            }

            if (!missile.released)
            {
                continue;
            }

            missilesBuffer.Add(missile);
        }

        List<AIAIM9Guidance> missilesToRemove = new List<AIAIM9Guidance>();

        foreach (KeyValuePair<AIAIM9Guidance, RWRContactUI> pair in missileContactMap)
        {
            if (pair.Key == null || !missilesBuffer.Contains(pair.Key))
            {
                missilesToRemove.Add(pair.Key);
            }
        }

        for (int i = 0; i < missilesToRemove.Count; i++)
        {
            RemoveMissileContact(missilesToRemove[i]);
        }

        for (int i = 0; i < missilesBuffer.Count; i++)
        {
            AIAIM9Guidance missile = missilesBuffer[i];

            if (!missileContactMap.ContainsKey(missile))
            {
                CreateMissileContact(missile);
            }
        }
    }

    bool RadarHasLockOnThisJet(DetectionRadar radar)
    {
        if (radar == null)
        {
            return false;
        }

        for (int i = 0; i < radar.radarContacts.Count; i++)
        {
            if (radar.radarContacts[i] == transform)
            {
                return true;
            }
        }

        return false;
    }

    void CreateRadarContact(DetectionRadar radar)
    {
        if (radar == null || contactPrefab == null || rwrDisplay == null)
        {
            return;
        }

        GameObject iconObject = Instantiate(contactPrefab, rwrDisplay);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        Image iconImage = iconObject.GetComponent<Image>();
        TMP_Text label = iconObject.GetComponentInChildren<TMP_Text>(true);

        RWRContactUI entry = new RWRContactUI();
        entry.radar = radar;
        entry.missile = null;
        entry.icon = iconRect;
        entry.iconImage = iconImage;
        entry.label = label;
        entry.lastDetectedTime = Time.time;

        if (entry.label != null)
        {
            entry.label.gameObject.SetActive(showLabels);
            entry.label.text = radar.RWRID;
        }

        activeContacts.Add(entry);
        radarContactMap.Add(radar, entry);
    }

    void CreateMissileContact(AIAIM9Guidance missile)
    {
        if (missile == null || contactPrefab == null || rwrDisplay == null)
        {
            return;
        }

        GameObject iconObject = Instantiate(contactPrefab, rwrDisplay);
        RectTransform iconRect = iconObject.GetComponent<RectTransform>();
        Image iconImage = iconObject.GetComponent<Image>();
        TMP_Text label = iconObject.GetComponentInChildren<TMP_Text>(true);

        RWRContactUI entry = new RWRContactUI();
        entry.radar = null;
        entry.missile = missile;
        entry.icon = iconRect;
        entry.iconImage = iconImage;
        entry.label = label;
        entry.lastDetectedTime = Time.time;

        if (entry.label != null)
        {
            entry.label.gameObject.SetActive(showLabels);
            entry.label.text = missile.RWRID;
        }

        activeContacts.Add(entry);
        missileContactMap.Add(missile, entry);
    }

    void RemoveRadarContact(DetectionRadar radar)
    {
        if (radar == null)
        {
            return;
        }

        if (!radarContactMap.TryGetValue(radar, out RWRContactUI entry))
        {
            return;
        }

        if (entry != null && entry.icon != null)
        {
            Destroy(entry.icon.gameObject);
        }

        activeContacts.Remove(entry);
        radarContactMap.Remove(radar);
    }

    void RemoveMissileContact(AIAIM9Guidance missile)
    {
        if (missile == null)
        {
            return;
        }

        if (!missileContactMap.TryGetValue(missile, out RWRContactUI entry))
        {
            return;
        }

        if (entry != null && entry.icon != null)
        {
            Destroy(entry.icon.gameObject);
        }

        activeContacts.Remove(entry);
        missileContactMap.Remove(missile);
    }

    void UpdateContactPositions()
    {
        Vector2 center = centerReference != null ? centerReference.anchoredPosition : Vector2.zero;

        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            RWRContactUI contact = activeContacts[i];

            if (contact == null || contact.icon == null)
            {
                activeContacts.RemoveAt(i);
                continue;
            }

            Transform sourceTransform = null;

            if (contact.radar != null)
            {
                sourceTransform = contact.radar.transform;
            }
            else if (contact.missile != null)
            {
                sourceTransform = contact.missile.transform;
            }

            if (sourceTransform == null)
            {
                if (contact.radar != null)
                {
                    radarContactMap.Remove(contact.radar);
                }

                if (contact.missile != null)
                {
                    missileContactMap.Remove(contact.missile);
                }

                if (contact.icon != null)
                {
                    Destroy(contact.icon.gameObject);
                }

                activeContacts.RemoveAt(i);
                continue;
            }

            Vector3 toEmitter = sourceTransform.position - transform.position;
            Vector3 local = transform.InverseTransformDirection(toEmitter.normalized);

            Vector2 topDownDirection = new Vector2(local.x, local.z);

            if (topDownDirection.sqrMagnitude < 0.0001f)
            {
                topDownDirection = Vector2.up;
            }
            else
            {
                topDownDirection.Normalize();
            }

            float radialDistance = displayRadius;

            if (scaleByDistance)
            {
                float maxDistance = manualMaxDistance;

                if (contact.radar != null && useRadarRangeForDistanceNormalization)
                {
                    maxDistance = contact.radar.CurrentRangeValue;
                }

                float actualDistance = Vector3.Distance(transform.position, sourceTransform.position);
                float t = Mathf.Clamp01(actualDistance / Mathf.Max(1f, maxDistance));
                radialDistance = Mathf.Lerp(displayRadius * 0.35f, displayRadius, t);
            }

            Vector2 uiPosition = center + topDownDirection * radialDistance;
            contact.icon.anchoredPosition = uiPosition;

            float angle = Mathf.Atan2(topDownDirection.y, topDownDirection.x) * Mathf.Rad2Deg;
            contact.icon.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (scaleByDistance)
            {
                float maxDistance = manualMaxDistance;

                if (contact.radar != null && useRadarRangeForDistanceNormalization)
                {
                    maxDistance = contact.radar.CurrentRangeValue;
                }

                float actualDistance = Vector3.Distance(transform.position, sourceTransform.position);
                float t = 1f - Mathf.Clamp01(actualDistance / Mathf.Max(1f, maxDistance));
                float scale = Mathf.Lerp(minScale, maxScale, t);
                contact.icon.localScale = Vector3.one * scale;
            }
            else
            {
                contact.icon.localScale = Vector3.one;
            }

            if (contact.label != null)
            {
                contact.label.gameObject.SetActive(showLabels);

                if (showLabels)
                {
                    if (contact.radar != null)
                    {
                        contact.label.text = contact.radar.RWRID;
                    }
                    else if (contact.missile != null)
                    {
                        contact.label.text = contact.missile.RWRID;
                    }
                }
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawDebugLines)
        {
            return;
        }

        Gizmos.color = Color.red;

        DetectionRadar[] allRadars = FindObjectsByType<DetectionRadar>(FindObjectsSortMode.None);

        for (int i = 0; i < allRadars.Length; i++)
        {
            DetectionRadar radar = allRadars[i];

            if (radar == null)
            {
                continue;
            }

            if (RadarHasLockOnThisJet(radar))
            {
                Gizmos.DrawLine(transform.position, radar.transform.position);
            }
        }

        AIAIM9Guidance[] allMissiles = FindObjectsByType<AIAIM9Guidance>(FindObjectsSortMode.None);

        for (int i = 0; i < allMissiles.Length; i++)
        {
            AIAIM9Guidance missile = allMissiles[i];

            if (missile == null)
            {
                continue;
            }

            if (missile.released)
            {
                Gizmos.DrawLine(transform.position, missile.transform.position);
            }
        }
    }
}