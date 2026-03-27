using UnityEngine;

public class RadarSweep : MonoBehaviour
{
    public float minYRotation = -45f;
    public float maxYRotation = 45f;
    public float sweepSpeed = 45f;

    float currentY;
    float direction = 1f;
    Vector3 startLocalEulerAngles;

    void Start()
    {
        startLocalEulerAngles = transform.localEulerAngles;
        currentY = Mathf.DeltaAngle(0f, startLocalEulerAngles.y);
    }

    void Update()
    {
        currentY += direction * sweepSpeed * Time.deltaTime;

        if (currentY >= maxYRotation)
        {
            currentY = maxYRotation;
            direction = -1f;
        }
        else if (currentY <= minYRotation)
        {
            currentY = minYRotation;
            direction = 1f;
        }

        transform.localRotation = Quaternion.Euler(startLocalEulerAngles.x, currentY, startLocalEulerAngles.z);
    }
}