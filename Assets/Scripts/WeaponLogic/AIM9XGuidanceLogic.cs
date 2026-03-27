using UnityEngine;

public class AIM9XGuidanceLogic : MonoBehaviour
{
    public Transform targetTransform;
    public Rigidbody missileRigidbody;
    public Transform radarGimble;
    public PayloadManager payloadManager;
    public Collider ExplosionTrigger;
    public GameObject ExplosionPrefab;
    public bool preReleaseSeekerActive;
    public string mainCameraTag = "MainCamera";
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
    bool detonated;
    Transform mainCameraTransform;

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

        if (ExplosionTrigger != null)
        {
            ExplosionTrigger.enabled = false;
        }

        CacheMainCameraTransform();
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        boostTimer = 0f;
        seekerMemoryTimer = 0f;
        propulsionActive = false;
        released = false;
        targetLocked = false;
        detonated = false;
        preReleaseSeekerActive = false;
        designatedTargetTransform = targetTransform;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;
        lastRadarHitTarget = false;

        if (ExplosionTrigger != null)
        {
            ExplosionTrigger.enabled = false;
        }

        if (radarGimble != null)
        {
            radarGimble.localRotation = Quaternion.identity;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position;
        }
        else
        {
            lastRadarRayStart = transform.position;
            lastRadarRayEnd = transform.position;
        }

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
            payloadManager.HasHeatSource = false;
        }
    }

    void Update()
    {
        if (!released)
        {
            UpdateRailGimbleState();
        }
    }

    void FixedUpdate()
    {
        if (!released || detonated)
        {
            return;
        }

        lifeTimer += Time.fixedDeltaTime;
        if (lifeTimer >= lifeTime)
        {
            Detonate();
            return;
        }

        UpdateTargetKinematics();
        UpdateReleasedSeeker();
        UpdateGuidance();
        UpdatePropulsion();
    }

    public void SetPreReleaseSeekerActive(bool active)
    {
        preReleaseSeekerActive = active;

        if (!released && !preReleaseSeekerActive)
        {
            targetLocked = false;
            lastRadarHitTarget = false;
            seekerMemoryTimer = 0f;
            targetTransform = null;
            designatedTargetTransform = null;

            if (payloadManager != null)
            {
                payloadManager.HasHeatSource = false;
            }

            if (radarGimble != null)
            {
                radarGimble.localRotation = Quaternion.identity;
                lastRadarRayStart = radarGimble.position;
                lastRadarRayEnd = radarGimble.position;
            }
            else
            {
                lastRadarRayStart = transform.position;
                lastRadarRayEnd = transform.position;
            }
        }
    }

    public bool RefreshRailHeatSourceNow()
    {
        if (released)
        {
            return targetLocked;
        }

        UpdateRailGimbleState();
        return targetLocked;
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
        preReleaseSeekerActive = false;

        if (payloadManager != null)
        {
            payloadManager.HasHeatSource = false;
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

        if (ExplosionTrigger != null)
        {
            ExplosionTrigger.enabled = true;
        }
    }

    public void Detonate()
    {
        if (detonated)
        {
            return;
        }

        detonated = true;

        if (ExplosionPrefab != null)
        {
            Instantiate(ExplosionPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    void CacheMainCameraTransform()
    {
        GameObject mainCameraObject = GameObject.FindGameObjectWithTag(mainCameraTag);
        if (mainCameraObject != null)
        {
            mainCameraTransform = mainCameraObject.transform;
        }
        else
        {
            mainCameraTransform = null;
        }
    }

    void UpdateRailGimbleState()
    {
        if (radarGimble == null)
        {
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = transform.position;
            lastRadarRayEnd = transform.position;

            if (payloadManager != null)
            {
                payloadManager.HasHeatSource = false;
            }

            return;
        }

        if (!preReleaseSeekerActive)
        {
            radarGimble.localRotation = Quaternion.identity;
            targetLocked = false;
            lastRadarHitTarget = false;
            targetTransform = null;
            designatedTargetTransform = null;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position;

            if (payloadManager != null)
            {
                payloadManager.HasHeatSource = false;
            }

            return;
        }

        if (mainCameraTransform == null)
        {
            CacheMainCameraTransform();
        }

        if (mainCameraTransform == null)
        {
            radarGimble.localRotation = Quaternion.identity;
            targetLocked = false;
            lastRadarHitTarget = false;
            targetTransform = null;
            designatedTargetTransform = null;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position;

            if (payloadManager != null)
            {
                payloadManager.HasHeatSource = false;
            }

            return;
        }

        Transform reference = radarGimble.parent != null ? radarGimble.parent : transform;
        Quaternion localCameraRotation = Quaternion.Inverse(reference.rotation) * mainCameraTransform.rotation;
        Vector3 mainCameraLocalEuler = localCameraRotation.eulerAngles;

        float mainCameraX = Mathf.Clamp(NormalizeAngle(mainCameraLocalEuler.x), gimbleMinX, gimbleMaxX);
        float mainCameraY = Mathf.Clamp(NormalizeAngle(mainCameraLocalEuler.y), gimbleMinY, gimbleMaxY);

        radarGimble.localRotation = Quaternion.Euler(mainCameraX, mainCameraY, 0f);

        Vector3 searchRayStart = radarGimble.position;
        Vector3 searchRayDirection = radarGimble.forward;

        RaycastHit[] hits = Physics.SphereCastAll(searchRayStart, radarCastRadius, searchRayDirection, radarRange, radarMask, QueryTriggerInteraction.Ignore);

        HeatSignature hottestHeatSignature = null;
        RaycastHit hottestHeatHit = default;
        float hottestHeatValue = float.MinValue;

        for (int i = 0; i < hits.Length; i++)
        {
            HeatSignature heatSignature = hits[i].transform.GetComponentInParent<HeatSignature>();
            if (heatSignature == null)
            {
                continue;
            }

            float heatValue = heatSignature.GetHeatValue();

            if (hottestHeatSignature == null || heatValue > hottestHeatValue || (Mathf.Approximately(heatValue, hottestHeatValue) && hits[i].distance < hottestHeatHit.distance))
            {
                hottestHeatSignature = heatSignature;
                hottestHeatHit = hits[i];
                hottestHeatValue = heatValue;
            }
        }

        if (hottestHeatSignature != null)
        {
            Vector3 toHeatWorld = hottestHeatSignature.transform.position - radarGimble.position;

            if (toHeatWorld.sqrMagnitude > 0.000001f)
            {
                Vector3 localDirection = reference.InverseTransformDirection(toHeatWorld.normalized);
                Quaternion desiredLocalRotation = Quaternion.LookRotation(localDirection, Vector3.up);
                Vector3 desiredLocalEuler = desiredLocalRotation.eulerAngles;

                float x = Mathf.Clamp(NormalizeAngle(desiredLocalEuler.x), gimbleMinX, gimbleMaxX);
                float y = Mathf.Clamp(NormalizeAngle(desiredLocalEuler.y), gimbleMinY, gimbleMaxY);

                radarGimble.localRotation = Quaternion.Euler(x, y, 0f);
            }

            targetLocked = true;
            lastRadarHitTarget = true;
            targetTransform = hottestHeatSignature.transform;
            designatedTargetTransform = hottestHeatSignature.transform;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = hottestHeatHit.point;

            if (payloadManager != null)
            {
                payloadManager.HasHeatSource = true;
            }

            return;
        }

        targetLocked = false;
        lastRadarHitTarget = false;
        targetTransform = null;
        designatedTargetTransform = null;
        lastRadarRayStart = searchRayStart;
        lastRadarRayEnd = searchRayStart + searchRayDirection * radarRange;

        if (payloadManager != null)
        {
            payloadManager.HasHeatSource = false;
        }
    }

    void UpdateReleasedSeeker()
    {
        if (radarGimble == null)
        {
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = transform.position;
            lastRadarRayEnd = transform.position;
            return;
        }

        Transform trackingTarget = targetTransform != null ? targetTransform : designatedTargetTransform;

        if (trackingTarget == null)
        {
            radarGimble.localRotation = Quaternion.identity;
            targetLocked = false;
            lastRadarHitTarget = false;
            lastRadarRayStart = radarGimble.position;
            lastRadarRayEnd = radarGimble.position + radarGimble.forward * radarRange;
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
                targetTransform = designatedTargetTransform;
                return;
            }
        }

        targetLocked = false;
        lastRadarHitTarget = false;
        seekerMemoryTimer -= Time.fixedDeltaTime;

        if (seekerMemoryTimer > 0f && designatedTargetTransform != null)
        {
            targetTransform = designatedTargetTransform;
        }
        else
        {
            targetTransform = null;
            designatedTargetTransform = null;
        }
    }

    void UpdateTargetKinematics()
    {
        Transform trackingTarget = targetTransform != null ? targetTransform : designatedTargetTransform;

        if (trackingTarget == null)
        {
            targetVelocity = Vector3.zero;
            targetAcceleration = Vector3.zero;
            hasLastTargetPosition = false;
            hasPreviousTargetVelocity = false;
            return;
        }

        Rigidbody targetRb = trackingTarget.GetComponentInParent<Rigidbody>();

        if (targetRb != null)
        {
            Vector3 currentVelocity = targetRb.linearVelocity;
            targetAcceleration = hasPreviousTargetVelocity ? (currentVelocity - previousTargetVelocity) / Time.fixedDeltaTime : Vector3.zero;
            targetVelocity = currentVelocity;
            previousTargetVelocity = currentVelocity;
            hasPreviousTargetVelocity = true;
            lastTargetPosition = trackingTarget.position;
            hasLastTargetPosition = true;
            return;
        }

        if (useEstimatedVelocityIfNoRigidbody)
        {
            if (hasLastTargetPosition)
            {
                Vector3 estimatedVelocity = (trackingTarget.position - lastTargetPosition) / Time.fixedDeltaTime;
                targetAcceleration = hasPreviousTargetVelocity ? (estimatedVelocity - previousTargetVelocity) / Time.fixedDeltaTime : Vector3.zero;
                targetVelocity = estimatedVelocity;
                previousTargetVelocity = estimatedVelocity;
                hasPreviousTargetVelocity = true;
            }
            else
            {
                targetVelocity = Vector3.zero;
                targetAcceleration = Vector3.zero;
                hasPreviousTargetVelocity = false;
            }

            lastTargetPosition = trackingTarget.position;
            hasLastTargetPosition = true;
            return;
        }

        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;
        lastTargetPosition = trackingTarget.position;
        hasLastTargetPosition = true;
    }

    void UpdateGuidance()
    {
        if (missileRigidbody == null)
        {
            return;
        }

        Transform trackingTarget = targetTransform != null ? targetTransform : designatedTargetTransform;

        if (trackingTarget == null)
        {
            return;
        }

        Vector3 missilePosition = transform.position;
        Vector3 missileVelocity = missileRigidbody.linearVelocity;
        float missileSpeed = missileVelocity.magnitude;

        if (missileSpeed < minSpeed)
        {
            missileSpeed = minSpeed;
        }

        Vector3 interceptPoint = trackingTarget.position;
        float timeToIntercept = 0f;

        for (int i = 0; i < Mathf.Max(1, Mathf.RoundToInt(predictiveIterations)); i++)
        {
            float distance = Vector3.Distance(missilePosition, interceptPoint);
            timeToIntercept = missileSpeed > 0.01f ? distance / missileSpeed : 0f;
            interceptPoint = trackingTarget.position + targetVelocity * timeToIntercept + 0.5f * targetAcceleration * targetAccelerationInfluence * timeToIntercept * timeToIntercept;
        }

        predictedInterceptPoint = interceptPoint;

        Vector3 desiredDirection = (predictedInterceptPoint - missilePosition).normalized;
        if (desiredDirection.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        Quaternion desiredRotation = Quaternion.LookRotation(desiredDirection, transform.up);
        float maxStep = maxTurnRateDegrees * Time.fixedDeltaTime;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, desiredRotation, maxStep);

        Vector3 noseDirection = transform.forward;
        float noseTrackStep = noseTrackRateDegrees * Time.fixedDeltaTime;
        Vector3 steeredDirection = Vector3.RotateTowards(missileVelocity.normalized, noseDirection, Mathf.Deg2Rad * noseTrackStep, 0f);

        if (steeredDirection.sqrMagnitude > 0.000001f)
        {
            missileRigidbody.linearVelocity = steeredDirection.normalized * missileSpeed;
        }
    }

    void UpdatePropulsion()
    {
        if (missileRigidbody == null || !propulsionActive)
        {
            return;
        }

        float appliedForce = boostTimer < boostDuration ? boostForce : sustainForce;
        missileRigidbody.AddForce(transform.forward * appliedForce, ForceMode.Acceleration);
        boostTimer += Time.fixedDeltaTime;

        float speed = missileRigidbody.linearVelocity.magnitude;

        if (speed > maxSpeed)
        {
            missileRigidbody.linearVelocity = missileRigidbody.linearVelocity.normalized * maxSpeed;
        }
        else if (speed < minSpeed && speed > 0.001f)
        {
            missileRigidbody.linearVelocity = missileRigidbody.linearVelocity.normalized * minSpeed;
        }
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    void OnTriggerEnter(Collider other)
    {
        if (detonated)
        {
            return;
        }

        if (ExplosionTrigger == null)
        {
            return;
        }

        if (!released)
        {
            return;
        }

        if (other == null || other.isTrigger)
        {
            return;
        }

        if (other.transform == transform || other.transform.IsChildOf(transform))
        {
            return;
        }

        if (other.CompareTag("RCS"))
        {
            Detonate();
        }
    }

    void OnDrawGizmos()
    {
        if (radarGimble == null)
        {
            return;
        }

        Gizmos.color = lastRadarHitTarget ? Color.red : Color.green;
        Gizmos.DrawWireSphere(lastRadarRayEnd, gizmoSphereRadius);
        Gizmos.DrawLine(lastRadarRayStart, lastRadarRayEnd);
    }
}