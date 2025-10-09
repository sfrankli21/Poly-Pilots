using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class DebugEventTrigger : MonoBehaviour
{
    public UnityEvent OnNinePressed;
    public UnityEvent OnZeroPressed;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        if (kb.digit9Key.wasPressedThisFrame) OnNinePressed?.Invoke();
        if (kb.digit0Key.wasPressedThisFrame) OnZeroPressed?.Invoke();
    }
}
