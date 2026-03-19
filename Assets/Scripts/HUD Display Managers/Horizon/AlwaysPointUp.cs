using UnityEngine;

public class AlwaysPointUp : MonoBehaviour
{
    void Update()
    {
        Vector3 up = transform.parent != null ? transform.parent.InverseTransformDirection(Vector3.up) : Vector3.up;
        float angle = Mathf.Atan2(up.x, up.y) * Mathf.Rad2Deg;
        Vector3 euler = transform.localEulerAngles;
        euler.z = -angle;
        transform.localEulerAngles = euler;
    }
}