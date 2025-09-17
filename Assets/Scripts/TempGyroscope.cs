using UnityEngine;

public class TempGyroscope : MonoBehaviour
{
    public bool runInFixedUpdate;

    void LateUpdate()
    {
        if (!runInFixedUpdate) LockToWorldAxes();
    }

    void FixedUpdate()
    {
        if (runInFixedUpdate) LockToWorldAxes();
    }

    void LockToWorldAxes()
    {
        transform.rotation = Quaternion.identity;
    }
}
