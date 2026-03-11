using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

public class ZuniPodRelease : MonoBehaviour
{
    public UnityEvent FireRocket;
    public EventReference fireRocketEvent;

    ZUNIRocketLogic[] rockets;

    void Awake()
    {
        rockets = GetComponentsInChildren<ZUNIRocketLogic>(true);
    }

    public void Release()
    {
        ZUNIRocketLogic rocket = GetNextAvailableRocket();
        if (rocket == null)
        {
            return;
        }

        rocket.Fire();

        if (!fireRocketEvent.IsNull)
        {
            RuntimeManager.PlayOneShot(fireRocketEvent, transform.position);
        }

        Debug.Log("ZUNI rocket fired from " + gameObject.name + " | Remaining: " + GetRemainingRocketCount());
        FireRocket.Invoke();
    }

    ZUNIRocketLogic GetNextAvailableRocket()
    {
        if (rockets == null || rockets.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i] != null && !rockets[i].HasFired)
            {
                return rockets[i];
            }
        }

        return null;
    }

    public bool HasAmmo()
    {
        return GetRemainingRocketCount() > 0;
    }

    public int GetRemainingRocketCount()
    {
        if (rockets == null || rockets.Length == 0)
        {
            return 0;
        }

        int count = 0;

        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i] != null && !rockets[i].HasFired)
            {
                count++;
            }
        }

        return count;
    }
}