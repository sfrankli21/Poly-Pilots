using UnityEngine;

public class HeatSignature : MonoBehaviour
{
    [Range(0f, 20f)]
    public float heatValue = 0f;

    public float GetHeatValue()
    {
        return heatValue;
    }

    public void SetHeatValue(float newValue)
    {
        heatValue = Mathf.Clamp(newValue, 0f, 20f);
    }
}