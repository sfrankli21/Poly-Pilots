using System.Collections;
using UnityEngine;

public class JetMechanics : MonoBehaviour
{
    public enum RotationAxis
    {
        X,
        Y,
        Z
    }

    [System.Serializable]
    public class SurfaceElement
    {
        public Transform targetTransform;
        public RotationAxis rotationAxis;
        public float minAngleValue;
        public float maxAngleValue;
        public float rotationSpeed;
        [HideInInspector] public float currentAngle;
    }

    [System.Serializable]
    public class JoystickVisualElement
    {
        public Transform targetTransform;
        public float minXAngleValue;
        public float maxXAngleValue;
        public float xRotationSpeed;
        public float minZAngleValue;
        public float maxZAngleValue;
        public float zRotationSpeed;
        [HideInInspector] public float currentXAngle;
        [HideInInspector] public float currentZAngle;
    }

    [System.Serializable]
    public class ThrottleVisualElement
    {
        public Transform targetTransform;
        public RotationAxis rotationAxis;
        public float minAngleValue;
        public float maxAngleValue;
        public float rotationSpeed;
        [HideInInspector] public float currentAngle;
    }

    [System.Serializable]
    public class RudderPedalVisualElement
    {
        public Transform targetTransform;
        public Vector3 minLocalPosition;
        public Vector3 maxLocalPosition;
        public float moveSpeed;
        [HideInInspector] public Vector3 currentLocalPosition;
    }

    [System.Serializable]
    public class LandingGearElement
    {
        public string gearUpAnimationName;
        public float playGearUpDelay;
        public string gearDownAnimationName;
        public float playGearDownDelay;
        public Collider gearCollider;
    }

    [SerializeField]
    InputRouter inputRouter;
    [SerializeField]
    float gLimit;
    [SerializeField]
    float gLimitPitch;

    [Header("Thrust")]
    [SerializeField]
    float throttleSpeed;
    [SerializeField]
    float leftMaxThrust;
    [SerializeField]
    float rightMaxThrust;
    [SerializeField]
    Transform leftEngine;
    [SerializeField]
    Transform rightEngine;

    [Header("Lift")]
    [SerializeField]
    float liftPower;
    [SerializeField]
    AnimationCurve liftAOACurve;
    [SerializeField]
    float inducedDrag;
    [SerializeField]
    AnimationCurve inducedDragCurve;
    [SerializeField]
    float rudderPower;
    [SerializeField]
    AnimationCurve rudderAOACurve;
    [SerializeField]
    AnimationCurve rudderInducedDragCurve;
    [SerializeField]
    float flapsLiftPower;
    [SerializeField]
    float flapsAOABias;
    [SerializeField]
    float flapsDrag;

    [Header("Steering")]
    [SerializeField]
    Vector3 turnSpeed;
    [SerializeField]
    Vector3 turnAcceleration;
    [SerializeField]
    AnimationCurve steeringCurve;

    [Header("Drag")]
    [SerializeField]
    AnimationCurve dragForward;
    [SerializeField]
    AnimationCurve dragBack;
    [SerializeField]
    AnimationCurve dragLeft;
    [SerializeField]
    AnimationCurve dragRight;
    [SerializeField]
    AnimationCurve dragTop;
    [SerializeField]
    AnimationCurve dragBottom;
    [SerializeField]
    Vector3 angularDrag;
    [SerializeField]
    float airbrakeDrag;

    [Header("Misc")]
    [SerializeField]
    LandingGearElement[] landingGear;
    [SerializeField]
    PhysicsMaterial landingGearBrakesMaterial;
    [SerializeField]
    bool flapsDeployed;
    [SerializeField]
    bool gearDeployed;
    [SerializeField]
    float initialSpeed;

