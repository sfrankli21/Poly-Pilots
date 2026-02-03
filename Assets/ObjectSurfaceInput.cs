using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectSurfaceInput : MonoBehaviour
{
    [SerializeField] InputActionReference inputAction;
    [SerializeField] float minXRotation = -30f;
    [SerializeField] float maxXRotation = 30f;
    [SerializeField, InspectorName("Reflect")] bool reflect;

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
        if (inputAction == null)
            return;

        float inputValue = inputAction.action.ReadValue<float>();

        if (reflect)
            inputValue = -inputValue;

        float t = (inputValue + 1f) * 0.5f;
        float xRotation = Mathf.Lerp(minXRotation, maxXRotation, t);

        Vector3 localEuler = transform.localEulerAngles;
        localEuler.x = xRotation;
        transform.localEulerAngles = localEuler;
    }
}
