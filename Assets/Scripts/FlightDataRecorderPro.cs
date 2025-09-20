using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(50)]
public class FlightDataRecorder : MonoBehaviour
{
    [Header("Output")]
    public string recordedFlightCsvPath = "";
    public string defaultFileName = "F22_FlightData.csv";
    public bool includeUtcTimestampInHeader = true;

    [Header("Sources")]
    public Rigidbody rb;
    public Transform aircraftRoot;

    [Header("Optional Inputs")]
    public InputActionReference pitchInput;
    public InputActionReference rollInput;
    public InputActionReference yawInput;
    public InputActionReference throttleInput;

    [Header("Optional Components")]
    public List<Component> controlSurfaces = new List<Component>();
    public List<Component> engineSources = new List<Component>();

    [Header("Environment")]
    public float airDensity = 1.225f;
    public float rho0 = 1.225f;
    public float speedOfSound = 340.29f;
    public float altitudeMeters = 0f;

    [Header("Recording")]
    public bool recordOnStart = true;
    public float samplesPerSecond = 10f;
    public bool verboseLogging = false;
    public int logEveryNRows = 0;

    private StreamWriter writer;
    private FileStream stream;
    private string resolvedPath = "";
    private bool isRecording = false;
    private double nextSampleTime;
    private int rowsWritten = 0;
    private long sampleIndex = 0;
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);
    private Vector3 lastVelWorld;
    private bool haveLast;

    void Reset()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (aircraftRoot == null) aircraftRoot = transform;
    }

    void OnEnable()
    {
        TryEnable(pitchInput);
        TryEnable(rollInput);
        TryEnable(yawInput);
        TryEnable(throttleInput);
    }

    void OnDisable()
    {
        TryDisable(pitchInput);
        TryDisable(rollInput);
        TryDisable(yawInput);
        TryDisable(throttleInput);
        StopRecording();
    }

    void Start()
    {
        if (recordOnStart) StartRecording(true);
        haveLast = false;
    }

    void OnDestroy()
    {
        StopRecording();
    }

    public void StartRecording(bool overwriteOnPlay)
    {
        if (isRecording) return;

        resolvedPath = ResolvePath(recordedFlightCsvPath, defaultFileName);
        try
        {
            var dir = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            stream = new FileStream(resolvedPath, overwriteOnPlay ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, Utf8NoBom) { AutoFlush = true };
            WriteHeader();

            nextSampleTime = Time.timeAsDouble;
            rowsWritten = 0;
            sampleIndex = 0;
            isRecording = true;
            haveLast = false;

            if (verboseLogging) Debug.Log($"[FlightDataRecorder] Recording to: {resolvedPath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlightDataRecorder] Failed to open CSV at '{resolvedPath}': {ex.Message}");
            Cleanup();
        }
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        try { writer?.Flush(); } catch { }
        Cleanup();
        if (verboseLogging) Debug.Log($"[FlightDataRecorder] Stopped. Rows: {rowsWritten} File: {resolvedPath}");
    }

    void FixedUpdate()
    {
        if (!isRecording || writer == null || rb == null) return;

        if (samplesPerSecond > 0f)
        {
            double now = Time.timeAsDouble;
            if (now < nextSampleTime) return;
            double dt = 1.0 / Mathf.Max(0.0001f, samplesPerSecond);
            nextSampleTime += dt;
            if (nextSampleTime < now) nextSampleTime = now;
        }

        float dtFix = Time.fixedDeltaTime;

        Vector3 velWorld = rb.linearVelocity;
        float V = velWorld.magnitude;
        float tasKts = V * 1.943844f;
        float iasKts = (V * Mathf.Sqrt(Mathf.Max(1e-6f, airDensity / Mathf.Max(1e-6f, rho0)))) * 1.943844f;
        float mach = speedOfSound > 0.01f ? (V / speedOfSound) : 0f;
        float qbar = 0.5f * airDensity * V * V;

        Vector3 velBody = aircraftRoot.InverseTransformDirection(velWorld);
        float u = velBody.z;
        float v = velBody.x;
        float w = velBody.y;

        float aoaDeg = Mathf.Atan2(w, Mathf.Max(1e-6f, u)) * Mathf.Rad2Deg;
        float betaDeg = Mathf.Atan2(v, Mathf.Max(1e-6f, u)) * Mathf.Rad2Deg;

        Vector3 eul = aircraftRoot.rotation.eulerAngles;
        Quaternion q = aircraftRoot.rotation;

        Vector3 angVelWorld = rb.angularVelocity;
        Vector3 angVelBody = aircraftRoot.InverseTransformDirection(angVelWorld);
        float pDeg = angVelBody.z * Mathf.Rad2Deg;
        float qDeg = angVelBody.x * Mathf.Rad2Deg;
        float rDeg = angVelBody.y * Mathf.Rad2Deg;

        Vector3 accelWorld = Vector3.zero;
        if (haveLast && dtFix > 1e-6f) accelWorld = (velWorld - lastVelWorld) / dtFix;
        Vector3 accelBody = aircraftRoot.InverseTransformDirection(accelWorld);

        const float g = 9.80665f;
        float Nx = accelBody.x / g;
        float Ny = accelBody.y / g;
        float Nz = accelBody.z / g;

        float alt = Mathf.Abs(altitudeMeters) > 0.0001f ? altitudeMeters : aircraftRoot.position.y;

        Vector3 localCoM = Vector3.zero;
        try { localCoM = aircraftRoot.InverseTransformPoint(rb.worldCenterOfMass); } catch { }

        float inPitch = ReadInput(pitchInput);
        float inRoll = ReadInput(rollInput);
        float inYaw = ReadInput(yawInput);
        float inThrottle = ReadInput(throttleInput);

        float avgDefl = float.NaN;
        if (controlSurfaces != null && controlSurfaces.Count > 0)
        {
            float sum = 0f; int n = 0;
            foreach (var c in controlSurfaces)
            {
                if (c == null) continue;
                var t = c.GetType();
                var prop = t.GetProperty("deflectionAngle");
                if (prop != null && prop.PropertyType == typeof(float)) { sum += (float)prop.GetValue(c, null); n++; }
                else
                {
                    var field = t.GetField("deflectionAngle");
                    if (field != null && field.FieldType == typeof(float)) { sum += (float)field.GetValue(c); n++; }
                }
            }
            if (n > 0) avgDefl = sum / n;
        }

        float thrustN = 0f;
        if (engineSources != null && engineSources.Count > 0)
        {
            foreach (var eng in engineSources)
            {
                if (eng == null) continue;
                var t = eng.GetType();
                float add = TryReadFloat(t, eng, "currentThrust");
                if (float.IsNaN(add)) add = TryReadFloat(t, eng, "thrust");
                if (float.IsNaN(add)) add = TryReadFloat(t, eng, "outputThrust");
                if (!float.IsNaN(add)) thrustN += add;
            }
        }

        try
        {
            writer.Write(Time.time.ToString("F4")); writer.Write(',');
            writer.Write(dtFix.ToString("F4")); writer.Write(',');
            writer.Write(sampleIndex.ToString()); writer.Write(',');

            writer.Write(aircraftRoot.position.x.ToString("G6")); writer.Write(',');
            writer.Write(aircraftRoot.position.y.ToString("G6")); writer.Write(',');
            writer.Write(aircraftRoot.position.z.ToString("G6")); writer.Write(',');

            writer.Write(eul.x.ToString("G6")); writer.Write(',');
            writer.Write(eul.y.ToString("G6")); writer.Write(',');
            writer.Write(eul.z.ToString("G6")); writer.Write(',');

            writer.Write(q.x.ToString("G6")); writer.Write(',');
            writer.Write(q.y.ToString("G6")); writer.Write(',');
            writer.Write(q.z.ToString("G6")); writer.Write(',');
            writer.Write(q.w.ToString("G6")); writer.Write(',');

            writer.Write(V.ToString("G6")); writer.Write(',');
            writer.Write(tasKts.ToString("G6")); writer.Write(',');
            writer.Write(iasKts.ToString("G6")); writer.Write(',');
            writer.Write(mach.ToString("G6")); writer.Write(',');
            writer.Write(qbar.ToString("G6")); writer.Write(',');
            writer.Write(airDensity.ToString("G6")); writer.Write(',');
            writer.Write(alt.ToString("G6")); writer.Write(',');

            writer.Write(velWorld.x.ToString("G6")); writer.Write(',');
            writer.Write(velWorld.y.ToString("G6")); writer.Write(',');
            writer.Write(velWorld.z.ToString("G6")); writer.Write(',');

            writer.Write(u.ToString("G6")); writer.Write(',');
            writer.Write(v.ToString("G6")); writer.Write(',');
            writer.Write(w.ToString("G6")); writer.Write(',');

            writer.Write(aoaDeg.ToString("G6")); writer.Write(',');
            writer.Write(betaDeg.ToString("G6")); writer.Write(',');

            writer.Write(pDeg.ToString("G6")); writer.Write(',');
            writer.Write(qDeg.ToString("G6")); writer.Write(',');
            writer.Write(rDeg.ToString("G6")); writer.Write(',');

            writer.Write(accelWorld.x.ToString("G6")); writer.Write(',');
            writer.Write(accelWorld.y.ToString("G6")); writer.Write(',');
            writer.Write(accelWorld.z.ToString("G6")); writer.Write(',');

            writer.Write(accelBody.x.ToString("G6")); writer.Write(',');
            writer.Write(accelBody.y.ToString("G6")); writer.Write(',');
            writer.Write(accelBody.z.ToString("G6")); writer.Write(',');

            writer.Write(Nx.ToString("G6")); writer.Write(',');
            writer.Write(Ny.ToString("G6")); writer.Write(',');
            writer.Write(Nz.ToString("G6")); writer.Write(',');

            writer.Write(localCoM.x.ToString("G6")); writer.Write(',');
            writer.Write(localCoM.y.ToString("G6")); writer.Write(',');
            writer.Write(localCoM.z.ToString("G6")); writer.Write(',');

            writer.Write(inPitch.ToString("G6")); writer.Write(',');
            writer.Write(inRoll.ToString("G6")); writer.Write(',');
            writer.Write(inYaw.ToString("G6")); writer.Write(',');
            writer.Write(inThrottle.ToString("G6")); writer.Write(',');

            writer.Write(avgDefl.ToString("G6")); writer.Write(',');
            writer.Write(thrustN.ToString("G6"));

            writer.WriteLine();

            rowsWritten++;
            sampleIndex++;
            if (logEveryNRows > 0 && (rowsWritten % logEveryNRows) == 0 && verboseLogging)
                Debug.Log($"[FlightDataRecorder] Rows written: {rowsWritten}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlightDataRecorder] Write failed: {ex.Message}");
            StopRecording();
        }

        lastVelWorld = velWorld;
        haveLast = true;
    }

    void WriteHeader()
    {
        if (includeUtcTimestampInHeader)
            writer.WriteLine($"# UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");

        writer.WriteLine("time_s,dt_s,sample_index,"
                       + "pos_x,pos_y,pos_z,"
                       + "rot_eul_x,rot_eul_y,rot_eul_z,"
                       + "rot_q_x,rot_q_y,rot_q_z,rot_q_w,"
                       + "tas_mps,tas_kts,ias_kts,mach,qbar_Pa,rho,altitude_m,"
                       + "vel_world_x,vel_world_y,vel_world_z,"
                       + "u_body_mps,v_body_mps,w_body_mps,"
                       + "aoa_deg,beta_deg,"
                       + "p_deg_s,q_deg_s,r_deg_s,"
                       + "ax_world,ay_world,az_world,"
                       + "ax_body,ay_body,az_body,"
                       + "Nx_g,Ny_g,Nz_g,"
                       + "com_local_x,com_local_y,com_local_z,"
                       + "in_pitch,in_roll,in_yaw,in_throttle,"
                       + "avg_deflection_deg,thrust_total_N");
    }

    static string ResolvePath(string userPath, string fallbackFileName)
    {
        if (!string.IsNullOrWhiteSpace(userPath) && Path.IsPathRooted(userPath))
            return Path.GetFullPath(userPath);

        if (!string.IsNullOrWhiteSpace(userPath))
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, userPath));
        }

        string name = string.IsNullOrWhiteSpace(fallbackFileName) ? "F22_FlightData.csv" : fallbackFileName;
        return Path.Combine(Application.persistentDataPath, name);
    }

    void Cleanup()
    {
        try { writer?.Close(); } catch { }
        try { stream?.Close(); } catch { }
        try { writer?.Dispose(); } catch { }
        try { stream?.Dispose(); } catch { }
        writer = null;
        stream = null;
        isRecording = false;
    }

    static void TryEnable(InputActionReference r) { try { r?.action?.Enable(); } catch { } }
    static void TryDisable(InputActionReference r) { try { r?.action?.Disable(); } catch { } }
    static float ReadInput(InputActionReference r)
    {
        if (r == null || r.action == null) return float.NaN;
        try { return r.action.ReadValue<float>(); } catch { return float.NaN; }
    }
    static float TryReadFloat(Type t, object instance, string member)
    {
        try
        {
            var p = t.GetProperty(member);
            if (p != null && p.PropertyType == typeof(float)) return (float)p.GetValue(instance, null);
            var f = t.GetField(member);
            if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(instance);
        }
        catch { }
        return float.NaN;
    }
}
