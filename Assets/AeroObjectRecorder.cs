using System;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(60)]
public class AeroObjectRecorder : MonoBehaviour
{
    [Header("References")]
    public Component aeroObject;       // the AeroObject component
    public Rigidbody rb;               // same rigidbody the AeroObject applies forces to
    public Transform axes;             // aircraft/body axes (+Z fwd, +Y up, +X right). Defaults to this.transform

    [Header("Reference Geometry (for coefficients)")]
    public float referenceArea_m2 = 1f;     // S
    public float referenceChord_m = 1f;     // c (for Cm)
    public float referenceSpan_m = 1f;     // b (for Cl)

    [Header("Environment")]
    public float airDensity = 1.225f;       // rho
    public float rho0 = 1.225f;             // for IAS, optional
    public float speedOfSound = 340.29f;    // m/s

    [Header("Sampling")]
    public float samplesPerSecond = 20f;    // 0 = every FixedUpdate
    public bool recordOnStart = true;

    [Header("Output")]
    public string outputPath = "";          // leave empty to use persistentDataPath
    public string fileName = "AeroObjectTest.csv";
    public bool includeUtcTimestampInHeader = true;

    StreamWriter writer;
    FileStream stream;
    bool isRecording;
    double nextSampleTime;
    static readonly UTF8Encoding UTF8NoBom = new UTF8Encoding(false);

    void Reset()
    {
        rb ??= GetComponentInParent<Rigidbody>();
        axes ??= transform;
    }

    void OnEnable()
    {
        if (recordOnStart) StartRecording(true);
    }
    void OnDisable() { StopRecording(); }
    void OnDestroy() { StopRecording(); }

    public void StartRecording(bool overwrite)
    {
        if (isRecording) return;

        if (aeroObject == null || rb == null) { Debug.LogError("[AeroObjectRecorder] Assign AeroObject and Rigidbody."); return; }
        if (axes == null) axes = transform;

        string path = ResolvePath(outputPath, fileName);
        try
        {
            var dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
            stream = new FileStream(path, overwrite ? FileMode.Create : FileMode.Append, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, UTF8NoBom) { AutoFlush = true };
            WriteHeader();
            isRecording = true;
            nextSampleTime = Time.timeAsDouble;
            Debug.Log($"[AeroObjectRecorder] Recording to: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AeroObjectRecorder] Failed to open file: {ex.Message}");
            Cleanup();
        }
    }

    public void StopRecording()
    {
        if (!isRecording) return;
        try { writer?.Flush(); } catch { }
        Cleanup();
        isRecording = false;
    }

