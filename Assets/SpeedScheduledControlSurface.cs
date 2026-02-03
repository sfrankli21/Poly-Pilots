using UnityEngine;
using AerodynamicObjects;

public class SpeedScheduledControlSurface : MonoBehaviour
{
    [SerializeField] FlowSensor flowSensor;
    [SerializeField] ControlSurface controlSurface;
    [SerializeField] AnimationCurve speedToDeflection; // x = speed, y = deflection (deg or rad)
    [SerializeField] bool inputIsDegrees = true;

    void Awake()
    {
        if (flowSensor == null)
            flowSensor = GetComponent<FlowSensor>();
        if (controlSurface == null)
            controlSurface = GetComponent<ControlSurface>();
    }

    void FixedUpdate()
    {
        if (flowSensor == null || controlSurface == null)
            return;

        float forwardAirspeed = flowSensor.localRelativeVelocity.z;
        float deflectionFromCurve = speedToDeflection.Evaluate(forwardAirspeed);

        if (inputIsDegrees)
            deflectionFromCurve = Mathf.Deg2Rad * deflectionFromCurve;

        controlSurface.deflectionAngle = deflectionFromCurve;
    }
}