    [Header("Control Surfaces")]
    [SerializeField]
    SurfaceElement[] pitchSurfaces;
    [SerializeField]
    SurfaceElement[] rollSurfaces;
    [SerializeField]
    SurfaceElement[] yawSurfaces;
    [SerializeField]
    SurfaceElement[] airbrakeSurfaces;
    [SerializeField]
    SurfaceElement[] flapsSurfaces;

    [Header("Visuals")]
    [SerializeField]
    JoystickVisualElement[] joystickVisual;
    [SerializeField]
    ThrottleVisualElement[] leftThrottleVisual;
    [SerializeField]
    ThrottleVisualElement[] rightThrottleVisual;
    [SerializeField]
    RudderPedalVisualElement[] rudderPedalVisual;

    float leftThrottleInput;
    float rightThrottleInput;
    Vector3 controlInput;
    Vector3 lastVelocity;
    PhysicsMaterial landingGearDefaultMaterial;
    Coroutine[] landingGearAnimationCoroutines;

    public Rigidbody Rigidbody { get; private set; }
    public float Throttle { get; private set; }
    public float LeftThrottle { get; private set; }
    public float RightThrottle { get; private set; }
    public Vector3 EffectiveInput { get; private set; }
    public Vector3 Velocity { get; private set; }
    public Vector3 LocalVelocity { get; private set; }
    public Vector3 LocalGForce { get; private set; }
    public Vector3 LocalAngularVelocity { get; private set; }
    public float AngleOfAttack { get; private set; }
    public float AngleOfAttackYaw { get; private set; }
    public bool AirbrakeDeployed { get; private set; }

    public bool FlapsDeployed
    {
        get
        {
            return flapsDeployed;
        }
        private set
        {
            flapsDeployed = value;
        }
    }

    public bool GearDeployed
    {
        get
        {
            return gearDeployed;
        }
        private set
        {
            if (gearDeployed == value)
            {
                return;
            }

            gearDeployed = value;
            PlayLandingGearAnimations(value);
        }
    }

    void Start()
    {
        Rigidbody = GetComponent<Rigidbody>();

        if (landingGear != null)
        {
            landingGearAnimationCoroutines = new Coroutine[landingGear.Length];
        }

        if (landingGear != null && landingGear.Length > 0 && landingGear[0] != null && landingGear[0].gearCollider != null)
        {
            landingGearDefaultMaterial = landingGear[0].gearCollider.sharedMaterial;
        }

        if (Rigidbody != null)
        {
            Rigidbody.linearVelocity = Rigidbody.rotation * new Vector3(0f, 0f, initialSpeed);
        }

        ApplyLandingGearStateImmediate(gearDeployed);

        InitializeSurfaceAngles(pitchSurfaces);
        InitializeSurfaceAngles(rollSurfaces);
        InitializeSurfaceAngles(yawSurfaces);
        InitializeSurfaceAngles(airbrakeSurfaces);
        InitializeSurfaceAngles(flapsSurfaces);

        InitializeJoystickVisuals(joystickVisual);
        InitializeThrottleVisuals(leftThrottleVisual);
        InitializeThrottleVisuals(rightThrottleVisual);
        InitializeRudderPedalVisuals(rudderPedalVisual);
    }

    void ApplyLandingGearStateImmediate(bool deployed)
    {
        gearDeployed = deployed;
    }

    void PlayLandingGearAnimations(bool deployed)
    {
        if (landingGear == null)
        {
            return;
        }

        for (int i = 0; i < landingGear.Length; i++)
        {
            if (landingGearAnimationCoroutines != null && i < landingGearAnimationCoroutines.Length && landingGearAnimationCoroutines[i] != null)
            {
                StopCoroutine(landingGearAnimationCoroutines[i]);
                landingGearAnimationCoroutines[i] = null;
            }

            landingGearAnimationCoroutines[i] = StartCoroutine(PlayLandingGearAnimationRoutine(i, deployed));
        }
    }

