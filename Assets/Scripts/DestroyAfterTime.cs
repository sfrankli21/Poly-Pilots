using UnityEngine;

public class DestroyAfterTime : MonoBehaviour
{
    public float destroyDelay = 5f;

    void Start()
    {
        Destroy(gameObject, destroyDelay);
    }
}