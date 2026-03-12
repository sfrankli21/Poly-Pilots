using UnityEngine;
using UnityEngine.InputSystem;

public class WASDMoveObject : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 5f;
    public Transform cameraTransform;

    float localYaw;
    float localPitch;

    void Start()
    {
        if (cameraTransform != null)
        {
            Vector3 startEuler = cameraTransform.localEulerAngles;
            localPitch = NormalizeAngle(startEuler.x);
            localYaw = NormalizeAngle(startEuler.y);
        }
    }

    void Update()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.wKey.isPressed)
                input.y += 1f;

            if (Keyboard.current.sKey.isPressed)
                input.y -= 1f;

            if (Keyboard.current.aKey.isPressed)
                input.x -= 1f;

            if (Keyboard.current.dKey.isPressed)
                input.x += 1f;
        }

        Vector3 movement = new Vector3(input.x, input.y, 0f);
        transform.position += movement * moveSpeed * Time.deltaTime;

        if (cameraTransform != null && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            localYaw += mouseDelta.x * lookSpeed * Time.deltaTime;
            localPitch -= mouseDelta.y * lookSpeed * Time.deltaTime;
            localPitch = Mathf.Clamp(localPitch, -89f, 89f);

            cameraTransform.localRotation = Quaternion.Euler(localPitch, localYaw, 0f);
        }
    }

    float NormalizeAngle(float angle)
    {
        while (angle > 180f)
            angle -= 360f;

        while (angle < -180f)
            angle += 360f;

        return angle;
    }
}