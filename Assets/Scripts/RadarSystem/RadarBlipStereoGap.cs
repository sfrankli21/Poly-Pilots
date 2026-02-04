using UnityEngine;
using UnityEngine.XR;

public class RadarBlipStereoGap : MonoBehaviour
{
    [Header("Auto Bind Tags")]
    [SerializeField] string playerCameraTag = "MainCamera";
    [SerializeField] string hudTag = "HUD";

    [Header("References")]
    [SerializeField] Transform playerCamera;
    [SerializeField] Transform hudPlane;

    [Header("Blip Halves (RectTransforms)")]
    [SerializeField] RectTransform leftHalf;
    [SerializeField] RectTransform rightHalf;

    [Header("IPD")]
    [SerializeField] bool autoIPD = true;
    [SerializeField] float ipdFallbackMeters = 0.064f;

    [Header("Behavior")]
    [SerializeField] bool clampToDivergentOnly = true;
    [SerializeField] float maxSeparationMeters = 0.04f;
    [SerializeField] float smoothSpeed = 20f;

    [Header("Safety")]
    [SerializeField] float minBlipDepthMeters = 0.05f;
    [SerializeField] float minHudDepthMeters = 0.05f;

    Vector3 leftBaseLocalPos;
    Vector3 rightBaseLocalPos;

    float currentSeparationMeters;
    bool bound;

    void Awake()
    {
        if (leftHalf != null) leftBaseLocalPos = leftHalf.localPosition;
        if (rightHalf != null) rightBaseLocalPos = rightHalf.localPosition;

        TryAutoBind();
    }

    void OnEnable()
    {
        TryAutoBind();
    }

    void Update()
    {
        if (!bound)
        {
            TryAutoBind();
            return;
        }

        if (leftHalf == null || rightHalf == null)
            return;

        Vector3 camPos = playerCamera.position;
        Vector3 camFwd = playerCamera.forward;

        float zHud = Vector3.Dot(hudPlane.position - camPos, camFwd);
        if (zHud < minHudDepthMeters) zHud = minHudDepthMeters;

        float zBlip = Vector3.Dot(transform.position - camPos, camFwd);
        if (zBlip < minBlipDepthMeters)
        {
            ApplySeparationMeters(0f);
            return;
        }

        float ipd = autoIPD ? GetIPDMetersFallback(ipdFallbackMeters) : Mathf.Max(0.001f, ipdFallbackMeters);

        float desiredSeparation = ipd * (1f - (zHud / zBlip));

        if (clampToDivergentOnly)
            desiredSeparation = Mathf.Max(0f, desiredSeparation);

        desiredSeparation = Mathf.Clamp(desiredSeparation, -maxSeparationMeters, maxSeparationMeters);

        float t = 1f - Mathf.Exp(-smoothSpeed * Time.deltaTime);
        currentSeparationMeters = Mathf.Lerp(currentSeparationMeters, desiredSeparation, t);

        ApplySeparationMeters(currentSeparationMeters);
    }

    void TryAutoBind()
    {
        if (playerCamera == null)
        {
            GameObject camObj = GameObject.FindGameObjectWithTag(playerCameraTag);
            if (camObj != null) playerCamera = camObj.transform;
        }

        if (hudPlane == null)
        {
            GameObject hudObj = GameObject.FindGameObjectWithTag(hudTag);
            if (hudObj != null) hudPlane = hudObj.transform;
        }

        bound = (playerCamera != null && hudPlane != null);
    }

    void ApplySeparationMeters(float separationMeters)
    {
        float halfSep = separationMeters * 0.5f;

        Transform space = transform.parent != null ? transform.parent : transform;

        Vector3 worldOffset = hudPlane.right * halfSep;
        Vector3 localOffset = space.InverseTransformVector(worldOffset);

        Vector3 l = leftBaseLocalPos;
        Vector3 r = rightBaseLocalPos;

        l.x -= localOffset.x;
        r.x += localOffset.x;

        leftHalf.localPosition = l;
        rightHalf.localPosition = r;
    }

    static float GetIPDMetersFallback(float fallback)
    {
        Vector3 left = InputTracking.GetLocalPosition(XRNode.LeftEye);
        Vector3 right = InputTracking.GetLocalPosition(XRNode.RightEye);
        float ipd = (right - left).magnitude;
        if (ipd > 0.03f && ipd < 0.09f) return ipd;
        return Mathf.Max(0.001f, fallback);
    }

    public void ForceRebind()
    {
        bound = false;
        TryAutoBind();
    }

    public void SetReferences(Transform cameraTransform, Transform hudTransform)
    {
        playerCamera = cameraTransform;
        hudPlane = hudTransform;
        bound = (playerCamera != null && hudPlane != null);
    }
}
