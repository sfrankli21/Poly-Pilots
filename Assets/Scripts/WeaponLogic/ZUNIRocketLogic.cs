using UnityEngine;
using UnityEngine.Events;

public class ZUNIRocketLogic : MonoBehaviour
{
    public float pushForce = 200f;
    public float gravityMultiplier = 0.01f;
    public UnityEvent Impact;
    public UnityEvent FireEvent;
    public GameObject impactPrefab;

    public bool HasFired { get; private set; }

    Rigidbody rb;
    float releaseForwardSpeed;

    public void Fire()
    {
        if (HasFired)
        {
            return;
        }

        Rigidbody aircraftRb = null;
        Transform current = transform.parent;
        FireEvent.Invoke();

        while (current != null)
        {
            aircraftRb = current.GetComponent<Rigidbody>();
            if (aircraftRb != null)
            {
                break;
            }

            current = current.parent;
        }

        releaseForwardSpeed = 0f;

        if (aircraftRb != null)
        {
            releaseForwardSpeed = Vector3.Dot(aircraftRb.linearVelocity, transform.forward);
        }

        HasFired = true;

        transform.SetParent(null, true);

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = true;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        rb.linearVelocity = transform.forward * releaseForwardSpeed;
    }

    void FixedUpdate()
    {
        if (!HasFired)
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

        rb.AddForce(transform.forward * pushForce, ForceMode.Acceleration);
        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!HasFired)
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