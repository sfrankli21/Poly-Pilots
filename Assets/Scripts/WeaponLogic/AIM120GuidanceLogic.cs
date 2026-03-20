using UnityEngine;

public class AIM120GuidanceLogic : MonoBehaviour
{
    public Transform targetTransform;
    public Rigidbody missileRigidbody;
    public Transform radarGimble;
    public PayloadManager payloadManager;
    public float gimbleMinY = -60f;
    public float gimbleMaxY = 60f;
    public float gimbleMinX = -60f;
    public float gimbleMaxX = 60f;
    public float radarRange = 20000f;
    public float radarCastRadius = 5f;
    public LayerMask radarMask = ~0;
    public float boostForce = 200f;
    public float sustainForce = 0f;
    public float boostDuration = 4f;
    public float maxSpeed = 1200f;
    public float minSpeed = 150f;
    public float lifeTime = 25f;
    public float maxTurnRateDegrees = 45f;
    public float noseTrackRateDegrees = 120f;
    public float seekerMemoryTime = 1.25f;
    public float predictiveIterations = 3f;
    public float targetAccelerationInfluence = 0.35f;
    public bool useEstimatedVelocityIfNoRigidbody = true;
    public bool released;
    public bool propulsionActive;
    public bool detachOnRelease = true;
    public bool enablePhysicsOnRelease = true;
    public bool inheritParentVelocityOnRelease = true;
    public bool targetLocked;
    public float gizmoSphereRadius = 2f;
    public Vector3 inheritedLaunchVelocity;
    public bool launchVelocityAssigned;
    public Vector3 predictedInterceptPoint;
    public Vector3 targetVelocity;
    public Vector3 targetAcceleration;

    Vector3 lastTargetPosition;
    Vector3 previousTargetVelocity;
    bool hasLastTargetPosition;
    bool hasPreviousTargetVelocity;
    float lifeTimer;
    float boostTimer;
    float seekerMemoryTimer;
    bool lastRadarHitTarget;
    Vector3 lastRadarRayStart;
    Vector3 lastRadarRayEnd;
    Transform designatedTargetTransform;

    void Awake()
    {
        if (missileRigidbody == null)
        {
            missileRigidbody = GetComponent<Rigidbody>();
        }

        if (payloadManager == null)
        {
            payloadManager = GetComponentInParent<PayloadManager>();
        }
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        boostTimer = 0f;
        seekerMemoryTimer = 0f;
        propulsionActive = false;
        released = false;
        targetLocked = false;
        designatedTargetTransform = targetTransform;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;

        if (designatedTargetTransform != null)
        {
            lastTargetPosition = designatedTargetTransform.position;
            hasLastTargetPosition = true;
        }
        else
        {
            hasLastTargetPosition = false;
        }

        if (payloadManager != null)
        {
            payloadManager.AIM120TargetEstablished = false;
        }
    }

    void Update()
    {
        if (!released)
        {
            UpdateRadarGimble();
        }
    }

    void FixedUpdate()
    {
        if (!released)
        {
            return;
        }

        lifeTimer += Time.fixedDeltaTime;
        if (lifeTimer >= lifeTime)
        {
            Destroy(gameObject);
            return;
        }

        UpdateTargetKinematics();
        UpdateRadarGimble();
        UpdateGuidance();
        UpdatePropulsion();
    }

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        designatedTargetTransform = newTarget;
        targetLocked = false;
        seekerMemoryTimer = 0f;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;

