using UnityEngine;

public class MK84TrajectoryPreview : MonoBehaviour
{
    public int stepCount = 60;
    public float timeStep = 0.1f;
    public float releaseForwardSpeedMultiplier = 1f;
    public LayerMask impactLayers = 1;
    public GameObject HudImpactPoint;
    public string TargetCameraTag;
    public Vector3 closestHitMinScale = new Vector3(15f, 15f, 15f);
    public Vector3 closestHitMaxScale = new Vector3(200f, 200f, 200f);
    public float closestHitMinScaleDistance = 0f;
    public float closestHitMaxScaleDistance = 10000f;

    bool previewVisible;
    GameObject currentHudImpactPointInstance;
    Transform targetCameraTransform;

    void Start()
    {
        if (!string.IsNullOrEmpty(TargetCameraTag))
        {
            GameObject targetObject = GameObject.FindGameObjectWithTag(TargetCameraTag);
            if (targetObject != null)
            {
                targetCameraTransform = targetObject.transform;
            }
        }
    }

    void Update()
    {
        if (!previewVisible)
        {
            return;
        }

        UpdateTrajectory();
    }

    public void SetPreviewVisible(bool visible)
    {
        previewVisible = visible;

        if (previewVisible)
        {
            UpdateTrajectory();
        }
        else
        {
            DestroyHudImpactPoint();
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

        Vector3 position = transform.position;
        Vector3 velocity = Vector3.zero;

        if (aircraftRb != null)
        {
            float forwardReleaseSpeed = Vector3.Dot(aircraftRb.linearVelocity, transform.forward) * releaseForwardSpeedMultiplier;
            velocity = transform.forward * forwardReleaseSpeed;
        }

        bool hasImpact = false;
        Vector3 impactPoint = Vector3.zero;

        for (int i = 1; i < stepCount; i++)
        {
            velocity += Physics.gravity * timeStep;
            Vector3 nextPosition = position + velocity * timeStep;

            if (Physics.Linecast(position, nextPosition, out RaycastHit hit, impactLayers, QueryTriggerInteraction.Ignore))
            {
                hasImpact = true;
                impactPoint = hit.point;
                break;
            }

            position = nextPosition;
        }

        if (hasImpact)
        {
            UpdateHudImpactPoint(impactPoint);
        }
        else
        {
            DestroyHudImpactPoint();
        }
    }

    void UpdateHudImpactPoint(Vector3 impactPoint)
    {
        if (HudImpactPoint == null)
        {
            return;
        }

        if (currentHudImpactPointInstance == null)
        {
            currentHudImpactPointInstance = Instantiate(HudImpactPoint, impactPoint, Quaternion.identity);
        }
        else
        {
            currentHudImpactPointInstance.transform.position = impactPoint;
        }

        UpdateHudImpactPointVisuals();
    }

    void UpdateHudImpactPointVisuals()
    {
        if (currentHudImpactPointInstance == null)
        {
            return;
        }

        if (targetCameraTransform != null)
        {
            Vector3 directionToTarget = targetCameraTransform.position - currentHudImpactPointInstance.transform.position;

            if (directionToTarget.sqrMagnitude > 0.0001f)
            {
                currentHudImpactPointInstance.transform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            }

            float distanceToTarget = Vector3.Distance(targetCameraTransform.position, currentHudImpactPointInstance.transform.position);
            float scaleT = closestHitMaxScaleDistance <= closestHitMinScaleDistance
                ? 1f
                : Mathf.InverseLerp(closestHitMinScaleDistance, closestHitMaxScaleDistance, distanceToTarget);

            currentHudImpactPointInstance.transform.localScale = Vector3.Lerp(closestHitMinScale, closestHitMaxScale, scaleT);
        }
        else
        {
            currentHudImpactPointInstance.transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
            currentHudImpactPointInstance.transform.localScale = closestHitMinScale;
        }
    }

    void DestroyHudImpactPoint()
    {
        if (currentHudImpactPointInstance != null)
        {
            Destroy(currentHudImpactPointInstance);
            currentHudImpactPointInstance = null;
        }
    }
}