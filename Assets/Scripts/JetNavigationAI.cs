using System.Collections.Generic;
using UnityEngine;

public class JetNavigationAI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] JetMechanics jet;
    [SerializeField] Transform target;

    [Header("Steering")]
    [SerializeField] float steeringSpeed = 3f;
    [SerializeField] float rollFactor = 1f;
    [SerializeField] float yawFactor = 1f;
    [SerializeField] float fineSteeringAngle = 10f;

    [Header("Throttle")]
    [SerializeField] float minSpeed = 40f;
    [SerializeField] float maxSpeed = 120f;
    [SerializeField] float groundAvoidThrottle = 1f;
    [SerializeField] float recoverSpeedThrottle = 1f;

    [Header("Ground Avoidance")]
    [SerializeField] bool useGroundAvoidance = true;
    [SerializeField] LayerMask groundMask = ~0;
    [SerializeField] float minimumSafeAltitude = 150f;
    [SerializeField] float forwardCollisionCheckDistance = 400f;
    [SerializeField] float groundAvoidanceRollStrength = 2f;
    [SerializeField] float groundAvoidanceYawStrength = 2f;
    [SerializeField] float localHitXForFullRoll = 50f;
    [SerializeField] float localHitXForFullYaw = 50f;
    [SerializeField] float altitudeRecoveryPitch = -0.7f;
    [SerializeField] float collisionRecoveryPitch = -1f;

    [Header("Low Altitude Protection")]
    [SerializeField] float criticalAltitude = 75f;
    [SerializeField] float wingsLevelRollStrength = 2f;
    [SerializeField] float lowAltitudeYawSuppression = 1f;
    [SerializeField] float lowAltitudeRollSuppression = 1f;

    [Header("Recovery")]
    [SerializeField] bool useSpeedRecovery = true;
    [SerializeField] float recoverBelowSpeed = 30f;
    [SerializeField] float recoveryPitchStrength = 2f;
    [SerializeField] float recoveryRollStrength = 2f;

    [Header("Reaction Delay")]
    [SerializeField] bool useReactionDelay = true;
    [SerializeField] float reactionDelayMin = 0.05f;
    [SerializeField] float reactionDelayMax = 0.4f;
    [SerializeField] float reactionDistanceMin = 100f;
    [SerializeField] float reactionDistanceMax = 3000f;

    struct DelayedTargetPoint
    {
        public Vector3 position;
        public float time;

        public DelayedTargetPoint(Vector3 position, float time)
        {
            this.position = position;
            this.time = time;
        }
    }

    readonly Queue<DelayedTargetPoint> inputQueue = new Queue<DelayedTargetPoint>();

    Vector3 lastInput;

    bool hasAltitudeHit;
    bool hasForwardHit;
    RaycastHit altitudeHit;
    RaycastHit forwardHit;

    public Transform Target
    {
        get => target;
        set => target = value;
    }

    void Reset()
    {
        jet = GetComponent<JetMechanics>();
    }

    void Awake()
    {
        if (jet == null)
        {
            jet = GetComponent<JetMechanics>();
        }
    }

    void FixedUpdate()
    {
        if (jet == null || jet.Rigidbody == null)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;

        Vector3 currentTargetPosition = target != null ? target.position : transform.position + transform.forward * 1000f;
        EnqueueTargetPosition(currentTargetPosition);

        Vector3 steering;
        float throttle;

        if (useGroundAvoidance && IsGroundDanger())
        {
            steering = AvoidGround(dt);
            throttle = groundAvoidThrottle;
        }
        else if (useSpeedRecovery && jet.LocalVelocity.z < recoverBelowSpeed)
        {
            steering = RecoverSpeed(dt);
            throttle = recoverSpeedThrottle;
        }
        else
        {
            Vector3 delayedTargetPosition = GetDelayedTargetPosition(currentTargetPosition);
            steering = CalculateSteering(dt, delayedTargetPosition);
            throttle = CalculateThrottle(minSpeed, maxSpeed);
        }

        jet.SetControlInput(steering);
        jet.SetThrottleInput(throttle);
    }

    void EnqueueTargetPosition(Vector3 position)
    {
        inputQueue.Enqueue(new DelayedTargetPoint(position, Time.time));

        while (inputQueue.Count > 100)
        {
            inputQueue.Dequeue();
        }
    }

    Vector3 GetDelayedTargetPosition(Vector3 fallbackPosition)
    {
        if (!useReactionDelay || inputQueue.Count == 0 || jet == null || jet.Rigidbody == null)
        {
            return fallbackPosition;
        }

        float distance = Vector3.Distance(jet.Rigidbody.position, fallbackPosition);
        float t = Mathf.InverseLerp(reactionDistanceMin, reactionDistanceMax, distance);
        float desiredDelay = Mathf.Lerp(reactionDelayMin, reactionDelayMax, t);
        float cutoffTime = Time.time - desiredDelay;

        DelayedTargetPoint selected = inputQueue.Peek();

        foreach (DelayedTargetPoint point in inputQueue)
        {
            if (point.time <= cutoffTime)
            {
                selected = point;
            }
            else
            {
                break;
            }
        }

        while (inputQueue.Count > 1 && inputQueue.Peek().time < cutoffTime - 1f)
        {
            inputQueue.Dequeue();
        }

        return selected.position;
    }

    Vector3 CalculateSteering(float dt, Vector3 targetPosition)
    {
        Vector3 error = targetPosition - jet.Rigidbody.position;
        error = Quaternion.Inverse(jet.Rigidbody.rotation) * error;

        if (error.sqrMagnitude < 0.0001f)
        {
            return Vector3.zero;
        }

        Vector3 errorDir = error.normalized;
        Vector3 pitchError = new Vector3(0f, error.y, error.z);
        Vector3 rollError = new Vector3(error.x, error.y, 0f);
        Vector3 yawError = new Vector3(error.x, 0f, error.z);

        Vector3 targetInput = Vector3.zero;

        float pitch = Vector3.SignedAngle(Vector3.forward, pitchError.normalized, Vector3.right);
        targetInput.x = Mathf.Clamp(pitch / 90f, -1f, 1f);

        if (Vector3.Angle(Vector3.forward, errorDir) < fineSteeringAngle)
        {
            float yaw = Vector3.SignedAngle(Vector3.forward, yawError.normalized, Vector3.up);
            targetInput.y = Mathf.Clamp((yaw / 90f) * yawFactor, -1f, 1f);
            targetInput.z = 0f;
        }
        else
        {
            float roll = Vector3.SignedAngle(Vector3.up, rollError.normalized, Vector3.forward);
            targetInput.z = Mathf.Clamp((roll / 90f) * rollFactor, -1f, 1f);
            targetInput.y = 0f;
        }

        Vector3 input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
        lastInput = input;
        return input;
    }

    Vector3 AvoidGround(float dt)
    {
        float pitchInput = 0f;
        float yawInput = 0f;
        float rollInput = 0f;

        float altitudeCloseness = 0f;
        float criticalCloseness = 0f;

        if (hasAltitudeHit)
        {
            altitudeCloseness = 1f - Mathf.Clamp01(altitudeHit.distance / Mathf.Max(0.001f, minimumSafeAltitude));
            criticalCloseness = 1f - Mathf.Clamp01(altitudeHit.distance / Mathf.Max(0.001f, criticalAltitude));
        }

        if (hasAltitudeHit && altitudeHit.distance < minimumSafeAltitude)
        {
            pitchInput = Mathf.Lerp(0f, altitudeRecoveryPitch, altitudeCloseness);
        }

        float bankLevelInput = Mathf.Clamp(-transform.right.y * wingsLevelRollStrength, -1f, 1f);

        if (hasForwardHit)
        {
            Vector3 localHitPoint = transform.InverseTransformPoint(forwardHit.point);
            float forwardCloseness = 1f - Mathf.Clamp01(forwardHit.distance / Mathf.Max(0.001f, forwardCollisionCheckDistance));

            float collisionPitch = Mathf.Lerp(0f, collisionRecoveryPitch, forwardCloseness);
            float collisionRoll = Mathf.Clamp((-localHitPoint.x / Mathf.Max(0.001f, localHitXForFullRoll)) * groundAvoidanceRollStrength, -1f, 1f);
            float collisionYaw = Mathf.Clamp((-localHitPoint.x / Mathf.Max(0.001f, localHitXForFullYaw)) * groundAvoidanceYawStrength, -1f, 1f);

            if (Mathf.Abs(collisionPitch) > Mathf.Abs(pitchInput))
            {
                pitchInput = collisionPitch;
            }

            rollInput = collisionRoll;
            yawInput = collisionYaw;
        }

        if (hasAltitudeHit && altitudeHit.distance <= criticalAltitude)
        {
            rollInput = Mathf.Lerp(rollInput, bankLevelInput, Mathf.Clamp01(lowAltitudeRollSuppression + criticalCloseness));
            yawInput = Mathf.Lerp(yawInput, 0f, Mathf.Clamp01(lowAltitudeYawSuppression + criticalCloseness));
        }

        Vector3 targetInput = new Vector3(pitchInput, yawInput, rollInput);
        Vector3 input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
        lastInput = input;
        return input;
    }

    Vector3 RecoverSpeed(float dt)
    {
        float pitchLevel = Mathf.Clamp(-jet.LocalVelocity.y * recoveryPitchStrength, -1f, 1f);
        float rollLevel = Mathf.Clamp(-jet.LocalAngularVelocity.z * recoveryRollStrength, -1f, 1f);
        Vector3 targetInput = new Vector3(pitchLevel, 0f, rollLevel);
        Vector3 input = Vector3.MoveTowards(lastInput, targetInput, steeringSpeed * dt);
        lastInput = input;
        return input;
    }

    float CalculateThrottle(float minimumSpeed, float maximumSpeed)
    {
        float forwardSpeed = jet.LocalVelocity.z;

        if (forwardSpeed < minimumSpeed)
        {
            return 1f;
        }

        if (forwardSpeed > maximumSpeed)
        {
            return -1f;
        }

        return 0f;
    }

    bool IsGroundDanger()
    {
        Vector3 origin = jet.Rigidbody.position;

        hasAltitudeHit = Physics.Raycast(
            origin,
            Vector3.down,
            out altitudeHit,
            minimumSafeAltitude,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        hasForwardHit = Physics.Raycast(
            origin,
            transform.forward,
            out forwardHit,
            forwardCollisionCheckDistance,
            groundMask,
            QueryTriggerInteraction.Ignore
        );

        return hasAltitudeHit || hasForwardHit;
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }

    public void ClearTarget()
    {
        target = null;
    }

    void OnDrawGizmosSelected()
    {
        if (jet == null)
        {
            jet = GetComponent<JetMechanics>();
        }

        Gizmos.color = Color.cyan;

        if (target != null)
        {
            Gizmos.DrawLine(transform.position, target.position);
            Gizmos.DrawSphere(target.position, 10f);
        }

        if (jet == null || jet.Rigidbody == null)
        {
            return;
        }

        Vector3 origin = jet.Rigidbody.position;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + Vector3.down * minimumSafeAltitude);

        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawLine(origin, origin + Vector3.down * criticalAltitude);

        Gizmos.color = Color.green;
        Gizmos.DrawLine(origin, origin + transform.forward * forwardCollisionCheckDistance);

        if (hasAltitudeHit)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(altitudeHit.point, 5f);
        }

        if (hasForwardHit)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawSphere(forwardHit.point, 5f);
        }
    }
}