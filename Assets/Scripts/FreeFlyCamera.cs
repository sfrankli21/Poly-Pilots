using UnityEngine;

[AddComponentMenu("Camera/Mouse & Keyboard Free Look")]
public class FreeFlyCamera : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSensitivity = 1f;

    float yaw;
    float pitch;

    void Start()
    {
        Vector3 e = transform.eulerAngles;
        yaw = e.y;
        pitch = e.x;
        if (Application.isFocused) LockCursor();
        else UnlockCursor();
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus) LockCursor();
        else UnlockCursor();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
            return;
        }

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X") * lookSensitivity;
            float my = Input.GetAxis("Mouse Y") * lookSensitivity;

            yaw += mx;
            pitch -= my;
            pitch = Mathf.Clamp(pitch, -89f, 89f);
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
        float right = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float up = (Input.GetKey(KeyCode.E) ? 1f : 0f) + (Input.GetKey(KeyCode.Q) ? -1f : 0f);

        Vector3 move = transform.forward * forward + transform.right * right + Vector3.up * up;
        if (move.sqrMagnitude > 1f) move.Normalize();
        transform.position += move * moveSpeed * Time.deltaTime;
    }

    void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
