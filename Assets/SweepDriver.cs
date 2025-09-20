using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using AerodynamicObjects.Flow; // UniformFlow

public class SweepDriverOneShot : MonoBehaviour
{
    [Header("References")]
    public Transform gimbal;               // rotates during sweep
    public UniformFlow uniformFlow;        // your UniformFlow component
    public Transform flowFacingReference;  // test part; its +Z is forward (nose)
    public Rigidbody testRigidbody;        // optional: zero velocities each step

    public enum AxisMode { PitchAoA, YawBeta }

    [Header("Sweep Mode")]
    public AxisMode axis = AxisMode.PitchAoA;

    [Header("Angles (deg)")]
    public float startAngle = -10f;
    public float endAngle = 30f;
    public float stepAngle = 2f;

    [Header("Speeds (knots)")]
    public List<float> speedsKnots = new List<float> { 120f, 200f, 300f, 500f, 800f, 1000f, 1300f };

    [Header("Timing")]
    public float settleSecondsOnSpeedChange = 0.2f;  // wait after changing speed
    public float dwellSecondsPerAngle = 0.1f;        // wait at each angle

    [Header("Flow Settings")]
    public bool disableTurbulenceDuringTests = true;
    public float strengthScale = 1f;                 // FlowPrimitive multiplier if available

    [Header("Fast Mode")]
    public bool fastMode = true;
    public float sweepTimeScale = 6f;                // Time.timeScale during sweep
    public float sweepFixedDelta = 0.01f;            // physics step during sweep (seconds)

    [Header("Continuous Sweep")]
    public bool continuousSweep = false;             // smooth rotation instead of step+dwell
    public float sweepRateDegPerSec = 60f;           // deg/sec when continuousSweep is true

    const float KTS2MS = 0.514444f;
    bool running;
    float origTimeScale, origFixedDelta;

    // cached reflection into windVelocity (DirectionalVelocity)
    FieldInfo fWind, fSpeed, fAz, fEl;

    void Awake()
    {
        origTimeScale = Time.timeScale;
        origFixedDelta = Time.fixedDeltaTime;
    }

    void Start()
    {
        if (!running) StartCoroutine(RunOnce());
    }

    IEnumerator RunOnce()
    {
        running = true;

        if (gimbal == null || uniformFlow == null)
        {
            Debug.LogError("[SweepDriverOneShot] Assign gimbal and uniformFlow.");
            StopAndRestore();
            yield break;
        }

        if (disableTurbulenceDuringTests) uniformFlow.enableTurbulence = false;
        TrySetStrengthScale(uniformFlow, strengthScale);

        if (!CacheDV(uniformFlow))
        {
            Debug.LogError("[SweepDriverOneShot] Could not access windVelocity.speed/azimuth/elevation.");
            StopAndRestore();
            yield break;
        }

        if (fastMode)
        {
            Time.timeScale = Mathf.Max(0.1f, sweepTimeScale);
            Time.fixedDeltaTime = Mathf.Max(0.001f, sweepFixedDelta);
            settleSecondsOnSpeedChange = Mathf.Min(settleSecondsOnSpeedChange, 0.2f);
            if (!continuousSweep) dwellSecondsPerAngle = Mathf.Min(dwellSecondsPerAngle, 0.1f);
        }

        float span = endAngle - startAngle;
        float step = Mathf.Abs(stepAngle) > 1e-6f ? Mathf.Sign(span) * Mathf.Abs(stepAngle) : (span >= 0 ? 1f : -1f);

        foreach (var kts in speedsKnots)
        {
            Vector3 dirWorld = (flowFacingReference ? -flowFacingReference.forward : Vector3.back).normalized;
            float speedMps = kts * KTS2MS;

            SetUniformFlow_ByAzEl(uniformFlow, dirWorld, speedMps);

            ResetBodyState();
            yield return new WaitForSeconds(settleSecondsOnSpeedChange);

            if (continuousSweep)
            {
                if (step > 0f)
                {
                    float a = startAngle;
                    while (a <= endAngle)
                    {
                        a += Mathf.Abs(sweepRateDegPerSec) * Time.fixedDeltaTime;
                        ApplyAngle(a);
                        ResetBodyState();
                        yield return new WaitForFixedUpdate();
                    }
                }
                else
                {
                    float a = startAngle;
                    while (a >= endAngle)
                    {
                        a -= Mathf.Abs(sweepRateDegPerSec) * Time.fixedDeltaTime;
                        ApplyAngle(a);
                        ResetBodyState();
                        yield return new WaitForFixedUpdate();
                    }
                }
            }
            else
            {
                if (step > 0f)
                {
                    for (float a = startAngle; a <= endAngle + 1e-4f; a += step)
                    {
                        ApplyAngle(a);
                        ResetBodyState();
                        yield return Dwell(dwellSecondsPerAngle);
                    }
                }
                else
                {
                    for (float a = startAngle; a >= endAngle - 1e-4f; a += step)
                    {
                        ApplyAngle(a);
                        ResetBodyState();
                        yield return Dwell(dwellSecondsPerAngle);
                    }
                }
            }
        }

        Debug.Log("[SweepDriverOneShot] Sweep complete.");
        StopAndRestore();
        enabled = false; // one shot
    }

