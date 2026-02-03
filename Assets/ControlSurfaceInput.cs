using UnityEngine;
using UnityEngine.InputSystem;
using AerodynamicObjects;

public class ControlSurfaceInput : MonoBehaviour
{
    [SerializeField] ControlSurface controlSurface;
    [SerializeField] InputActionReference inputAction;
    [SerializeField] float minDeflectionRadians = -0.5f;
    [SerializeField] float maxDeflectionRadians = 0.5f;
    [SerializeField, InspectorName("Reflect")] bool reflect;

    void Awake()
    {
        if (controlSurface == null)
            controlSurface = GetComponent<ControlSurface>();
    }

    void OnEnable()
    {
        if (inputAction != null)
            inputAction.action.Enable();
    }

    void OnDisable()
    {
        if (inputAction != null)
            inputAction.action.Disable();
    }

    void Update()
    {
        if (controlSurface == null || inputAction == null)
            return;

        float inputValue = inputAction.action.ReadValue<float>();

        if (reflect)
            inputValue = -inputValue;

        float t = (inputValue + 1f) * 0.5f;
        float deflection = Mathf.Lerp(minDeflectionRadians, maxDeflectionRadians, t);

        controlSurface.deflectionAngle = deflection;
    }
}
