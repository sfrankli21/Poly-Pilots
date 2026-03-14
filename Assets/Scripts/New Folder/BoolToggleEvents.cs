using UnityEngine;
using UnityEngine.Events;

public class BoolToggleEvents : MonoBehaviour
{
    [SerializeField]
    bool isActive = false;

    [Header("Events")]
    public UnityEvent onActivate;
    public UnityEvent onDeactivate;

    public void Toggle()
    {
        isActive = !isActive;

        if (isActive)
            onActivate.Invoke();
        else
            onDeactivate.Invoke();
    }

    public void Activate()
    {
        if (isActive) return;
        isActive = true;
        onActivate.Invoke();
    }

    public void Deactivate()
    {
        if (!isActive) return;
        isActive = false;
        onDeactivate.Invoke();
    }

    public bool IsActive()
    {
        return isActive;
    }
}
