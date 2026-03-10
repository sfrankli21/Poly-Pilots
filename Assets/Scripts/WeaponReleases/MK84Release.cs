using UnityEngine;
using UnityEngine.Events;

public class MK84Release : MonoBehaviour
{
    public UnityEvent Arming;

    public void Release()
    {
        Rigidbody aircraftRb = null;
        Transform current = transform.parent;

        while (current != null)
        {
            aircraftRb = current.GetComponent<Rigidbody>();
            if (aircraftRb != null)
            {
                break;
            }

            current = current.parent;
        }

        float forwardReleaseSpeed = 0f;

        if (aircraftRb != null)
        {
            forwardReleaseSpeed = Vector3.Dot(aircraftRb.linearVelocity, transform.forward);
        }

        transform.SetParent(null, true);

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.AddForce(transform.forward * forwardReleaseSpeed, ForceMode.VelocityChange);

        Arming.Invoke();

        AlignToVelocity alignToVelocity = GetComponent<AlignToVelocity>();
        if (alignToVelocity != null)
        {
            alignToVelocity.Activate();
        }
    }
}