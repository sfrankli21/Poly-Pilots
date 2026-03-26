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
    readonly Dictionary<DetectionRadar, RWRContactUI> contactMap = new Dictionary<DetectionRadar, RWRContactUI>();
    readonly List<DetectionRadar> radarsBuffer = new List<DetectionRadar>();

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

            if (existing == null || existing.radar == null || !radarsBuffer.Contains(existing.radar))
            {
                RemoveContact(existing != null ? existing.radar : null);
                continue;
            }

            if (RadarHasLockOnThisJet(existing.radar))
            {
                existing.lastDetectedTime = Time.time;
            }
            else if (Time.time - existing.lastDetectedTime > blipPersistTime)
            {
                RemoveContact(existing.radar);
            }
        }

        for (int i = 0; i < radarsBuffer.Count; i++)
        {
            DetectionRadar radar = radarsBuffer[i];

            if (RadarHasLockOnThisJet(radar))
            {
                if (!contactMap.ContainsKey(radar))
                {
                    CreateContact(radar);
                }
                else
                {
                    contactMap[radar].lastDetectedTime = Time.time;
                }
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

    void CreateContact(DetectionRadar radar)
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
        contactMap.Add(radar, entry);
    }

    void RemoveContact(DetectionRadar radar)
    {
        if (radar == null)
        {
            return;
        }

        if (!contactMap.TryGetValue(radar, out RWRContactUI entry))
        {
            return;
        }

        if (entry != null && entry.icon != null)
        {
            Destroy(entry.icon.gameObject);
        }

        activeContacts.Remove(entry);
        contactMap.Remove(radar);
    }

    void UpdateContactPositions()
    {
        Vector2 center = centerReference != null ? centerReference.anchoredPosition : Vector2.zero;

        for (int i = activeContacts.Count - 1; i >= 0; i--)
        {
            RWRContactUI contact = activeContacts[i];

            if (contact == null || contact.radar == null || contact.icon == null)
            {
                if (contact != null && contact.radar != null)
                {
                    contactMap.Remove(contact.radar);
                }

                activeContacts.RemoveAt(i);
                continue;
            }

            Vector3 toEmitter = contact.radar.transform.position - transform.position;
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
                float maxDistance = useRadarRangeForDistanceNormalization ? contact.radar.CurrentRangeValue : manualMaxDistance;
                float actualDistance = Vector3.Distance(transform.position, contact.radar.transform.position);
                float t = Mathf.Clamp01(actualDistance / Mathf.Max(1f, maxDistance));
                radialDistance = Mathf.Lerp(displayRadius * 0.35f, displayRadius, t);
            }

            Vector2 uiPosition = center + topDownDirection * radialDistance;
            contact.icon.anchoredPosition = uiPosition;

            float angle = Mathf.Atan2(topDownDirection.y, topDownDirection.x) * Mathf.Rad2Deg;
            contact.icon.localRotation = Quaternion.Euler(0f, 0f, angle - 90f);

            if (scaleByDistance)
            {
                float maxDistance = useRadarRangeForDistanceNormalization ? contact.radar.CurrentRangeValue : manualMaxDistance;
                float actualDistance = Vector3.Distance(transform.position, contact.radar.transform.position);
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
                    contact.label.text = contact.radar.RWRID;
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
    }
}