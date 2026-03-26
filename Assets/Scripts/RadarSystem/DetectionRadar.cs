using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

public class DetectionRadar : MonoBehaviour
{
    public enum RadarMode
    {
        TWS_30X30,
        TWS_20x85
    }

    public enum RadarRange
    {
        Range_5000,
        Range_7500,
        Range_10000
    }

    public RadarMode currentMode = RadarMode.TWS_30X30;
    public RadarRange currentRange = RadarRange.Range_5000;

    public string rcsTag = "RCS";
    public float scanInterval = 0.25f;

    public EventReference FirstPing;
    public EventReference NotFirstPing;

    public int SpikeCount;
    public UnityEvent SpikeReset;

    public List<Transform> radarContacts = new List<Transform>();
    public List<Transform> Missiles = new List<Transform>();

    HashSet<Transform> contactsSet = new HashSet<Transform>();

    public bool drawGizmos = true;
    public float gizmoDrawDistance = 5000f;
    public Color gizmoColor = Color.green;

    float scanTimer;
    bool hasDetectedBefore;

    public float CurrentRangeValue
    {
        get
        {
            switch (currentRange)
            {
                case RadarRange.Range_5000:
                    return 5000f;
                case RadarRange.Range_7500:
                    return 7500f;
                case RadarRange.Range_10000:
                    return 10000f;
            }

            return 5000f;
        }
    }

    public Vector2 CurrentAngleLimits
    {
        get
        {
            switch (currentMode)
            {
                case RadarMode.TWS_30X30:
                    return new Vector2(30f, 30f);
                case RadarMode.TWS_20x85:
                    return new Vector2(20f, 85f);
            }

            return new Vector2(30f, 30f);
        }
    }

    void Update()
    {
        ClearNullContacts();
        ClearNullMissiles();

        scanTimer += Time.deltaTime;

        if (scanTimer >= scanInterval)
        {
            scanTimer = 0f;
            ScanForContacts();
        }
    }

    void ScanForContacts()
    {
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(rcsTag);
        HashSet<Transform> detectedThisScan = new HashSet<Transform>();

        float range = CurrentRangeValue;
        Vector2 angles = CurrentAngleLimits;
        float yawHalf = angles.x * 0.5f;
        float pitchHalf = angles.y * 0.5f;

        for (int i = 0; i < taggedObjects.Length; i++)
        {
            GameObject obj = taggedObjects[i];

            if (obj == null)
            {
                continue;
            }

            Transform target = obj.transform;
            Vector3 toTarget = target.position - transform.position;
            float distance = toTarget.magnitude;

            if (distance > range)
            {
                continue;
            }

            Vector3 localDirection = transform.InverseTransformDirection(toTarget.normalized);
            float yaw = Mathf.Atan2(localDirection.x, localDirection.z) * Mathf.Rad2Deg;
            float pitch = Mathf.Atan2(localDirection.y, localDirection.z) * Mathf.Rad2Deg;

            if (Mathf.Abs(yaw) > yawHalf)
            {
                continue;
            }

            if (Mathf.Abs(pitch) > pitchHalf)
            {
                continue;
            }

            detectedThisScan.Add(target);

            if (!contactsSet.Contains(target))
            {
                AddContact(target);
            }
        }

        for (int i = radarContacts.Count - 1; i >= 0; i--)
        {
            Transform contact = radarContacts[i];

            if (contact == null || !detectedThisScan.Contains(contact))
            {
                RemoveContact(contact);
            }
        }
    }

    public void AddContact(Transform contact)
    {
        if (contact == null)
        {
            return;
        }

        if (contactsSet.Add(contact))
        {
            radarContacts.Add(contact);
            SpikeCount += CalculateSpikePoints(contact);
            CheckSpikeReset();

            if (!hasDetectedBefore)
            {
                PlayPing(FirstPing);
                hasDetectedBefore = true;
            }
            else
            {
                PlayPing(NotFirstPing);
            }
        }
    }

    public void RemoveContact(Transform contact)
    {
        if (contact == null)
        {
            return;
        }

        if (contactsSet.Remove(contact))
        {
            radarContacts.Remove(contact);
        }
    }

    int CalculateSpikePoints(Transform contact)
    {
        float maxRange = CurrentRangeValue;
        float distance = Vector3.Distance(transform.position, contact.position);
        float clampedDistance = Mathf.Clamp(distance, 0f, maxRange);
        float bandSize = maxRange / 10f;
        int points = Mathf.FloorToInt((maxRange - clampedDistance) / bandSize);
        return Mathf.Clamp(points, 0, 10);
    }

    void CheckSpikeReset()
    {
        if (SpikeCount >= 75)
        {
            SpikeCount = 0;
            ReleaseMissileFromList();
            SpikeReset.Invoke();
        }
    }

    void ReleaseMissileFromList()
    {
        for (int i = 0; i < Missiles.Count; i++)
        {
            Transform missile = Missiles[i];

            if (missile == null)
            {
                continue;
            }

            AIAIM9Guidance aim9 = missile.GetComponent<AIAIM9Guidance>();

            if (aim9 != null)
            {
                aim9.Release();
                Missiles.RemoveAt(i);
                return;
            }
        }
    }

    void PlayPing(EventReference eventReference)
    {
        if (!eventReference.IsNull)
        {
            RuntimeManager.PlayOneShot(eventReference, transform.position);
        }
    }

    public void ClearNullContacts()
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
            if (radarContacts[i] != null)
            {
                contactsSet.Add(radarContacts[i]);
            }
        }
    }

    public void ClearNullMissiles()
    {
        for (int i = Missiles.Count - 1; i >= 0; i--)
        {
            if (Missiles[i] == null)
            {
                Missiles.RemoveAt(i);
            }
        }
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos)
        {
            return;
        }

        Gizmos.color = gizmoColor;

        Vector2 angles = CurrentAngleLimits;
        float yawHalf = angles.x * 0.5f;
        float pitchHalf = angles.y * 0.5f;

        Vector3 forward = transform.forward * gizmoDrawDistance;
        Vector3 leftEdge = Quaternion.Euler(0f, -yawHalf, 0f) * transform.forward * gizmoDrawDistance;
        Vector3 rightEdge = Quaternion.Euler(0f, yawHalf, 0f) * transform.forward * gizmoDrawDistance;
        Vector3 upEdge = Quaternion.Euler(-pitchHalf, 0f, 0f) * transform.forward * gizmoDrawDistance;
        Vector3 downEdge = Quaternion.Euler(pitchHalf, 0f, 0f) * transform.forward * gizmoDrawDistance;

        Gizmos.DrawRay(transform.position, forward);
        Gizmos.DrawRay(transform.position, leftEdge);
        Gizmos.DrawRay(transform.position, rightEdge);
        Gizmos.DrawRay(transform.position, upEdge);
        Gizmos.DrawRay(transform.position, downEdge);

        Gizmos.DrawLine(transform.position + leftEdge, transform.position + upEdge);
        Gizmos.DrawLine(transform.position + upEdge, transform.position + rightEdge);
        Gizmos.DrawLine(transform.position + rightEdge, transform.position + downEdge);
        Gizmos.DrawLine(transform.position + downEdge, transform.position + leftEdge);
    }
}