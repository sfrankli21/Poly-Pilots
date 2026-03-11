using UnityEngine;
using UnityEngine.Events;

public class ZUNIRocketLogic : MonoBehaviour
{
    public AlignToVelocity alignToVelocity;
    public float moveSpeed = 100f;
    public float gravityMultiplier = 0.25f;
    public UnityEvent Impact;
    public GameObject impactPrefab;

    public bool HasFired { get; private set; }

    Rigidbody rb;

    public void Fire()
    {
        if (HasFired)
        {
            return;
        }

        HasFired = true;

        transform.SetParent(null, true);

        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
        }

        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (alignToVelocity != null)
        {
            alignToVelocity.Activate();
        }
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

        rb.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);

        Vector3 gravityVelocity = Vector3.Project(rb.linearVelocity, Vector3.up);
        Vector3 forwardVelocity = transform.forward * moveSpeed;
        rb.linearVelocity = forwardVelocity + gravityVelocity;
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