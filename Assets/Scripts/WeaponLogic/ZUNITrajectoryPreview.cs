using UnityEngine;

public class ZUNITrajectoryPreview : MonoBehaviour
{
    public int stepCount = 80;
    public float timeStep = 0.05f;
    public LayerMask impactLayers = 1;
    public GameObject HudImpactPoint;
    public string TargetCameraTag;
    public Vector3 closestHitMinScale = new Vector3(15f, 15f, 15f);
    public Vector3 closestHitMaxScale = new Vector3(200f, 200f, 200f);
    public float closestHitMinScaleDistance = 0f;
    public float closestHitMaxScaleDistance = 10000f;

    ZUNIRocketLogic[] rockets;
    GameObject currentHudImpactPointInstance;
    bool previewVisible;
    Transform targetCameraTransform;

    void Awake()
    {
        rockets = GetComponentsInChildren<ZUNIRocketLogic>(true);
    }

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
        ZUNIRocketLogic rocket = GetNextAvailableRocket();
        if (rocket == null)
        {
            DestroyHudImpactPoint();
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

        bool hasImpact = false;
        Vector3 impactPoint = Vector3.zero;

        for (int i = 1; i < stepCount; i++)
        {
            velocity += acceleration * timeStep;
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