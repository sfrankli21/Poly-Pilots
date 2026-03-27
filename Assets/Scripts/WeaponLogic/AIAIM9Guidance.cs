using UnityEngine;
using UnityEngine.Events;
using FMODUnity;
using FMOD.Studio;

public class AIAIM9Guidance : MonoBehaviour
{
    public Transform targetTransform;
    public Rigidbody missileRigidbody;
    public Transform radarGimble;
    public Collider ExplosionTrigger;
    public GameObject ExplosionPrefab;
    public float gimbleMinY = -90f;
    public float gimbleMaxY = 90f;
    public float gimbleMinX = -90f;
    public float gimbleMaxX = 90f;
    public float radarRange = 10000f;
    public float radarCastRadius = 5f;
    public LayerMask radarMask;
    public float boostForce = 200f;
    public float sustainForce = 0f;
    public float boostDuration = 4f;
    public float maxSpeed = 1200f;
    public float minSpeed = 150f;
    public float lifeTime = 25f;
    public float maxTurnRateDegrees = 45f;
    public float noseTrackRateDegrees = 120f;
    public float predictiveIterations = 3f;
    public float targetAccelerationInfluence = 0.35f;
    public bool useEstimatedVelocityIfNoRigidbody = true;
    public bool released;
    public bool propulsionActive;
    public bool detachOnRelease = true;
    public bool enablePhysicsOnRelease = true;
    public bool inheritParentVelocityOnRelease = true;
    public Vector3 inheritedLaunchVelocity;
    public bool launchVelocityAssigned;
    public Vector3 predictedInterceptPoint;
    public Vector3 targetVelocity;
    public Vector3 targetAcceleration;
    public bool targetInSeeker;
    public bool drawSeekerGizmos = true;
    public Color seekerPathColor = Color.yellow;
    public Color seekerHitColor = Color.red;
    public float seekerHitMarkerSize = 1f;
    public EventReference RWRLaunch;
    public string RWRID;
    public UnityEvent OnReleased;

    public string lastSeekerHitName;
    public string lastHeatTargetName;
    public float lastHeatTargetValue;
    public float lastHeatTargetDistance;

    Vector3 lastTargetPosition;
    Vector3 previousTargetVelocity;
    bool hasLastTargetPosition;
    bool hasPreviousTargetVelocity;
    float lifeTimer;
    float boostTimer;
    bool detonated;
    bool seekerHasHit;
    Vector3 seekerHitPoint;
    float seekerLastCastDistance;
    EventInstance rwrLaunchInstance;
    bool rwrLaunchPlaying;

    void Awake()
    {
        if (missileRigidbody == null)
        {
            missileRigidbody = GetComponent<Rigidbody>();
        }

        if (ExplosionTrigger != null)
        {
            ExplosionTrigger.enabled = false;
        }
    }

    void OnEnable()
    {
        lifeTimer = 0f;
        boostTimer = 0f;
        propulsionActive = false;
        released = false;
        detonated = false;
        targetInSeeker = false;
        seekerHasHit = false;
        seekerHitPoint = Vector3.zero;
        seekerLastCastDistance = radarRange;
        lastSeekerHitName = "";
        lastHeatTargetName = "";
        lastHeatTargetValue = 0f;
        lastHeatTargetDistance = 0f;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;
        StopRWRLaunchLoop();

        if (targetTransform != null)
        {
            lastTargetPosition = targetTransform.position;
            hasLastTargetPosition = true;
        }
        else
        {
            hasLastTargetPosition = false;
        }

        if (ExplosionTrigger != null)
        {
            ExplosionTrigger.enabled = false;
        }
    }

    void OnDisable()
    {
        StopRWRLaunchLoop();
    }

    void OnDestroy()
    {
        StopRWRLaunchLoop();
    }

    void FixedUpdate()
    {
        UpdateRadarGimble();

        if (!released)
        {
            targetInSeeker = false;
            seekerHasHit = false;
            seekerLastCastDistance = radarRange;
            lastSeekerHitName = "";
            lastHeatTargetName = "";
            lastHeatTargetValue = 0f;
            lastHeatTargetDistance = 0f;
        }
        else
        {
            UpdateSeekerTarget();
        }

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
        UpdateGuidance();
        UpdatePropulsion();
        UpdateRWRLaunch3DAttributes();
    }

