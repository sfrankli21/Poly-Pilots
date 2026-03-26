using UnityEngine;

public class ObjectRotation : MonoBehaviour
{
    public bool enableX;
    public bool enableY;
    public bool enableZ;
    public float rate;
    public bool Rotating;

    void Update()
    {
        if (!Rotating)
        {
            return;
        }

        Vector3 rotation = Vector3.zero;

        if (enableX)
        {
            rotation.x = rate;
        }

        if (enableY)
        {
            rotation.y = rate;
        }

        if (enableZ)
        {
            rotation.z = rate;
        }

        transform.Rotate(rotation * Time.deltaTime);
    }
}