    void FixedUpdate()
    {
        if (!isRecording || writer == null) return;

        if (samplesPerSecond > 0f)
        {
            double now = Time.timeAsDouble;
            if (now < nextSampleTime) return;
            double dt = 1.0 / Mathf.Max(0.0001f, samplesPerSecond);
            nextSampleTime += dt;
            if (nextSampleTime < now) nextSampleTime = now;
        }

        float dtFix = Time.fixedDeltaTime;

        Vector3 vWorld = rb.linearVelocity;
        float V = vWorld.magnitude;
        float tas_kts = V * 1.943844f;
        float ias_kts = (V * Mathf.Sqrt(Mathf.Max(1e-6f, airDensity / Mathf.Max(1e-6f, rho0)))) * 1.943844f;
        float mach = speedOfSound > 0.01f ? V / speedOfSound : 0f;
        float qbar = 0.5f * airDensity * V * V;

        Vector3 vBody = axes.InverseTransformDirection(vWorld);
        float u = vBody.z;
        float v = vBody.x;
        float w = vBody.y;
        float aoa_deg = Mathf.Atan2(w, Mathf.Max(1e-6f, u)) * Mathf.Rad2Deg;
        float beta_deg = Mathf.Atan2(v, Mathf.Max(1e-6f, u)) * Mathf.Rad2Deg;

        // Wind-axis basis
        Vector3 Xw = (V > 1e-3f) ? (-vWorld.normalized) : -axes.forward;
        Vector3 Yw = Vector3.ProjectOnPlane(axes.right, Xw).normalized;
        if (Yw.sqrMagnitude < 1e-6f) Yw = Vector3.right; // fallback
        Vector3 Zw = Vector3.Cross(Xw, Yw).normalized;

        // Read Net Aerodynamic Load (Force & Moment) via reflection
        Vector3 F_world = Vector3.zero;
        Vector3 M_world = Vector3.zero;
        float dynP_fromComponent = float.NaN;

        TryReadNetLoad(aeroObject, ref F_world, ref M_world, ref dynP_fromComponent);

        // Components in wind axes
        float Drag = -Vector3.Dot(F_world, Xw);
        float Side = Vector3.Dot(F_world, Yw);
        float Lift = Vector3.Dot(F_world, Zw);

        // Moments to body axes (Unity conv.: roll Z, pitch X, yaw Y)
        Vector3 M_body = axes.InverseTransformDirection(M_world);
        float Mx = M_body.x; // pitch
        float My = M_body.y; // yaw
        float Mz = M_body.z; // roll

        // Coefficients
        float S = Mathf.Max(1e-6f, referenceArea_m2);
        float c = Mathf.Max(1e-6f, referenceChord_m);
        float b = Mathf.Max(1e-6f, referenceSpan_m);

        float CL = (qbar > 1e-6f) ? Lift / (qbar * S) : 0f;
        float CD = (qbar > 1e-6f) ? Drag / (qbar * S) : 0f;
        float CY = (qbar > 1e-6f) ? Side / (qbar * S) : 0f;

        float Cm = (qbar > 1e-6f) ? Mx / (qbar * S * c) : 0f;  // pitch
        float Cn = (qbar > 1e-6f) ? My / (qbar * S * b) : 0f;  // yaw
        float Cl = (qbar > 1e-6f) ? Mz / (qbar * S * b) : 0f;  // roll

        // Write row
        try
        {
            writer.Write(Time.time.ToString("F4")); writer.Write(',');
            writer.Write(dtFix.ToString("F4")); writer.Write(',');

            writer.Write(V.ToString("G6")); writer.Write(',');
            writer.Write(tas_kts.ToString("G6")); writer.Write(',');
            writer.Write(ias_kts.ToString("G6")); writer.Write(',');
            writer.Write(mach.ToString("G6")); writer.Write(',');
            writer.Write(qbar.ToString("G6")); writer.Write(',');
            writer.Write(airDensity.ToString("G6")); writer.Write(',');
            writer.Write((float.IsNaN(dynP_fromComponent) ? 0f : dynP_fromComponent).ToString("G6")); writer.Write(',');

            writer.Write(aoa_deg.ToString("G6")); writer.Write(',');
            writer.Write(beta_deg.ToString("G6")); writer.Write(',');

            writer.Write(F_world.x.ToString("G6")); writer.Write(',');
            writer.Write(F_world.y.ToString("G6")); writer.Write(',');
            writer.Write(F_world.z.ToString("G6")); writer.Write(',');

            writer.Write(M_world.x.ToString("G6")); writer.Write(',');
            writer.Write(M_world.y.ToString("G6")); writer.Write(',');
            writer.Write(M_world.z.ToString("G6")); writer.Write(',');

            writer.Write(Lift.ToString("G6")); writer.Write(',');
            writer.Write(Drag.ToString("G6")); writer.Write(',');
            writer.Write(Side.ToString("G6")); writer.Write(',');

            writer.Write(CL.ToString("G6")); writer.Write(',');
            writer.Write(CD.ToString("G6")); writer.Write(',');
            writer.Write(CY.ToString("G6")); writer.Write(',');

            writer.Write(Cm.ToString("G6")); writer.Write(',');
            writer.Write(Cn.ToString("G6")); writer.Write(',');
            writer.Write(Cl.ToString("G6")); writer.Write(',');

            writer.Write(referenceArea_m2.ToString("G6")); writer.Write(',');
            writer.Write(referenceChord_m.ToString("G6")); writer.Write(',');
            writer.Write(referenceSpan_m.ToString("G6"));

            writer.WriteLine();
        }
        catch (Exception ex)
        {
            Debug.LogError($"[AeroObjectRecorder] Write failed: {ex.Message}");
            StopRecording();
        }
    }

