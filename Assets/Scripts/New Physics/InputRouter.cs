using UnityEngine;
using UnityEngine.InputSystem;

public class InputRouter : MonoBehaviour
{
    [SerializeField] InputActionAsset flightControls;
    [SerializeField] string actionMapName = "PhysicalControls";

    InputActionMap actionMap;

    InputAction leftEnginesAction;
    InputAction rightEnginesAction;
    InputAction thrustVectorLeftAction;
    InputAction thrustVectorRightAction;
    InputAction rollAction;
    InputAction pitchAction;
    InputAction yawAction;
    InputAction aioThrustAction;
    InputAction aioThrustVectorAction;
    InputAction switchRadarModeOffAction;
    InputAction switchRadarModeTWSAction;
    InputAction switchRadarModeHMDAction;
    InputAction lockRadarAction;
    InputAction nextRadarTargetAction;
    InputAction previousRadarTargetAction;
    InputAction releaseWeaponAction;
    InputAction nextPylonAction;
    InputAction previousPylonAction;
    InputAction radarGimbleXAction;
    InputAction radarGimbleYAction;
    InputAction gearUpAction;
    InputAction gearDownAction;
    InputAction flapsUpAction;
    InputAction flapsDownAction;
    InputAction gOverrideAction;
    InputAction shootGunAction;
    InputAction airBrakeUpAction;
    InputAction airBrakeDownAction;

    public float LeftEngines;
    public float RightEngines;
    public float ThrustVectorLeft;
    public float ThrustVectorRight;
    public float Roll;
    public float Pitch;
    public float Yaw;
    public float AIOThrust;
    public float AIOThrustVector;
    public float SwitchRadarModeOff;
    public bool SwitchRadarModeTWS;
    public bool SwitchRadarModeHMD;
    public bool LockRadar;
    public bool NextRadarTarget;
    public bool PreviousRadarTarget;
    public bool ReleaseWeapon;
    public bool NextPylon;
    public bool PreviousPylon;
    public float RadarGimbleX;
    public float RadarGimbleY;
    public bool GearUp;
    public bool GearDown;
    public bool FlapsUp;
    public bool FlapsDown;
    public bool GOverride;
    public bool ShootGun;
    public bool AirBrakeUp;
    public bool AirBrakeDown;

    void Awake()
    {
        SetupActions();
    }

    void OnEnable()
    {
        if (actionMap == null)
        {
            SetupActions();
        }

        if (actionMap != null)
        {
            actionMap.Enable();
        }
    }

    void OnDisable()
    {
        if (actionMap != null)
        {
            actionMap.Disable();
        }

        ResetValues();
    }

    void SetupActions()
    {
        if (flightControls == null)
        {
            return;
        }

        actionMap = flightControls.FindActionMap(actionMapName, false);
        if (actionMap == null)
        {
            return;
        }

        leftEnginesAction = actionMap.FindAction("LeftEngines", false);
        rightEnginesAction = actionMap.FindAction("RightEngines", false);
        thrustVectorLeftAction = actionMap.FindAction("ThrustVectorLeft", false);
        thrustVectorRightAction = actionMap.FindAction("ThrustVectorRight", false);
        rollAction = actionMap.FindAction("Roll", false);
        pitchAction = actionMap.FindAction("Pitch", false);
        yawAction = actionMap.FindAction("Yaw", false);
        aioThrustAction = actionMap.FindAction("AIOThrust", false);
        aioThrustVectorAction = actionMap.FindAction("AIOThrustVector", false);
        switchRadarModeOffAction = actionMap.FindAction("SwitchRadarModeOff", false);
        switchRadarModeTWSAction = actionMap.FindAction("SwitchRadarModeTWS", false);
        switchRadarModeHMDAction = actionMap.FindAction("SwitchRadarModeHMD", false);
        lockRadarAction = actionMap.FindAction("LockRadar", false);
        nextRadarTargetAction = actionMap.FindAction("NextRadarTarget", false);
        previousRadarTargetAction = actionMap.FindAction("PreviousRadarTarget", false);
        releaseWeaponAction = actionMap.FindAction("ReleaseWeapon", false);
        nextPylonAction = actionMap.FindAction("NextPylon", false);
        previousPylonAction = actionMap.FindAction("PreviousPylon", false);
        radarGimbleXAction = actionMap.FindAction("RadarGimbleX", false);
        radarGimbleYAction = actionMap.FindAction("RadarGimbleY", false);
        gearUpAction = actionMap.FindAction("GearUp", false);
        gearDownAction = actionMap.FindAction("GearDown", false);
        flapsUpAction = actionMap.FindAction("FlapsUp", false);
        flapsDownAction = actionMap.FindAction("FlapsDown", false);
        gOverrideAction = actionMap.FindAction("GOverride", false);
        shootGunAction = actionMap.FindAction("ShootGun", false);
        airBrakeUpAction = actionMap.FindAction("AirBrakeUp", false);
        airBrakeDownAction = actionMap.FindAction("AirBrakeDown", false);
    }

