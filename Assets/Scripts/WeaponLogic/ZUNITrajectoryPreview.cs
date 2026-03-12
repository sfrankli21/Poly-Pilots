using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ZUNITrajectoryPreview : MonoBehaviour
{
    public int stepCount = 80;
    public float timeStep = 0.05f;
    public LayerMask impactLayers = 1;

    LineRenderer lineRenderer;
    ZUNIRocketLogic[] rockets;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        rockets = GetComponentsInChildren<ZUNIRocketLogic>(true);

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    void Update()
    {
        if (lineRenderer == null)
        {
            return;
        }

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

        if (lineRenderer == null)
        {
            return;
        }

        lineRenderer.enabled = visible;

        if (visible)
        {
            UpdateTrajectory();
        }
    }

    void UpdateTrajectory()
    {
        if (lineRenderer == null)
        {
            return;
        }

        ZUNIRocketLogic rocket = GetNextAvailableRocket();
        if (rocket == null)
        {
            lineRenderer.positionCount = 0;
            return;
        }

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

        Vector3 position = rocket.transform.position;
        Vector3 forward = rocket.transform.forward.normalized;

        float releaseForwardSpeed = 0f;
        if (aircraftRb != null)
        {
            releaseForwardSpeed = Vector3.Dot(aircraftRb.linearVelocity, forward);
        }

        Vector3 velocity = forward * releaseForwardSpeed;
        Vector3 acceleration = (forward * rocket.pushForce) + (Physics.gravity * (1f + rocket.gravityMultiplier));

        lineRenderer.positionCount = stepCount;
        lineRenderer.SetPosition(0, position);

        int actualCount = 1;

        for (int i = 1; i < stepCount; i++)
        {
            velocity += acceleration * timeStep;
            Vector3 nextPosition = position + velocity * timeStep;

            if (Physics.Linecast(position, nextPosition, out RaycastHit hit, impactLayers, QueryTriggerInteraction.Ignore))
            {
                lineRenderer.SetPosition(actualCount, hit.point);
                actualCount++;
                break;
            }

            lineRenderer.SetPosition(actualCount, nextPosition);
            actualCount++;
            position = nextPosition;
        }

        lineRenderer.positionCount = actualCount;
    }

    ZUNIRocketLogic GetNextAvailableRocket()
    {
        if (rockets == null || rockets.Length == 0)
        {
            rockets = GetComponentsInChildren<ZUNIRocketLogic>(true);
        }

        if (rockets == null || rockets.Length == 0)
        {
            return null;
        }

        for (int i = 0; i < rockets.Length; i++)
        {
            if (rockets[i] != null && !rockets[i].HasFired)
            {
                return rockets[i];
            }
        }

        return null;
    }
}