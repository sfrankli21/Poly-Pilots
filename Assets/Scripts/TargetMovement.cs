using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float yRotationSpeed = 45f;

    void Update()
    {
        transform.Rotate(0f, yRotationSpeed * Time.deltaTime, 0f, Space.Self);
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}