    public void SetTarget(Transform newTarget)
    {
        targetTransform = newTarget;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;

        if (targetTransform != null)
        {
            lastTargetPosition = targetTransform.position;
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
        targetInSeeker = false;
        hasPreviousTargetVelocity = false;
        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        seekerHasHit = false;
        seekerLastCastDistance = radarRange;
        lastSeekerHitName = "";
        lastHeatTargetName = "";
        lastHeatTargetValue = 0f;
        lastHeatTargetDistance = 0f;

        if (targetTransform != null)
        {
            lastTargetPosition = targetTransform.position;
            hasLastTargetPosition = true;
        }
        else
        {
            hasLastTargetPosition = false;
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

        StartRWRLaunchLoop();
        OnReleased.Invoke();
    }

    public void Detonate()
    {
        if (detonated)
        {
            return;
        }

        detonated = true;
        StopRWRLaunchLoop();

        if (ExplosionPrefab != null)
        {
            Instantiate(ExplosionPrefab, transform.position, transform.rotation);
        }

        Destroy(gameObject);
    }

    void StartRWRLaunchLoop()
    {
        if (RWRLaunch.IsNull)
        {
            return;
        }

        StopRWRLaunchLoop();
        rwrLaunchInstance = RuntimeManager.CreateInstance(RWRLaunch);
        RuntimeManager.AttachInstanceToGameObject(rwrLaunchInstance, transform, missileRigidbody);
        rwrLaunchInstance.start();
        rwrLaunchPlaying = true;
    }

    void StopRWRLaunchLoop()
    {
        if (!rwrLaunchPlaying)
        {
            return;
        }

        rwrLaunchInstance.stop(STOP_MODE.IMMEDIATE);
        rwrLaunchInstance.release();
        rwrLaunchPlaying = false;
    }

    void UpdateRWRLaunch3DAttributes()
    {
        if (!rwrLaunchPlaying)
        {
            return;
        }

        RuntimeManager.AttachInstanceToGameObject(rwrLaunchInstance, transform, missileRigidbody);
    }

    void UpdateRadarGimble()
    {
        if (radarGimble == null)
        {
            return;
        }

        if (targetTransform == null)
        {
            return;
        }

        Vector3 localTargetDirection = transform.InverseTransformPoint(targetTransform.position);
        Vector3 normalizedLocalDirection = localTargetDirection.normalized;

        float yaw = Mathf.Atan2(normalizedLocalDirection.x, normalizedLocalDirection.z) * Mathf.Rad2Deg;
        float pitch = -Mathf.Atan2(normalizedLocalDirection.y, normalizedLocalDirection.z) * Mathf.Rad2Deg;

        yaw = Mathf.Clamp(yaw, gimbleMinY, gimbleMaxY);
        pitch = Mathf.Clamp(pitch, gimbleMinX, gimbleMaxX);

        radarGimble.localRotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void UpdateSeekerTarget()
    {
        if (radarGimble == null)
        {
            targetInSeeker = false;
            seekerHasHit = false;
            seekerLastCastDistance = radarRange;
            lastSeekerHitName = "";
            lastHeatTargetName = "";
            lastHeatTargetValue = 0f;
            lastHeatTargetDistance = 0f;
            return;
        }

        Vector3 castOrigin = radarGimble.position;
        Vector3 castDirection = radarGimble.forward;

        RaycastHit[] hits = Physics.SphereCastAll(
            castOrigin,
            radarCastRadius,
            castDirection,
            radarRange,
            radarMask,
            QueryTriggerInteraction.Ignore
        );

        HeatSignature bestHeatSignature = null;
        float bestHeatValue = float.MinValue;
        float bestDistance = float.MaxValue;

        seekerHasHit = false;
        seekerLastCastDistance = radarRange;
        lastSeekerHitName = "";
        lastHeatTargetName = "";
        lastHeatTargetValue = 0f;
        lastHeatTargetDistance = 0f;

        for (int i = 0; i < hits.Length; i++)
        {
            Collider hitCollider = hits[i].collider;

            if (hitCollider == null)
            {
                continue;
            }

            if (!seekerHasHit || hits[i].distance < seekerLastCastDistance)
            {
                seekerHasHit = true;
                seekerLastCastDistance = hits[i].distance;
                seekerHitPoint = hits[i].point;
                lastSeekerHitName = hitCollider.name;
            }

            HeatSignature heatSignature = GetHeatSignature(hitCollider);

            if (heatSignature == null)
            {
                continue;
            }

            if (!IsInRadarMask(heatSignature.transform))
            {
                continue;
            }

            float heatValue = heatSignature.GetHeatValue();
            float hitDistance = hits[i].distance;

            if (heatValue > bestHeatValue || (Mathf.Approximately(heatValue, bestHeatValue) && hitDistance < bestDistance))
            {
                bestHeatValue = heatValue;
                bestDistance = hitDistance;
                bestHeatSignature = heatSignature;
                seekerHitPoint = hits[i].point;
            }
        }

        if (bestHeatSignature != null)
        {
            targetTransform = bestHeatSignature.transform;
            targetInSeeker = true;
            lastHeatTargetName = bestHeatSignature.name;
            lastHeatTargetValue = bestHeatValue;
            lastHeatTargetDistance = bestDistance;
        }
        else
        {
            targetInSeeker = false;
        }
    }

    bool IsInRadarMask(Transform t)
    {
        if (t == null)
        {
            return false;
        }

        return ((1 << t.gameObject.layer) & radarMask.value) != 0;
    }

    HeatSignature GetHeatSignature(Collider hitCollider)
    {
        HeatSignature heatSignature = hitCollider.GetComponent<HeatSignature>();

        if (heatSignature != null)
        {
            return heatSignature;
        }

        heatSignature = hitCollider.GetComponentInParent<HeatSignature>();

        if (heatSignature != null)
        {
            return heatSignature;
        }

        if (hitCollider.attachedRigidbody != null)
        {
            heatSignature = hitCollider.attachedRigidbody.GetComponent<HeatSignature>();

            if (heatSignature != null)
            {
                return heatSignature;
            }

            heatSignature = hitCollider.attachedRigidbody.GetComponentInParent<HeatSignature>();

            if (heatSignature != null)
            {
                return heatSignature;
            }
        }

        return hitCollider.transform.root.GetComponent<HeatSignature>();
    }

    void UpdateTargetKinematics()
    {
        if (targetTransform == null)
        {
            targetVelocity = Vector3.zero;
            targetAcceleration = Vector3.zero;
            hasLastTargetPosition = false;
            hasPreviousTargetVelocity = false;
            return;
        }

        Rigidbody targetRb = targetTransform.GetComponentInParent<Rigidbody>();

        if (targetRb != null)
        {
            Vector3 currentVelocity = targetRb.linearVelocity;
            targetAcceleration = hasPreviousTargetVelocity ? (currentVelocity - previousTargetVelocity) / Time.fixedDeltaTime : Vector3.zero;
            targetVelocity = currentVelocity;
            previousTargetVelocity = currentVelocity;
            hasPreviousTargetVelocity = true;
            lastTargetPosition = targetTransform.position;
            hasLastTargetPosition = true;
            return;
        }

        if (useEstimatedVelocityIfNoRigidbody)
        {
            if (hasLastTargetPosition)
            {
                Vector3 estimatedVelocity = (targetTransform.position - lastTargetPosition) / Time.fixedDeltaTime;
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

            lastTargetPosition = targetTransform.position;
            hasLastTargetPosition = true;
            return;
        }

        targetVelocity = Vector3.zero;
        targetAcceleration = Vector3.zero;
        previousTargetVelocity = Vector3.zero;
        hasPreviousTargetVelocity = false;
        lastTargetPosition = targetTransform.position;
        hasLastTargetPosition = true;
    }

    void UpdateGuidance()
    {
        if (missileRigidbody == null)
        {
            return;
        }

        if (targetTransform == null)
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

        Vector3 interceptPoint = targetTransform.position;
        float timeToIntercept = 0f;

        for (int i = 0; i < Mathf.Max(1, Mathf.RoundToInt(predictiveIterations)); i++)
        {
            float distance = Vector3.Distance(missilePosition, interceptPoint);
            timeToIntercept = missileSpeed > 0.01f ? distance / missileSpeed : 0f;
            interceptPoint = targetTransform.position + targetVelocity * timeToIntercept + 0.5f * targetAcceleration * targetAccelerationInfluence * timeToIntercept * timeToIntercept;
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

        Vector3 currentVelocityDirection = missileVelocity.sqrMagnitude > 0.000001f ? missileVelocity.normalized : transform.forward;
        float noseTrackStep = noseTrackRateDegrees * Time.fixedDeltaTime;
        Vector3 steeredDirection = Vector3.RotateTowards(currentVelocityDirection, transform.forward, Mathf.Deg2Rad * noseTrackStep, 0f);

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

        if (other.CompareTag("PlayerRCS"))
        {
            Detonate();
        }
        if (other.CompareTag("Flare"))
        {
            Detonate();
        }
    }

    void OnDrawGizmos()
    {
        if (!drawSeekerGizmos)
        {
            return;
        }

        if (radarGimble == null)
        {
            return;
        }

        Gizmos.color = seekerPathColor;
        Vector3 origin = radarGimble.position;
        Vector3 direction = radarGimble.forward;
        float drawDistance = seekerHasHit ? seekerLastCastDistance : radarRange;
        Gizmos.DrawLine(origin, origin + direction * drawDistance);

        if (seekerHasHit)
        {
            Gizmos.color = seekerHitColor;
            Gizmos.DrawSphere(seekerHitPoint, seekerHitMarkerSize);
        }
    }
}