    IEnumerator PlayLandingGearAnimationRoutine(int index, bool deployed)
    {
        if (landingGear == null || index < 0 || index >= landingGear.Length)
        {
            yield break;
        }

        LandingGearElement gear = landingGear[index];

        if (gear == null || gear.gearCollider == null)
        {
            yield break;
        }

        float delay = deployed ? gear.playGearDownDelay : gear.playGearUpDelay;
        string animationName = deployed ? gear.gearDownAnimationName : gear.gearUpAnimationName;

        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (string.IsNullOrEmpty(animationName))
        {
            yield break;
        }

        Animator animator = gear.gearCollider.GetComponent<Animator>();

        if (animator == null)
        {
            animator = gear.gearCollider.GetComponentInParent<Animator>();
        }

        if (animator != null)
        {
            animator.Play(animationName, 0, 0f);
        }

        if (landingGearAnimationCoroutines != null && index < landingGearAnimationCoroutines.Length)
        {
            landingGearAnimationCoroutines[index] = null;
        }
    }

    void InitializeSurfaceAngles(SurfaceElement[] surfaces)
    {
        if (surfaces == null)
        {
            return;
        }

        for (int i = 0; i < surfaces.Length; i++)
        {
            if (surfaces[i] == null || surfaces[i].targetTransform == null)
            {
                continue;
            }

            Vector3 localEuler = surfaces[i].targetTransform.localEulerAngles;
            surfaces[i].currentAngle = GetAxisAngle(localEuler, surfaces[i].rotationAxis);
        }
    }

