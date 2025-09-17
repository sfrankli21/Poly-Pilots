using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class AutoTrimAndSpeedLimiter : MonoBehaviour
{
    [Header("File I/O")]
    [Tooltip("Absolute or relative path to the FlightCurveData.csv that contains settings + a [Curve] section.")]
    public string flightCurveCsvPath = "";
    [Tooltip("If true, the script will try to load on Awake/OnValidate (Editor) and Start (Play).")]
    public bool autoLoadFlightCurveOnPlay = true;

    [Header("Refs")]
    public Rigidbody rb;
    public AerodynamicObjects.Tutorials.AircraftManager_Jet aircraft;

    [Header("Trim Limits")]
    public float trimMin = -1.30f;
    public float trimMax = +1.00f;

    [Header("PI Control")]
    public float kp = 0.040f;
    public float ki = 0.0009f;
    public float integratorClamp = 0.12f;
    public float deadbandFPAdeg = 0.55f;
    public float trimSlewPerSec = 1.60f;
    public float targetFPAdeg = 0f;

    [Header("Feed Forward")]
    public bool useFeedForward = true;
    public float ffWeight = 1f;
    public AnimationCurve trimVsKnots = new AnimationCurve();

    [Header("Polarity & Output")]
    [Range(-1f, 1f)] public float trimOutputPolarity = -1f;

    [Header("Speed Cap")]
    public float speedCapKnots = 1300f;
    public bool hardClamp = false;
    public bool softLimiter = true;
    public float limiterAccel = 25f;

    [Header("Debug")]
    [Range(-1f, 1f)] public float currentTrim;
    public float measuredFPAdeg;
    public float speedKnots;

    [Header("Momentum-Level Mode (AoA control)")]
    public bool levelToMomentum = true;        // when true, control to AoA instead of FPA
    public float targetAOAdeg = 0f;            // desired AoA when levelToMomentum = true
    public float measuredAOAdeg;               // debug

    float integ;

    void Reset()
    {
        rb = GetComponent<Rigidbody>();
        if (aircraft == null) aircraft = GetComponent<AerodynamicObjects.Tutorials.AircraftManager_Jet>();
    }

    void Awake()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoLoadFlightCurveOnPlay) TryLoadFlightCurveFromFile();
#endif
    }

    void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && autoLoadFlightCurveOnPlay) TryLoadFlightCurveFromFile();