        if (designatedTargetTransform != null)
        {
            lastTargetPosition = designatedTargetTransform.position;
            hasLastTargetPosition = true;
        }
        else
        {
            hasLastTargetPosition = false;
        }
    }

    public void SetLaunchVelocity(Vector3 velocity)
    {
        inheritedLaunchVelocity = velocity;
        launchVelocityAssigned = true;
    }

    public void Release()
    {
        if (released)
        {
            return;
        }

        if (!launchVelocityAssigned && inheritParentVelocityOnRelease)
        {
            Rigidbody parentRb = GetComponentInParent<Rigidbody>();
            if (parentRb != null)
            {
                inheritedLaunchVelocity = parentRb.linearVelocity;
                launchVelocityAssigned = true;
            }
        }

        released = true;
        propulsionActive = true;
        boostTimer = 0f;
        seekerMemoryTimer = seekerMemoryTime;

        if (payloadManager != null)
        {
            payloadManager.AIM120TargetEstablished = false;
        }

        if (detachOnRelease)
        {
            transform.SetParent(null, true);
        }

        if (missileRigidbody != null && enablePhysicsOnRelease)
        {
            missileRigidbody.isKinematic = false;
            missileRigidbody.linearVelocity += inheritedLaunchVelocity;
        }
    }

    void UpdateTargetKinematics()
    {
        Transform kinematicTarget = targetTransform != null ? targetTransform : designatedTargetTransform;

        if (kinematicTarget == null)
        {
            targetVelocity = Vector3.zero;
            targetAcceleration = Vector3.zero;
            hasPreviousTargetVelocity = false;
            return;
        }

        Vector3 newVelocity = Vector3.zero;
        Rigidbody targetRb = kinematicTarget.GetComponentInParent<Rigidbody>();

        if (targetRb != null)
        {
            newVelocity = targetRb.linearVelocity;
            lastTargetPosition = kinematicTarget.position;
            hasLastTargetPosition = true;
        }
        else
        {
            if (!useEstimatedVelocityIfNoRigidbody)
            {
                newVelocity = Vector3.zero;
            }
            else
            {
                if (!hasLastTargetPosition)
                {
                    lastTargetPosition = kinematicTarget.position;
                    hasLastTargetPosition = true;
                    newVelocity = Vector3.zero;
                }
                else
                {
                    Vector3 currentPosition = kinematicTarget.position;
                    newVelocity = (currentPosition - lastTargetPosition) / Time.fixedDeltaTime;
                    lastTargetPosition = currentPosition;
                }
            }
        }

        if (hasPreviousTargetVelocity)
        {
            targetAcceleration = (newVelocity - previousTargetVelocity) / Time.fixedDeltaTime;
        }
        else
        {
            targetAcceleration = Vector3.zero;
            hasPreviousTargetVelocity = true;
        }

        previousTargetVelocity = newVelocity;
        targetVelocity = newVelocity;
    }

    void UpdateRadarGimble()
    {
        if (radarGimble == null)
        {
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = transform.position;
            lastRadarRayEnd = transform.position;
            UpdatePayloadTargetEstablished(false);
            return;
        }

        Transform trackingTarget = released ? designatedTargetTransform : targetTransform;

        if (trackingTarget == null)
        {
            radarGimble.localRotation = Quaternion.identity;
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position + radarGimble.forward * radarRange;
            UpdatePayloadTargetEstablished(false);
            return;
        }

        Transform reference = radarGimble.parent != null ? radarGimble.parent : transform;
        Vector3 toTargetWorld = trackingTarget.position - radarGimble.position;

        if (toTargetWorld.sqrMagnitude <= 0.000001f)
        {
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position;
            UpdatePayloadTargetEstablished(false);
            return;
        }

        Vector3 localDirection = reference.InverseTransformDirection(toTargetWorld.normalized);
        Quaternion desiredLocalRotation = Quaternion.LookRotation(localDirection, Vector3.up);

        Vector3 localEuler = desiredLocalRotation.eulerAngles;
        float x = NormalizeAngle(localEuler.x);
        float y = NormalizeAngle(localEuler.y);

        x = Mathf.Clamp(x, gimbleMinX, gimbleMaxX);
        y = Mathf.Clamp(y, gimbleMinY, gimbleMaxY);

        radarGimble.localRotation = Quaternion.Euler(x, y, 0f);

        Vector3 rayStart = radarGimble.position;
        Vector3 rayDirection = radarGimble.forward;
        lastRadarRayStart = rayStart;
        lastRadarRayEnd = rayStart + rayDirection * radarRange;

        bool hitSomething = Physics.SphereCast(rayStart, radarCastRadius, rayDirection, out RaycastHit hit, radarRange, radarMask, QueryTriggerInteraction.Ignore);

        if (hitSomething)
        {
            lastRadarRayEnd = hit.point;

            if (hit.transform == trackingTarget || hit.transform.IsChildOf(trackingTarget))
            {
                targetLocked = true;
                lastRadarHitTarget = true;
                seekerMemoryTimer = seekerMemoryTime;

                if (released)
                {
                    targetTransform = designatedTargetTransform;
                }

                UpdatePayloadTargetEstablished(true);
                return;
            }
        }

        targetLocked = false;
        lastRadarHitTarget = false;

        if (released)
        {
            seekerMemoryTimer -= Time.fixedDeltaTime;

            if (seekerMemoryTimer <= 0f)
            {
                targetTransform = null;
            }
        }

        UpdatePayloadTargetEstablished(false);
    }

    void UpdatePayloadTargetEstablished(bool value)
    {
        if (released)
        {
            return;
        }

        if (payloadManager == null)
        {
            return;
        }

        int selectedPylonIndex = (int)payloadManager.CurrentSelectedPylon;
        if (selectedPylonIndex < 0 || selectedPylonIndex >= payloadManager.Pylons.Length)
        {
            payloadManager.AIM120TargetEstablished = false;
            return;
        }

        Transform selectedPylonTransform = payloadManager.Pylons[selectedPylonIndex] != null ? payloadManager.Pylons[selectedPylonIndex].pylonTransform : null;
        if (selectedPylonTransform == null)
        {
            payloadManager.AIM120TargetEstablished = false;
            return;
        }

        if (!transform.IsChildOf(selectedPylonTransform))
        {
            payloadManager.AIM120TargetEstablished = false;
            return;
        }

        payloadManager.AIM120TargetEstablished = value;
    }

    void UpdateGuidance()
    {
        if (missileRigidbody == null)
        {
            return;
        }

        Vector3 currentVelocity = missileRigidbody.linearVelocity;
        float currentSpeed = currentVelocity.magnitude;

        if (currentSpeed < minSpeed)
        {
            if (currentVelocity.sqrMagnitude <= 0.0001f)
            {
                currentVelocity = transform.forward * minSpeed;
            }
            else
            {
                currentVelocity = currentVelocity.normalized * minSpeed;
            }

            missileRigidbody.linearVelocity = currentVelocity;
            currentSpeed = minSpeed;
        }

        if (targetTransform == null)
        {
            predictedInterceptPoint = transform.position + transform.forward * 1000f;
            AlignNoseToVelocity(currentVelocity.normalized);
            return;
        }

        predictedInterceptPoint = CalculateInterceptPoint(currentVelocity, currentSpeed);

        Vector3 desiredDirection = (predictedInterceptPoint - transform.position).normalized;
        if (desiredDirection.sqrMagnitude <= 0.000001f)
        {
            AlignNoseToVelocity(currentVelocity.normalized);
            return;
        }

        Vector3 currentDirection = currentVelocity.sqrMagnitude > 0.000001f ? currentVelocity.normalized : transform.forward;
        Vector3 newDirection = Vector3.RotateTowards(
            currentDirection,
            desiredDirection,
            Mathf.Deg2Rad * maxTurnRateDegrees * Time.fixedDeltaTime,
            0f
        ).normalized;

        missileRigidbody.linearVelocity = newDirection * currentSpeed;
        AlignNoseToVelocity(newDirection);
    }

    Vector3 CalculateInterceptPoint(Vector3 missileVelocity, float missileSpeed)
    {
        Vector3 missilePosition = transform.position;
        Vector3 targetPosition = targetTransform.position;
        Vector3 estimatedTargetPosition = targetPosition;
        Vector3 estimatedTargetVelocity = targetVelocity;
        Vector3 toTarget = estimatedTargetPosition - missilePosition;
        float range = toTarget.magnitude;
        float timeToGo = range / Mathf.Max(missileSpeed, 1f);

        int iterations = Mathf.Max(1, Mathf.RoundToInt(predictiveIterations));

        for (int i = 0; i < iterations; i++)
        {
            estimatedTargetPosition = targetPosition
                + estimatedTargetVelocity * timeToGo
                + 0.5f * targetAcceleration * targetAccelerationInfluence * timeToGo * timeToGo;

            range = Vector3.Distance(missilePosition, estimatedTargetPosition);
            timeToGo = range / Mathf.Max(missileSpeed, 1f);
        }

        Vector3 interceptPoint = estimatedTargetPosition;
        

        return interceptPoint;
    }

    void AlignNoseToVelocity(Vector3 velocityDirection)
    {
        if (velocityDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(velocityDirection, Vector3.up);
        Quaternion newRotation = Quaternion.RotateTowards(
            missileRigidbody.rotation,
            targetRotation,
            noseTrackRateDegrees * Time.fixedDeltaTime
        );

        missileRigidbody.MoveRotation(newRotation);
    }

    void UpdatePropulsion()
    {
        if (!propulsionActive || missileRigidbody == null)
        {
            return;
        }

        if (boostTimer < boostDuration)
        {
            missileRigidbody.AddForce(transform.forward * boostForce, ForceMode.Force);
            boostTimer += Time.fixedDeltaTime;
        }
        else if (sustainForce > 0f)
        {
            missileRigidbody.AddForce(transform.forward * sustainForce, ForceMode.Force);
        }

        float speed = missileRigidbody.linearVelocity.magnitude;
        if (speed > maxSpeed)
        {
            missileRigidbody.linearVelocity = missileRigidbody.linearVelocity.normalized * maxSpeed;
        }
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    void OnDrawGizmos()
    {
        if (radarGimble != null)
        {
            Gizmos.color = lastRadarHitTarget ? Color.green : Color.red;
            Gizmos.DrawLine(lastRadarRayStart, lastRadarRayEnd);

            if (radarCastRadius > 0f)
            {
                Gizmos.DrawWireSphere(lastRadarRayStart, radarCastRadius);
                Gizmos.DrawWireSphere(lastRadarRayEnd, radarCastRadius);
            }

            if (gizmoSphereRadius > 0f)
            {
                Gizmos.DrawSphere(lastRadarRayEnd, gizmoSphereRadius);
            }
        }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(predictedInterceptPoint, gizmoSphereRadius);
    }
}