    void Update()
    {
        LeftEngines = ReadFloat(leftEnginesAction);
        RightEngines = ReadFloat(rightEnginesAction);
        ThrustVectorLeft = ReadFloat(thrustVectorLeftAction);
        ThrustVectorRight = ReadFloat(thrustVectorRightAction);
        Roll = ReadFloat(rollAction);
        Pitch = ReadFloat(pitchAction);
        Yaw = ReadFloat(yawAction);
        AIOThrust = ReadFloat(aioThrustAction);
        AIOThrustVector = ReadFloat(aioThrustVectorAction);
        SwitchRadarModeOff = ReadFloat(switchRadarModeOffAction);
        SwitchRadarModeTWS = ReadButton(switchRadarModeTWSAction);
        SwitchRadarModeHMD = ReadButton(switchRadarModeHMDAction);
        LockRadar = ReadButton(lockRadarAction);
        NextRadarTarget = ReadButton(nextRadarTargetAction);
        PreviousRadarTarget = ReadButton(previousRadarTargetAction);
        ReleaseWeapon = ReadButton(releaseWeaponAction);
        NextPylon = ReadButton(nextPylonAction);
        PreviousPylon = ReadButton(previousPylonAction);
        RadarGimbleX = ReadFloat(radarGimbleXAction);
        RadarGimbleY = ReadFloat(radarGimbleYAction);
        GearUp = ReadButton(gearUpAction);
        GearDown = ReadButton(gearDownAction);
        FlapsUp = ReadButton(flapsUpAction);
        FlapsDown = ReadButton(flapsDownAction);
        GOverride = ReadButton(gOverrideAction);
        ShootGun = ReadButton(shootGunAction);
        AirBrakeUp = ReadButton(airBrakeUpAction);
        AirBrakeDown = ReadButton(airBrakeDownAction);
    }

    float ReadFloat(InputAction action)
    {
        if (action == null)
        {
            return 0f;
        }

        return Mathf.Clamp(action.ReadValue<float>(), -1f, 1f);
    }

    bool ReadButton(InputAction action)
    {
        if (action == null)
        {
            return false;
        }

        return action.IsPressed();
    }

    void ResetValues()
    {
        LeftEngines = 0f;
        RightEngines = 0f;
        ThrustVectorLeft = 0f;
        ThrustVectorRight = 0f;
        Roll = 0f;
        Pitch = 0f;
        Yaw = 0f;
        AIOThrust = 0f;
        AIOThrustVector = 0f;
        SwitchRadarModeOff = 0f;
        SwitchRadarModeTWS = false;
        SwitchRadarModeHMD = false;
        LockRadar = false;
        NextRadarTarget = false;
        PreviousRadarTarget = false;
        ReleaseWeapon = false;
        NextPylon = false;
        PreviousPylon = false;
        RadarGimbleX = 0f;
        RadarGimbleY = 0f;
        GearUp = false;
        GearDown = false;
        FlapsUp = false;
        FlapsDown = false;
        GOverride = false;
        ShootGun = false;
        AirBrakeUp = false;
        AirBrakeDown = false;
    }
}