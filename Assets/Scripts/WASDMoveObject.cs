using UnityEngine;
using UnityEngine.InputSystem;

public class WASDMoveObject : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 5f;
    public Transform cameraTransform;
    public float scrollFovSpeed = 10f;
    public float minFov = 20f;
    public float maxFov = 90f;

    float localYaw;
    float localPitch;
    Camera targetCamera;
    bool cursorUnlocked;

    void Start()
    {
        if (cameraTransform != null)
        {
            Vector3 startEuler = cameraTransform.localEulerAngles;
            localPitch = NormalizeAngle(startEuler.x);
            localYaw = NormalizeAngle(startEuler.y);
            targetCamera = cameraTransform.GetComponent<Camera>();
        }

        LockCursor();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (cursorUnlocked)
                LockCursor();
            else
                UnlockCursor();
        }

        if (Application.isFocused && !cursorUnlocked && Cursor.lockState != CursorLockMode.Confined)
        {
            LockCursor();
        }

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

        if (!cursorUnlocked && cameraTransform != null && Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            localYaw += mouseDelta.x * lookSpeed * Time.deltaTime;
            localPitch -= mouseDelta.y * lookSpeed * Time.deltaTime;
            localPitch = Mathf.Clamp(localPitch, -89f, 89f);

            cameraTransform.localRotation = Quaternion.Euler(localPitch, localYaw, 0f);

            if (targetCamera != null)
            {
                float scroll = Mouse.current.scroll.ReadValue().y;
                targetCamera.fieldOfView -= scroll * scrollFovSpeed * Time.deltaTime;
                targetCamera.fieldOfView = Mathf.Clamp(targetCamera.fieldOfView, minFov, maxFov);
            }
        }
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !cursorUnlocked)
            LockCursor();
    }

    void LockCursor()
    {
        cursorUnlocked = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    void UnlockCursor()
    {
        cursorUnlocked = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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