    void WriteHeader()
    {
        if (includeUtcTimestampInHeader)
            writer.WriteLine($"# UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");

        writer.WriteLine("time_s,dt_s,"
                       + "tas_mps,tas_kts,ias_kts,mach,qbar_Pa,rho,component_qbar_Pa,"
                       + "aoa_deg,beta_deg,"
                       + "F_world_x,F_world_y,F_world_z,"
                       + "M_world_x,M_world_y,M_world_z,"
                       + "Lift_N,Drag_N,Side_N,"
                       + "CL,CD,CY,Cm,Cn,Cl,"
                       + "S_m2,c_m,b_m");
    }

    static string ResolvePath(string userPath, string fileName)
    {
        if (!string.IsNullOrWhiteSpace(userPath) && Path.IsPathRooted(userPath))
            return Path.GetFullPath(userPath);
        if (!string.IsNullOrWhiteSpace(userPath))
        {
            string proj = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(proj, userPath));
        }
        return Path.Combine(Application.persistentDataPath, string.IsNullOrWhiteSpace(fileName) ? "AeroObjectTest.csv" : fileName);
    }

    void Cleanup()
    {
        try { writer?.Close(); } catch { }
        try { stream?.Close(); } catch { }
        try { writer?.Dispose(); } catch { }
        try { stream?.Dispose(); } catch { }
        writer = null; stream = null;
    }

    // Attempts to read Net Aerodynamic Load: Force/Moment (+ DynamicPressure if available)
    static void TryReadNetLoad(Component ao, ref Vector3 F_world, ref Vector3 M_world, ref float dynP)
    {
        if (ao == null) return;
        var t = ao.GetType();

        object netLoad = TryGetMember(t, ao, "NetAerodynamicLoad");
        if (netLoad == null) netLoad = TryGetMember(t, ao, "netAerodynamicLoad");
        if (netLoad == null) netLoad = TryGetMember(t, ao, "NetLoad");

        if (netLoad != null)
        {
            F_world = TryGetVector3(netLoad, "Force", "force");
            M_world = TryGetVector3(netLoad, "Moment", "moment");
            float dp = TryGetFloat(netLoad, "DynamicPressure", "dynamicPressure", "q", "qbar");
            if (!float.IsNaN(dp)) dynP = dp;
        }
        else
        {
            // Fallback: some builds expose force/moment directly on the component
            Vector3 f = TryGetVector3(ao, "Force", "force", "NetForce", "netForce");
            Vector3 m = TryGetVector3(ao, "Moment", "moment", "NetMoment", "netMoment");
            if (f != Vector3.zero) F_world = f;
            if (m != Vector3.zero) M_world = m;
            float dp = TryGetFloat(ao, "DynamicPressure", "dynamicPressure", "q", "qbar");
            if (!float.IsNaN(dp)) dynP = dp;
        }
    }

    static object TryGetMember(Type t, object inst, string name)
    {
        try
        {
            var p = t.GetProperty(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (p != null) return p.GetValue(inst, null);
            var f = t.GetField(name, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
            if (f != null) return f.GetValue(inst);
        }
        catch { }
        return null;
    }
    static Vector3 TryGetVector3(object inst, params string[] names)
    {
        foreach (var n in names)
        {
            try
            {
                var t = inst.GetType();
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(Vector3)) return (Vector3)p.GetValue(inst, null);
                var f = t.GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(Vector3)) return (Vector3)f.GetValue(inst);
            }
            catch { }
        }
        return Vector3.zero;
    }
    static float TryGetFloat(object inst, params string[] names)
    {
        foreach (var n in names)
        {
            try
            {
                var t = inst.GetType();
                var p = t.GetProperty(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (p != null && p.PropertyType == typeof(float)) return (float)p.GetValue(inst, null);
                var f = t.GetField(n, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (f != null && f.FieldType == typeof(float)) return (float)f.GetValue(inst);
            }
            catch { }
        }
        return float.NaN;
    }
}