#endif
    }

    void Start()
    {
        if (autoLoadFlightCurveOnPlay) TryLoadFlightCurveFromFile();
    }

    public void ReloadFlightCurveNow()
    {
        TryLoadFlightCurveFromFile(true);
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        Vector3 v = rb.linearVelocity;
        float spd = v.magnitude;
        speedKnots = spd * 1.943844f;

        Vector3 nhat = spd > 0.1f ? v.normalized : transform.forward;

        // NEW: AoA measurement in local pitch plane (nose vs velocity)
        {
            Vector3 vLocal = transform.InverseTransformDirection(v);
            measuredAOAdeg = Mathf.Atan2(vLocal.y, vLocal.z) * Mathf.Rad2Deg;
        }

        // existing FPA measurement (kept for world-level mode)
        measuredFPAdeg = Mathf.Asin(Mathf.Clamp(Vector3.Dot(nhat, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;

        float ff = 0f;
        if (useFeedForward && trimVsKnots != null && trimVsKnots.keys != null && trimVsKnots.keys.Length > 0)
        {
            float table = trimVsKnots.Evaluate(Mathf.Max(0f, speedKnots));
            ff = Mathf.Clamp(table * ffWeight, GetLowerLimit(), GetUpperLimit());
        }

        float errRaw;
        if (levelToMomentum)
        {
            // control to AoA (level relative to momentum)
            errRaw = targetAOAdeg - measuredAOAdeg;
        }
        else
        {
            // original control to FPA (level to world)
            errRaw = targetFPAdeg - measuredFPAdeg;
        }

        float err = Mathf.Abs(errRaw) < deadbandFPAdeg ? 0f : errRaw;

        float dt = Time.fixedDeltaTime;
        float p = kp * err;

        bool atUpper = currentTrim >= GetUpperLimit() - 1e-4f && (p + integ + ff) > 0f;
        bool atLower = currentTrim <= GetLowerLimit() + 1e-4f && (p + integ + ff) < 0f;
        if (!atUpper && !atLower)
            integ = Mathf.Clamp(integ + ki * err * dt, -integratorClamp, integratorClamp);

        float cmd = p + integ + ff;
        ApplyTrim(cmd, dt);

        float capMS = Mathf.Max(0f, speedCapKnots) * 0.514444f;
        if (capMS > 0f && spd > capMS)
        {
            if (hardClamp) rb.linearVelocity = nhat * capMS;
            if (softLimiter)
            {
                float excess = spd - capMS;
                rb.AddForce(-nhat * limiterAccel * excess, ForceMode.Acceleration);
            }
        }
    }

    float GetLowerLimit() => Mathf.Min(trimMin, trimMax);
    float GetUpperLimit() => Mathf.Max(trimMin, trimMax);

    void ApplyTrim(float cmd, float dt)
    {
        float target = Mathf.Clamp(cmd, GetLowerLimit(), GetUpperLimit());
        currentTrim = Mathf.MoveTowards(currentTrim, target, trimSlewPerSec * dt);
        if (aircraft != null) aircraft.externalPitchTrim = trimOutputPolarity * currentTrim;
    }

    void TryLoadFlightCurveFromFile(bool logSuccess = false)
    {
        if (string.IsNullOrWhiteSpace(flightCurveCsvPath)) return;

        try
        {
            string fullPath = Path.GetFullPath(flightCurveCsvPath);
            if (!File.Exists(fullPath))
            {
                Debug.LogWarning($"[AutoTrim] Flight curve file not found: {fullPath}");
                return;
            }

            string[] lines = File.ReadAllLines(fullPath);
            if (lines == null || lines.Length == 0)
            {
                Debug.LogWarning($"[AutoTrim] Flight curve file is empty: {fullPath}");
                return;
            }

            ParseFlightCurveCsv(lines);

            if (logSuccess)
                Debug.Log($"[AutoTrim] Loaded settings + {trimVsKnots.length} curve keys from: {fullPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AutoTrim] Failed to load flight curve file: {ex.Message}");
        }
    }

    void ParseFlightCurveCsv(string[] lines)
    {
        var inv = CultureInfo.InvariantCulture;
        var keys = new List<Keyframe>(2048);

        bool inCurve = false;

        for (int i = 0; i < lines.Length; i++)
        {
            string raw = lines[i];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            string line = raw.Trim();

            // strip comments
            int hash = line.IndexOf('#');
            if (hash >= 0) line = line.Substring(0, hash).Trim();
            int slashes = line.IndexOf("//", StringComparison.Ordinal);
            if (slashes >= 0) line = line.Substring(0, slashes).Trim();

            if (string.IsNullOrEmpty(line)) continue;

            // section switch
            string lc = line.ToLowerInvariant();
            if (lc == "[curve]" || lc == "curve" || lc == "[keys]" || lc == "keys")
            {
                inCurve = true;
                continue;
            }
            if (lc == "[settings]" || lc == "settings")
            {
                inCurve = false;
                continue;
            }

            if (!inCurve)
            {
                // key=value or key,value
                string[] kv = line.Split(new[] { '=', ',' }, 2, StringSplitOptions.RemoveEmptyEntries);
                if (kv.Length == 2)
                {
                    string key = kv[0].Trim().ToLowerInvariant();
                    string val = kv[1].Trim();

                    // booleans
                    bool TryBool(out bool b)
                    {
                        return bool.TryParse(val, out b) ||
                               (val == "1" ? (b = true) == true : val == "0" ? (b = false) == false : false);
                    }

                    // floats
                    bool TryFloat(out float f) => float.TryParse(val, NumberStyles.Float, inv, out f);

                    switch (key)
                    {
                        case "trimmin": if (TryFloat(out var f0)) trimMin = f0; break;
                        case "trimmax": if (TryFloat(out var f1)) trimMax = f1; break;
                        case "kp": if (TryFloat(out var f2)) kp = f2; break;
                        case "ki": if (TryFloat(out var f3)) ki = f3; break;
                        case "integratorclamp": if (TryFloat(out var f4)) integratorClamp = f4; break;
                        case "deadbandfpadeg": if (TryFloat(out var f5)) deadbandFPAdeg = f5; break;
                        case "trimslewpersec": if (TryFloat(out var f6)) trimSlewPerSec = f6; break;
                        case "targetfpadeg": if (TryFloat(out var f7)) targetFPAdeg = f7; break;
                        case "usefeedforward": if (TryBool(out var b0)) useFeedForward = b0; break;
                        case "ffweight": if (TryFloat(out var f8)) ffWeight = f8; break;
                        case "trimoutputpolarity":
                            if (TryFloat(out var f9)) trimOutputPolarity = Mathf.Clamp(f9, -1f, 1f); break;
                        case "speedcapknots": if (TryFloat(out var f10)) speedCapKnots = Mathf.Max(0f, f10); break;
                        case "hardclamp": if (TryBool(out var b1)) hardClamp = b1; break;
                        case "softlimiter": if (TryBool(out var b2)) softLimiter = b2; break;
                        case "limiteraccel": if (TryFloat(out var f11)) limiterAccel = Mathf.Max(0f, f11); break;

                        // NEW: momentum-level settings
                        case "leveltomomentum": if (TryBool(out var b3)) levelToMomentum = b3; break;
                        case "targetaoadeg": if (TryFloat(out var f12)) targetAOAdeg = f12; break;

                        default: break; // ignore unknown
                    }
                }

                continue;
            }

            // curve rows: "knot,trim" (header allowed; skip if not numeric)
            string[] parts = line.Split(new[] { ',', ';', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) continue;

            if (!float.TryParse(parts[0], NumberStyles.Float, inv, out float k)) continue;
            if (!float.TryParse(parts[1], NumberStyles.Float, inv, out float v)) continue;

            if (k < 0f) continue;
            keys.Add(new Keyframe(k, v));
        }

        // Apply curve if any keys were parsed
        if (keys.Count > 0)
        {
            keys.Sort((a, b) => a.time.CompareTo(b.time));
            trimVsKnots = new AnimationCurve(keys.ToArray())
            {
                preWrapMode = WrapMode.ClampForever,
                postWrapMode = WrapModeClampForeverSafe()
            };
        }

        // Ensure limits are ordered
        if (trimMin > trimMax)
        {
            float t = trimMin;
            trimMin = trimMax;
            trimMax = t;
        }
    }

    WrapMode WrapModeClampForeverSafe()
    {
        // On some Unity versions ClampForever is enough; keeping method for future customization
        return WrapMode.ClampForever;
    }
}
