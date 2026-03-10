using UnityEngine;

public class AlignToVelocity : MonoBehaviour
{
    public float rotationSpeed = 10f;
    public float minimumSpeed = 0.1f;

    Rigidbody targetRigidbody;

    void Awake()
    {
        enabled = false;
    }

    public void Activate()
    {
        targetRigidbody = GetComponent<Rigidbody>();
        enabled = targetRigidbody != null;
    }

    void FixedUpdate()
    {
        if (targetRigidbody == null)
        {
            return;
        }

        Vector3 velocity = targetRigidbody.linearVelocity;

        if (velocity.sqrMagnitude < minimumSpeed * minimumSpeed)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(velocity.normalized, transform.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);
    }
}