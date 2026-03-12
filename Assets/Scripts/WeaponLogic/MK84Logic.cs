using UnityEngine;
using UnityEngine.Events;

public class MK84Logic : MonoBehaviour
{
    public UnityEvent Impact;
    public GameObject impactPrefab;

    Rigidbody rb;
    bool impactArmed;

    public void Fire()
    {
        rb = GetComponent<Rigidbody>();
        impactArmed = rb != null;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!impactArmed)
        {
            return;
        }

        if (rb == null)
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                return;
            }
        }

        if (collision.collider == null)
        {
            return;
        }

        if (collision.collider.isTrigger)
        {
            return;
        }

        if (collision.collider.gameObject.layer != LayerMask.NameToLayer("Default"))
        {
            return;
        }

        Impact.Invoke();

        if (impactPrefab != null)
        {
            Instantiate(impactPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }
}