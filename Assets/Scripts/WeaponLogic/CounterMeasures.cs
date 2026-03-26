using UnityEngine;

public class CounterMeasures : MonoBehaviour
{
    public InputRouter inputRouter;
    public GameObject counterMeasurePrefab;
    public Transform spawnPoint;

    bool lastFireCounterMeasure;

    void Update()
    {
        if (inputRouter == null || counterMeasurePrefab == null || spawnPoint == null)
        {
            return;
        }

        bool currentFireCounterMeasure = inputRouter.FireCounterMeasure;

        if (currentFireCounterMeasure && !lastFireCounterMeasure)
        {
            Instantiate(counterMeasurePrefab, spawnPoint.position, spawnPoint.rotation);
        }

        lastFireCounterMeasure = currentFireCounterMeasure;
    }
}