using UnityEngine;

public class TargetMovement : MonoBehaviour
{
    public float moveSpeed = 10f;

    void Update()
    {
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}