using UnityEngine;

public class MoveObjectForward : MonoBehaviour
{
    [Header("Movement Settings")]
    public float speed = 5f; // Units per second

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }
}
