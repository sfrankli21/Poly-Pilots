using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class HMDActivator : MonoBehaviour
{
    public Transform RayOrigin;
    public List<Collider> HUDColliders = new List<Collider>();
    public bool HMDOn;
    public UnityEvent HMDOnEvent;
    public UnityEvent HMDOffEvent;

    void Update()
    {
        if (RayOrigin == null || HUDColliders == null || HUDColliders.Count == 0)
        {
            return;
        }

        Ray ray = new Ray(RayOrigin.position, RayOrigin.forward);
        bool hitHUDCollider = false;

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
        {
            for (int i = 0; i < HUDColliders.Count; i++)
            {
                if (HUDColliders[i] == null)
                {
                    continue;
                }

                if (hit.collider == HUDColliders[i])
                {
                    hitHUDCollider = true;
                    break;
                }
            }
        }

        if (hitHUDCollider)
        {
            HMDOn = false;
            HMDOffEvent.Invoke();
        }
        else
        {
            HMDOn = true;
            HMDOnEvent.Invoke();
        }
    }
}