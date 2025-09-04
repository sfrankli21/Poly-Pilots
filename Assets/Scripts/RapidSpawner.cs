using UnityEngine;
using System.Collections;

public class RapidSpawner : MonoBehaviour
{
    [Header("Prefab to Spawn")]
    public GameObject prefab;

    [Header("Spawn Settings")]
    public float interval = 0.5f; // seconds between spawns

    void Start()
    {
        if (prefab != null)
            StartCoroutine(SpawnRoutine());
        else
            Debug.LogWarning("No prefab assigned to RapidSpawner.");
    }

    IEnumerator SpawnRoutine()
    {
        while (true)
        {
            Instantiate(prefab, transform.position, transform.rotation);
            yield return new WaitForSeconds(interval);
        }
    }
}
