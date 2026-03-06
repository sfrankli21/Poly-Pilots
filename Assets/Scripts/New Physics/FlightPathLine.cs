using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class FlightPathLine : MonoBehaviour
{
    [SerializeField]
    GameObject linePointPrefab;
    [SerializeField]
    float spawnInterval = 0.25f;

    LineRenderer lineRenderer;
    readonly List<Transform> points = new List<Transform>();
    float spawnTimer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        RedrawLine();
    }

    void Update()
    {
        spawnTimer += Time.deltaTime;

        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnPoint();
        }

        RedrawLine();
    }

    void SpawnPoint()
    {
        if (linePointPrefab == null)
        {
            return;
        }

        GameObject pointObject = Instantiate(linePointPrefab, transform.position, Quaternion.identity);
        pointObject.transform.SetParent(null);
        points.Add(pointObject.transform);
        RedrawLine();
    }

    void RedrawLine()
    {
        int validPointCount = 0;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] != null)
            {
                validPointCount++;
            }
        }

        lineRenderer.positionCount = validPointCount;

        int lineIndex = 0;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
            {
                continue;
            }

            lineRenderer.SetPosition(lineIndex, points[i].position);
            lineIndex++;
        }
    }
}