    void InitializeJoystickVisuals(JoystickVisualElement[] visuals)
    {
        if (visuals == null)
        {
            return;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null || visuals[i].targetTransform == null)
            {
                continue;
            }

            Vector3 localEuler = visuals[i].targetTransform.localEulerAngles;
            visuals[i].currentXAngle = localEuler.x;
            visuals[i].currentZAngle = localEuler.z;
        }
    }

    void InitializeThrottleVisuals(ThrottleVisualElement[] visuals)
    {
        if (visuals == null)
        {
            return;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null || visuals[i].targetTransform == null)
            {
                continue;
            }

            Vector3 localEuler = visuals[i].targetTransform.localEulerAngles;
            visuals[i].currentAngle = GetAxisAngle(localEuler, visuals[i].rotationAxis);
        }
    }

    void InitializeRudderPedalVisuals(RudderPedalVisualElement[] visuals)
    {
        if (visuals == null)
        {
            return;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            if (visuals[i] == null || visuals[i].targetTransform == null)
            {
                continue;
            }

            visuals[i].currentLocalPosition = visuals[i].targetTransform.localPosition;
        }
    }

    public void SetControlInput(Vector3 input)
    {
        controlInput = Vector3.ClampMagnitude(input, 1f);
    }

    public void SetThrottleInput(float input)
    {
        input = Mathf.Clamp(input, -1f, 1f);
        leftThrottleInput = input;
        rightThrottleInput = input;
    }

    public void SetThrottleInputs(float leftInput, float rightInput)
    {
        leftThrottleInput = Mathf.Clamp(leftInput, -1f, 1f);
        rightThrottleInput = Mathf.Clamp(rightInput, -1f, 1f);
    }

    public void SetLeftThrottleInput(float input)
    {
        leftThrottleInput = Mathf.Clamp(input, -1f, 1f);
    }

    public void SetRightThrottleInput(float input)
    {
        rightThrottleInput = Mathf.Clamp(input, -1f, 1f);
    }

    public void SetAirbrakeDeployed(bool value)
    {
        AirbrakeDeployed = value;
    }

    public void SetFlapsDeployed(bool value)
    {
        FlapsDeployed = value;
    }

    void ReadRouterInputs()
    {
        if (inputRouter == null)
        {
            return;
        }

        controlInput = Vector3.ClampMagnitude(new Vector3(inputRouter.Pitch, inputRouter.Yaw, -inputRouter.Roll), 1f);
        leftThrottleInput = Mathf.Clamp(inputRouter.LeftEngines, -1f, 1f);
        rightThrottleInput = Mathf.Clamp(inputRouter.RightEngines, -1f, 1f);

        if (inputRouter.FlapsUp)
        {
            FlapsDeployed = true;
        }
        else if (inputRouter.FlapsDown)
        {
            FlapsDeployed = false;
        }

        if (inputRouter.GearUp)
        {
            GearDeployed = true;
        }
        else if (inputRouter.GearDown)
        {
            GearDeployed = false;
        }
    }

    void UpdateThrottle(float dt)
    {
        float leftTarget = leftThrottleInput > 0f ? 1f : 0f;
        float rightTarget = rightThrottleInput > 0f ? 1f : 0f;

        LeftThrottle = Mathf.MoveTowards(LeftThrottle, leftTarget, throttleSpeed * Mathf.Abs(leftThrottleInput) * dt);
        RightThrottle = Mathf.MoveTowards(RightThrottle, rightTarget, throttleSpeed * Mathf.Abs(rightThrottleInput) * dt);
        Throttle = (LeftThrottle + RightThrottle) * 0.5f;

        if (landingGear == null)
        {
            return;
        }

        for (int i = 0; i < landingGear.Length; i++)
        {
            if (landingGear[i] == null || landingGear[i].gearCollider == null)
            {
                continue;
            }

            landingGear[i].gearCollider.sharedMaterial = AirbrakeDeployed ? landingGearBrakesMaterial : landingGearDefaultMaterial;
        }
    }

    void UpdateThrust()
    {
        if (leftEngine != null)
        {
            Rigidbody.AddForceAtPosition(leftEngine.forward * (LeftThrottle * leftMaxThrust), leftEngine.position);
        }
        else
        {
            Rigidbody.AddRelativeForce(Vector3.forward * (LeftThrottle * leftMaxThrust));
        }

        if (rightEngine != null)
        {
            Rigidbody.AddForceAtPosition(rightEngine.forward * (RightThrottle * rightMaxThrust), rightEngine.position);
        }
        else
        {
            Rigidbody.AddRelativeForce(Vector3.forward * (RightThrottle * rightMaxThrust));
        }
    }

    public void ToggleFlaps()
    {
        FlapsDeployed = !FlapsDeployed;
    }

    void CalculateAngleOfAttack()
    {
        if (LocalVelocity.sqrMagnitude < 0.1f)
        {
            AngleOfAttack = 0f;
            AngleOfAttackYaw = 0f;
            return;
        }

        AngleOfAttack = Mathf.Atan2(-LocalVelocity.y, LocalVelocity.z);
        AngleOfAttackYaw = Mathf.Atan2(LocalVelocity.x, LocalVelocity.z);
    }

    void CalculateGForce(float dt)
    {
        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        var acceleration = (Velocity - lastVelocity) / dt;
        LocalGForce = invRotation * acceleration;
        lastVelocity = Velocity;
    }

    void CalculateState()
    {
        var invRotation = Quaternion.Inverse(Rigidbody.rotation);
        Velocity = Rigidbody.linearVelocity;
        LocalVelocity = invRotation * Velocity;
        LocalAngularVelocity = invRotation * Rigidbody.angularVelocity;
        CalculateAngleOfAttack();
    }

    void UpdateDrag()
    {
        var lv = LocalVelocity;
        var lv2 = lv.sqrMagnitude;

        float extraAirbrakeDrag = AirbrakeDeployed ? airbrakeDrag : 0f;
        float extraFlapsDrag = FlapsDeployed ? flapsDrag : 0f;

        var coefficient = Scale6(
            lv.normalized,
            dragRight.Evaluate(Mathf.Abs(lv.x)),
            dragLeft.Evaluate(Mathf.Abs(lv.x)),
            dragTop.Evaluate(Mathf.Abs(lv.y)),
            dragBottom.Evaluate(Mathf.Abs(lv.y)),
            dragForward.Evaluate(Mathf.Abs(lv.z)) + extraAirbrakeDrag + extraFlapsDrag,
            dragBack.Evaluate(Mathf.Abs(lv.z))
        );

        var drag = coefficient.magnitude * lv2 * -lv.normalized;

        Rigidbody.AddRelativeForce(drag);
    }

    Vector3 CalculateLift(float angleOfAttack, Vector3 rightAxis, float liftPowerValue, AnimationCurve aoaCurve, AnimationCurve inducedDragCurveValue)
    {
        var liftVelocity = Vector3.ProjectOnPlane(LocalVelocity, rightAxis);
        var v2 = liftVelocity.sqrMagnitude;

        var liftCoefficient = aoaCurve.Evaluate(angleOfAttack * Mathf.Rad2Deg);
        var liftForce = v2 * liftCoefficient * liftPowerValue;

        var liftDirection = Vector3.Cross(liftVelocity.normalized, rightAxis);
        var lift = liftDirection * liftForce;

        var dragForce = liftCoefficient * liftCoefficient;
        var dragDirection = -liftVelocity.normalized;
        var inducedDragForce = dragDirection * v2 * dragForce * inducedDrag * inducedDragCurveValue.Evaluate(Mathf.Max(0f, LocalVelocity.z));

        return lift + inducedDragForce;
    }

    void UpdateLift()
    {
        if (LocalVelocity.sqrMagnitude < 1f)
        {
            return;
        }

        float currentFlapsLiftPower = FlapsDeployed ? flapsLiftPower : 0f;
        float currentFlapsAOABias = FlapsDeployed ? flapsAOABias : 0f;

        var liftForce = CalculateLift(
            AngleOfAttack + (currentFlapsAOABias * Mathf.Deg2Rad),
            Vector3.right,
            liftPower + currentFlapsLiftPower,
            liftAOACurve,
            inducedDragCurve
        );

        var yawForce = CalculateLift(
            AngleOfAttackYaw,
            Vector3.up,
            rudderPower,
            rudderAOACurve,
            rudderInducedDragCurve
        );

        Rigidbody.AddRelativeForce(liftForce);
        Rigidbody.AddRelativeForce(yawForce);
    }

    void UpdateAngularDrag()
    {
        var av = LocalAngularVelocity;
        var drag = av.sqrMagnitude * -av.normalized;
        Rigidbody.AddRelativeTorque(Vector3.Scale(drag, angularDrag), ForceMode.Acceleration);
    }

    Vector3 CalculateEstimatedGForce(Vector3 angularVelocity, Vector3 velocity)
    {
        return Vector3.Cross(angularVelocity, velocity);
    }

    Vector3 CalculateGForceLimit(Vector3 input)
    {
        return Scale6(
            input,
            gLimit,
            gLimitPitch,
            gLimit,
            gLimit,
            gLimit,
            gLimit
        ) * 9.81f;
    }

    float CalculateGLimiter(Vector3 input, Vector3 maxAngularVelocity)
    {
        if (input.magnitude < 0.01f)
        {
            return 1f;
        }

        var maxInput = input.normalized;

        var limit = CalculateGForceLimit(maxInput);
        var maxGForce = CalculateEstimatedGForce(Vector3.Scale(maxInput, maxAngularVelocity), LocalVelocity);

        if (maxGForce.magnitude > limit.magnitude)
        {
            return limit.magnitude / maxGForce.magnitude;
        }

        return 1f;
    }

    float CalculateSteering(float dt, float angularVelocity, float targetVelocity, float acceleration)
    {
        var error = targetVelocity - angularVelocity;
        var accel = acceleration * dt;
        return Mathf.Clamp(error, -accel, accel);
    }

    void UpdateSteering(float dt)
    {
        var speed = Mathf.Max(0f, LocalVelocity.z);
        var steeringPower = steeringCurve.Evaluate(speed);

        var gForceScaling = CalculateGLimiter(controlInput, turnSpeed * Mathf.Deg2Rad * steeringPower);

        var targetAV = Vector3.Scale(controlInput, turnSpeed * steeringPower * gForceScaling);
        var av = LocalAngularVelocity * Mathf.Rad2Deg;

        var correction = new Vector3(
            CalculateSteering(dt, av.x, targetAV.x, turnAcceleration.x * steeringPower),
            CalculateSteering(dt, av.y, targetAV.y, turnAcceleration.y * steeringPower),
            CalculateSteering(dt, av.z, targetAV.z, turnAcceleration.z * steeringPower)
        );

        Rigidbody.AddRelativeTorque(correction * Mathf.Deg2Rad, ForceMode.VelocityChange);

        var correctionInput = new Vector3(
            Mathf.Clamp((targetAV.x - av.x) / turnAcceleration.x, -1f, 1f),
            Mathf.Clamp((targetAV.y - av.y) / turnAcceleration.y, -1f, 1f),
            Mathf.Clamp((targetAV.z - av.z) / turnAcceleration.z, -1f, 1f)
        );

        var effectiveInput = (correctionInput + controlInput) * gForceScaling;

        EffectiveInput = new Vector3(
            Mathf.Clamp(effectiveInput.x, -1f, 1f),
            Mathf.Clamp(effectiveInput.y, -1f, 1f),
            Mathf.Clamp(effectiveInput.z, -1f, 1f)
        );
    }

    void UpdateSurfaceArray(SurfaceElement[] surfaces, float normalizedInput, float dt)
    {
        if (surfaces == null)
        {
            return;
        }

        for (int i = 0; i < surfaces.Length; i++)
        {
            SurfaceElement surface = surfaces[i];

            if (surface == null || surface.targetTransform == null)
            {
                continue;
            }

            float targetAngle = Mathf.Lerp(surface.minAngleValue, surface.maxAngleValue, (normalizedInput + 1f) * 0.5f);
            surface.currentAngle = Mathf.MoveTowards(surface.currentAngle, targetAngle, surface.rotationSpeed * dt);

            Vector3 localEuler = surface.targetTransform.localEulerAngles;
            SetAxisAngle(ref localEuler, surface.rotationAxis, surface.currentAngle);
            surface.targetTransform.localEulerAngles = localEuler;
        }
    }

    void UpdateJoystickVisuals(float dt)
    {
        if (joystickVisual == null)
        {
            return;
        }

        float pitchInput = Mathf.Clamp(controlInput.x, -1f, 1f);
        float rollInput = Mathf.Clamp(-controlInput.z, -1f, 1f);

        for (int i = 0; i < joystickVisual.Length; i++)
        {
            JoystickVisualElement visual = joystickVisual[i];

            if (visual == null || visual.targetTransform == null)
            {
                continue;
            }

            float targetX = Mathf.Lerp(visual.minXAngleValue, visual.maxXAngleValue, (pitchInput + 1f) * 0.5f);
            float targetZ = Mathf.Lerp(visual.minZAngleValue, visual.maxZAngleValue, (rollInput + 1f) * 0.5f);

            visual.currentXAngle = Mathf.MoveTowards(visual.currentXAngle, targetX, visual.xRotationSpeed * dt);
            visual.currentZAngle = Mathf.MoveTowards(visual.currentZAngle, targetZ, visual.zRotationSpeed * dt);

            Vector3 localEuler = visual.targetTransform.localEulerAngles;
            localEuler.x = visual.currentXAngle;
            localEuler.z = visual.currentZAngle;
            visual.targetTransform.localEulerAngles = localEuler;
        }
    }

    void UpdateThrottleVisuals(ThrottleVisualElement[] visuals, float normalizedInput, float dt)
    {
        if (visuals == null)
        {
            return;
        }

        for (int i = 0; i < visuals.Length; i++)
        {
            ThrottleVisualElement visual = visuals[i];

            if (visual == null || visual.targetTransform == null)
            {
                continue;
            }

            float targetAngle = Mathf.Lerp(visual.minAngleValue, visual.maxAngleValue, (normalizedInput + 1f) * 0.5f);
            visual.currentAngle = Mathf.MoveTowards(visual.currentAngle, targetAngle, visual.rotationSpeed * dt);

            Vector3 localEuler = visual.targetTransform.localEulerAngles;
            SetAxisAngle(ref localEuler, visual.rotationAxis, visual.currentAngle);
            visual.targetTransform.localEulerAngles = localEuler;
        }
    }

    void UpdateRudderPedalVisuals(float dt)
    {
        if (rudderPedalVisual == null)
        {
            return;
        }

        float yawInput = Mathf.Clamp(controlInput.y, -1f, 1f);

        for (int i = 0; i < rudderPedalVisual.Length; i++)
        {
            RudderPedalVisualElement visual = rudderPedalVisual[i];

            if (visual == null || visual.targetTransform == null)
            {
                continue;
            }

            Vector3 targetLocalPosition = Vector3.Lerp(visual.minLocalPosition, visual.maxLocalPosition, (yawInput + 1f) * 0.5f);
            visual.currentLocalPosition = Vector3.MoveTowards(visual.currentLocalPosition, targetLocalPosition, visual.moveSpeed * dt);
            visual.targetTransform.localPosition = visual.currentLocalPosition;
        }
    }

    void UpdateSurfaces(float dt)
    {
        UpdateSurfaceArray(pitchSurfaces, Mathf.Clamp(controlInput.x, -1f, 1f), dt);
        UpdateSurfaceArray(rollSurfaces, Mathf.Clamp(-controlInput.z, -1f, 1f), dt);
        UpdateSurfaceArray(yawSurfaces, Mathf.Clamp(controlInput.y, -1f, 1f), dt);
        UpdateSurfaceArray(airbrakeSurfaces, AirbrakeDeployed ? 1f : -1f, dt);
        UpdateSurfaceArray(flapsSurfaces, FlapsDeployed ? 1f : -1f, dt);

        UpdateJoystickVisuals(dt);
        UpdateThrottleVisuals(leftThrottleVisual, Mathf.Clamp(leftThrottleInput, -1f, 1f), dt);
        UpdateThrottleVisuals(rightThrottleVisual, Mathf.Clamp(rightThrottleInput, -1f, 1f), dt);
        UpdateRudderPedalVisuals(dt);
    }

    float GetAxisAngle(Vector3 euler, RotationAxis axis)
    {
        switch (axis)
        {
            case RotationAxis.X:
                return euler.x;
            case RotationAxis.Y:
                return euler.y;
            default:
                return euler.z;
        }
    }

    void SetAxisAngle(ref Vector3 euler, RotationAxis axis, float value)
    {
        switch (axis)
        {
            case RotationAxis.X:
                euler.x = value;
                break;
            case RotationAxis.Y:
                euler.y = value;
                break;
            case RotationAxis.Z:
                euler.z = value;
                break;
        }
    }

    Vector3 Scale6(Vector3 direction, float right, float left, float up, float down, float forward, float back)
    {
        Vector3 result = Vector3.zero;

        result += Vector3.right * (direction.x >= 0f ? direction.x * right : direction.x * left);
        result += Vector3.up * (direction.y >= 0f ? direction.y * up : direction.y * down);
        result += Vector3.forward * (direction.z >= 0f ? direction.z * forward : direction.z * back);

        return result;
    }

    void FixedUpdate()
    {
        if (Rigidbody == null)
        {
            return;
        }

        float dt = Time.fixedDeltaTime;

        ReadRouterInputs();
        CalculateState();
        CalculateGForce(dt);
        UpdateThrottle(dt);
        UpdateThrust();
        UpdateLift();
        UpdateSteering(dt);
        UpdateDrag();
        UpdateAngularDrag();
        UpdateSurfaces(dt);
        CalculateState();
    }
}