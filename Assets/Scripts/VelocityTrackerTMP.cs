using UnityEngine;
using TMPro;

[ExecuteAlways]
public class VelocityTrackerTMP : MonoBehaviour
{
    [Header("Output")]
    public TMP_Text textTarget;
    [Tooltip("Text prefix, for example: Speed")]
    public string label = "Speed";
    [Tooltip("How many decimal places to show")]
    [Range(0, 4)] public int decimalPlaces = 1;

    [Header("Units (can enable multiple)")]
    public bool metersPerSecond = true;
    public bool knots = false;
    public bool milesPerHour = false;

    [Header("Smoothing")]
    [Tooltip("If greater than zero, smooths the displayed speed with an exponential moving average")]
    [Range(0f, 1f)] public float smoothing = 0.2f;

    Rigidbody _rb;
    Vector3 _lastPos;
    bool _hadLastPos;
    float _displaySpeed;

    const float MS_TO_KNOTS = 1.94384449f;   // meters per second to knots
    const float MS_TO_MPH = 2.23693629f;   // meters per second to miles per hour

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void OnEnable()
    {
        _lastPos = transform.position;
        _hadLastPos = true;
    }

    void Update()
    {
        // Determine velocity magnitude in meters per second
        float speedMS = 0f;

        if (Application.isPlaying && _rb != null)
        {
            speedMS = _rb.linearVelocity.magnitude;
        }
        else
        {
            if (!_hadLastPos)
            {
                _lastPos = transform.position;
                _hadLastPos = true;
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            speedMS = (transform.position - _lastPos).magnitude / dt;
            _lastPos = transform.position;
        }

        // Smooth value if requested
        float rawSpeed = speedMS;
        if (smoothing > 0f)
        {
            _displaySpeed = Mathf.Lerp(_displaySpeed, rawSpeed, 1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f));
        }
        else
        {
            _displaySpeed = rawSpeed;
        }

        // Build display string
        if (textTarget != null)
        {
            string fmt = "F" + decimalPlaces.ToString();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            if (!string.IsNullOrEmpty(label))
                sb.Append(label).Append(": ");

            bool added = false;

            if (metersPerSecond)
            {
                if (added) sb.Append(" | ");
                sb.Append(_displaySpeed.ToString(fmt)).Append(" m/s");
                added = true;
            }

            if (knots)
            {
                if (added) sb.Append(" | ");
                sb.Append((_displaySpeed * MS_TO_KNOTS).ToString(fmt)).Append(" kn");
                added = true;
            }

            if (milesPerHour)
            {
                if (added) sb.Append(" | ");
                sb.Append((_displaySpeed * MS_TO_MPH).ToString(fmt)).Append(" mph");
                added = true;
            }

            if (!metersPerSecond && !knots && !milesPerHour)
            {
                sb.Append(_displaySpeed.ToString(fmt)).Append(" m/s");
            }

            textTarget.text = sb.ToString();
        }
    }
}
