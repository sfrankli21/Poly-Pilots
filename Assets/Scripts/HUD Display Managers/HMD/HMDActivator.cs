using UnityEngine;
using UnityEngine.Events;

public class HMDActivator : MonoBehaviour
{
    public Transform RayOrigin;
    public Collider HUDCollider;
    public bool HMDOn;
    public UnityEvent HMDOnEvent;
    public UnityEvent HMDOffEvent;

    void Update()
    {
        if (RayOrigin == null || HUDCollider == null)
        {
            return;
        }

        Ray ray = new Ray(RayOrigin.position, RayOrigin.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide) && hit.collider == HUDCollider)
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