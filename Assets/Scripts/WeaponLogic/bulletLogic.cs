using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class bulletLogic : MonoBehaviour
{
    public float speed;
    public float lifeTime;

    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = true;
        rb.linearVelocity = transform.forward * speed;
        Destroy(gameObject, lifeTime);
    }
}