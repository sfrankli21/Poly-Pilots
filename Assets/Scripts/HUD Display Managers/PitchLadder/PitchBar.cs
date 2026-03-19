using TMPro;
using UnityEngine;

public class PitchBar : MonoBehaviour
{
    [SerializeField]
    TMP_Text leftText;
    [SerializeField]
    TMP_Text rightText;
    [SerializeField]
    Transform leftTextRoot;
    [SerializeField]
    Transform rightTextRoot;

    public void SetNumber(int angle)
    {
        string value = Mathf.Abs(angle).ToString();

        if (leftText != null)
        {
            leftText.text = value;
        }

        if (rightText != null)
        {
            rightText.text = value;
        }
    }

    public void UpdateColor(Color color)
    {
        if (leftText != null)
        {
            leftText.color = color;
        }

        if (rightText != null)
        {
            rightText.color = color;
        }
    }

    public void UpdateRoll(float roll)
    {
        if (leftTextRoot != null)
        {
            leftTextRoot.localEulerAngles = new Vector3(0f, 0f, roll);
        }

        if (rightTextRoot != null)
        {
            rightTextRoot.localEulerAngles = new Vector3(0f, 0f, roll);
        }
    }
}