    void ApplyAngle(float angleDeg)
    {
        var e = gimbal.localEulerAngles;
        if (axis == AxisMode.PitchAoA) e.x = angleDeg;
        else e.y = angleDeg;
        gimbal.localRotation = Quaternion.Euler(e);
    }

    IEnumerator Dwell(float seconds)
    {
        float t = 0f;
        var wfe = new WaitForFixedUpdate();
        while (t < seconds)
        {
            yield return wfe;
            t += Time.fixedDeltaTime;
        }
    }

    void ResetBodyState()
    {
        if (!testRigidbody) return;
        testRigidbody.linearVelocity = Vector3.zero;
        testRigidbody.angularVelocity = Vector3.zero;
        Physics.SyncTransforms();
        testRigidbody.Sleep();
    }

    void StopAndRestore()
    {
        Time.timeScale = origTimeScale;
        Time.fixedDeltaTime = origFixedDelta;
        running = false;
    }

    // ===== UniformFlow helpers (DirectionalVelocity with speed/azimuth/elevation in DEGREES) =====

    bool CacheDV(UniformFlow flow)
    {
        var T = flow.GetType();
        fWind = T.GetField("windVelocity", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (fWind == null) return false;

        object dv = fWind.GetValue(flow);
        if (dv == null) return false;

        var DV = dv.GetType();
        fSpeed = DV.GetField("speed", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        fAz = DV.GetField("azimuth", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        fEl = DV.GetField("elevation", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        return (fSpeed != null && fAz != null && fEl != null);
    }

    void SetUniformFlow_ByAzEl(UniformFlow flow, Vector3 dirWorld, float speedMps)
    {
        object dv = fWind.GetValue(flow);
        if (dv == null) return;

        dirWorld.Normalize();

        // Azimuth: around +Y from +Z toward +X (degrees)
        float azDeg = Mathf.Atan2(dirWorld.x, dirWorld.z) * Mathf.Rad2Deg;
        // Elevation: angle above horizon (degrees)
        float elDeg = Mathf.Atan2(dirWorld.y, Mathf.Sqrt(dirWorld.x * dirWorld.x + dirWorld.z * dirWorld.z)) * Mathf.Rad2Deg;

        var boxed = dv;                  // boxed struct/class
        fSpeed.SetValue(boxed, speedMps);  // m/s
        fAz.SetValue(boxed, azDeg);        // degrees
        fEl.SetValue(boxed, elDeg);        // degrees
        fWind.SetValue(flow, boxed);       // write back (important if struct)
    }

    static void TrySetStrengthScale(UniformFlow flow, float value)
    {
        if (flow == null) return;
        var baseType = typeof(UniformFlow).BaseType; // FlowPrimitive
        if (baseType == null) return;

        var p = baseType.GetProperty("strengthScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (p != null && p.PropertyType == typeof(float)) { p.SetValue(flow, value, null); return; }

        var f = baseType.GetField("strengthScale", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (f != null && f.FieldType == typeof(float)) { f.SetValue(flow, value); }
    }
}
