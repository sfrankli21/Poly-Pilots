using System;
using System.IO;
using System.Text;
using UnityEngine;

[DefaultExecutionOrder(50)]
public class FlightDataRecorder : MonoBehaviour
{
    [Header("Output")]
    [Tooltip("Absolute path preferred. If left empty, will write to Application.persistentDataPath/RecordedFlightData.csv")]
    public string recordedFlightCsvPath = "";
    [Tooltip("Default file name when recordedFlightCsvPath is empty.")]
    public string defaultFileName = "RecordedFlightData.csv";

    [Header("Sources")]
    public Rigidbody rb;
    public AutoTrimAndSpeedLimiter autoTrim;

    [Header("Recording")]
    [Tooltip("Start recording automatically on Play.")]
    public bool recordOnStart = true;
    [Tooltip("Samples per second. 0 = every FixedUpdate.")]
    public float samplesPerSecond = 10f;
    [Tooltip("Write UTC timestamp/header at file start.")]
    public bool includeUtcTimestampInHeader = true;

    [Header("Diagnostics")]
    public bool verboseLogging = true;
    [Tooltip("Log every N rows (0 = never).")]
    public int logEveryNRows = 0;

    // ---- internals ----
    private StreamWriter writer;
    private FileStream stream;
    private string resolvedPath = "";
    private bool isRecording = false;
    private double nextSampleTime;
    private int rowsWritten = 0;
    private static readonly UTF8Encoding Utf8NoBom = new UTF8Encoding(false);

    void Reset()
    {
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (autoTrim == null) autoTrim = GetComponent<AutoTrimAndSpeedLimiter>();
    }

    void Start()
    {
        if (recordOnStart) StartRecording(overwriteOnPlay: true);
    }

    void OnDisable() { StopRecording(); }
    void OnDestroy() { StopRecording(); }

    /// <summary>
    /// Start logging. If overwriteOnPlay is true, the file is cleared and headers rewritten.
    /// </summary>
    public void StartRecording(bool overwriteOnPlay = true)
    {
        if (isRecording) return;

        resolvedPath = ResolvePath(recordedFlightCsvPath, defaultFileName);
        try
        {
            // Ensure directory exists
            var dir = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // Open with Create (always clears), share-read so you can open the file in another app while running.
            var mode = overwriteOnPlay ? FileMode.Create : FileMode.Append;
            stream = new FileStream(resolvedPath, mode, FileAccess.Write, FileShare.Read);
            writer = new StreamWriter(stream, Utf8NoBom) { AutoFlush = true };

            if (overwriteOnPlay)
                WriteHeader();
            else if (includeUtcTimestampInHeader)
                writer.WriteLine($"# SESSION START UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");

            nextSampleTime = Time.timeAsDouble;
            rowsWritten = 0;
            isRecording = true;

            if (verboseLogging)
                Debug.Log($"[FlightDataRecorder] Recording to: {resolvedPath}");
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
        if (verboseLogging)
            Debug.Log($"[FlightDataRecorder] Stopped. Rows: {rowsWritten}  File: {resolvedPath}");
    }

    void FixedUpdate()
    {
        if (!isRecording || writer == null) return;

        // Rate control
        if (samplesPerSecond > 0f)
        {
            double now = Time.timeAsDouble;
            if (now < nextSampleTime) return;
            double dt = 1.0 / Mathf.Max(0.0001f, samplesPerSecond);
            nextSampleTime += dt;
            if (nextSampleTime < now) nextSampleTime = now;
        }

        // ---- Gather data ----
        Vector3 vel = rb != null ? rb.linearVelocity : Vector3.zero; // If your project uses linearVelocity, change to rb.linearVelocity
        float speedMS = vel.magnitude;
        float speedKts = speedMS * 1.943844f;

        Vector3 nhat = speedMS > 0.1f ? vel.normalized : transform.forward;
        float fpaDeg = Mathf.Asin(Mathf.Clamp(Vector3.Dot(nhat, Vector3.up), -1f, 1f)) * Mathf.Rad2Deg;

        float trimOut = autoTrim != null ? autoTrim.currentTrim : 0f;
        float targetFpa = autoTrim != null ? autoTrim.targetFPAdeg : 0f;
        float speedKtsA = autoTrim != null ? autoTrim.speedKnots : speedKts;

        Vector3 pos = transform.position;
        Vector3 eul = transform.rotation.eulerAngles;

        // ---- Write row ----
        try
        {
            writer.Write(Time.time.ToString("F4")); writer.Write(',');
            writer.Write(Time.fixedDeltaTime.ToString("F4")); writer.Write(',');
            writer.Write(pos.x.ToString("G6")); writer.Write(',');
            writer.Write(pos.y.ToString("G6")); writer.Write(',');
            writer.Write(pos.z.ToString("G6")); writer.Write(',');
            writer.Write(eul.x.ToString("G6")); writer.Write(',');
            writer.Write(eul.y.ToString("G6")); writer.Write(',');
            writer.Write(eul.z.ToString("G6")); writer.Write(',');
            writer.Write(speedKts.ToString("G6")); writer.Write(',');
            writer.Write(speedKtsA.ToString("G6")); writer.Write(',');
            writer.Write(fpaDeg.ToString("G6")); writer.Write(',');
            writer.Write(trimOut.ToString("G6")); writer.Write(',');
            writer.Write(targetFpa.ToString("G6"));
            writer.WriteLine();

            rowsWritten++;
            if (logEveryNRows > 0 && (rowsWritten % logEveryNRows) == 0 && verboseLogging)
                Debug.Log($"[FlightDataRecorder] Rows written: {rowsWritten}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[FlightDataRecorder] Write failed: {ex.Message}");
            StopRecording();
        }
    }

    // ------- helpers -------

    void WriteHeader()
    {
        if (includeUtcTimestampInHeader)
            writer.WriteLine($"# UTC: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z");

        writer.WriteLine("# time,timeFixedDelta,posX,posY,posZ,eulX,eulY,eulZ,speedKts,speedKtsAuto,fpaDeg,trim,currentFpaTarget");
        writer.WriteLine("time,timeFixedDelta,posX,posY,posZ,eulX,eulY,eulZ,speedKts,speedKtsAuto,fpaDeg,trim,currentFpaTarget");
    }

    static string ResolvePath(string userPath, string fallbackFileName)
    {
        // If user gave an absolute path, normalize and return.
        if (!string.IsNullOrWhiteSpace(userPath) && Path.IsPathRooted(userPath))
            return Path.GetFullPath(userPath);

        // If user gave a relative path (Editor usage), make it relative to project root.
        if (!string.IsNullOrWhiteSpace(userPath))
        {
            string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
            return Path.GetFullPath(Path.Combine(projectRoot, userPath));
        }

        // Fallback: persistentDataPath (works in builds)
        string name = string.IsNullOrWhiteSpace(fallbackFileName) ? "RecordedFlightData.csv" : fallbackFileName;
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
}
