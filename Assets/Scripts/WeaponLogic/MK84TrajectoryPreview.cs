using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class MK84TrajectoryPreview : MonoBehaviour
{
    public int stepCount = 60;
    public float timeStep = 0.1f;
    public float releaseForwardSpeedMultiplier = 1f;

    LineRenderer lineRenderer;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    void Update()
    {
        if (!lineRenderer.enabled)
        {
            return;
        }

        UpdateTrajectory();
    }

    public void SetPreviewVisible(bool visible)
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        lineRenderer.enabled = visible;

        if (visible)
        {
            UpdateTrajectory();
        }
    }

    void UpdateTrajectory()
    {
        Rigidbody aircraftRb = null;
        Transform current = transform.parent;

        while (current != null)
        {
            aircraftRb = current.GetComponent<Rigidbody>();
            if (aircraftRb != null)
            {
                break;
            }

            current = current.parent;
        }

        Vector3 startPosition = transform.position;
        Vector3 initialVelocity = Vector3.zero;

        if (aircraftRb != null)
        {
            float forwardReleaseSpeed = Vector3.Dot(aircraftRb.linearVelocity, transform.forward) * releaseForwardSpeedMultiplier;
            initialVelocity = transform.forward * forwardReleaseSpeed;
        }

        lineRenderer.positionCount = stepCount;

        for (int i = 0; i < stepCount; i++)
        {
            float t = i * timeStep;
            Vector3 point = startPosition + initialVelocity * t + 0.5f * Physics.gravity * t * t;
            lineRenderer.SetPosition(i, point);
        }
    }
}