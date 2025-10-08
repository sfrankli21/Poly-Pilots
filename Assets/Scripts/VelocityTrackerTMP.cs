using UnityEngine;
using TMPro;

[ExecuteAlways]
public class VelocityTrackerTMP : MonoBehaviour
{
    [Header("Output")]
    public TMP_Text textTarget;
    public string label = "Speed";
    [Range(0, 4)] public int decimalPlaces = 1;

    [Header("Units (can enable multiple)")]
    public bool metersPerSecond = true;
    public bool knots = false;
    public bool milesPerHour = false;

    [Header("Smoothing")]
    [Range(0f, 1f)] public float smoothing = 0.2f;

    Rigidbody _rb;
    Vector3 _lastPos;
    bool _hadLastPos;
    float _displaySpeed;

    const float MS_TO_KNOTS = 1.94384449f;
    const float MS_TO_MPH = 2.23693629f;

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
        float speedMS = 0f;

        if (Application.isPlaying && _rb != null)
        {
            // Forward-only: project velocity onto transform.forward
            Vector3 v = _rb.linearVelocity; // use .velocity; replace with .linearVelocity if your rig defines it
            float forward = Vector3.Dot(v, transform.forward);
            speedMS = Mathf.Max(0f, forward);
        }
        else
        {
            // Editor/preview: derive velocity from position delta, then project onto forward
            if (!_hadLastPos)
            {
                _lastPos = transform.position;
                _hadLastPos = true;
            }
            float dt = Mathf.Max(Time.deltaTime, 1e-6f);
            Vector3 velApprox = (transform.position - _lastPos) / dt;
            _lastPos = transform.position;

            float forward = Vector3.Dot(velApprox, transform.forward);
            speedMS = Mathf.Max(0f, forward);
        }

        float rawSpeed = speedMS;
        if (smoothing > 0f)
        {
            _displaySpeed = Mathf.Lerp(_displaySpeed, rawSpeed, 1f - Mathf.Pow(1f - smoothing, Time.deltaTime * 60f));
        }
        else
        {
            _displaySpeed = rawSpeed;
        }

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
