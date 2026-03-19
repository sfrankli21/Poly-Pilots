using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Compass : MonoBehaviour
{
    [SerializeField]
    GameObject tickLargePrefab;
    [SerializeField]
    GameObject tickSmallPrefab;
    [SerializeField]
    GameObject textPrefab;
    [SerializeField]
    int largeTickInterval = 10;
    [SerializeField]
    int smallTickInterval = 5;
    [SerializeField]
    float unitsPerDegree = 0.02f;
    [SerializeField]
    float visibleRangeDegrees = 90f;
    [SerializeField]
    JetMechanics jetMechanics;
    [SerializeField]
    float headingOffsetDegrees;

    struct Tick
    {
        public Transform transform;
        public Image image;
        public int angle;

        public Tick(Transform transform, Image image, int angle)
        {
            this.transform = transform;
            this.image = image;
            this.angle = angle;
        }
    }

    static readonly string[] directions =
    {
        "N",
        "NE",
        "E",
        "SE",
        "S",
        "SW",
        "W",
        "NW"
    };

    List<Tick> ticks;
    List<TMP_Text> tickText;

    void Start()
    {
        ticks = new List<Tick>();
        tickText = new List<TMP_Text>();

        for (int i = 0; i < 360; i++)
        {
            if (i % largeTickInterval == 0)
            {
                MakeLargeTick(i);
            }
            else if (i % smallTickInterval == 0)
            {
                MakeSmallTick(i);
            }
        }
    }

    public void SetJetMechanics(JetMechanics jet)
    {
        jetMechanics = jet;
    }

    public void UpdateColor(Color color)
    {
        foreach (var tick in ticks)
        {
            if (tick.image != null)
            {
                tick.image.color = color;
            }
        }

        foreach (var text in tickText)
        {
            if (text != null)
            {
                text.color = color;
            }
        }
    }

    void MakeLargeTick(int angle)
    {
        GameObject tickGO = Instantiate(tickLargePrefab, transform);
        Transform tickTransform = tickGO.transform;
        Image tickImage = tickGO.GetComponent<Image>();

        GameObject textGO = Instantiate(textPrefab, tickTransform);
        TMP_Text text = textGO.GetComponent<TMP_Text>();

        if (text != null)
        {
            if (angle % 45 == 0)
            {
                text.text = directions[angle / 45];
            }
            else
            {
                text.text = angle.ToString();
            }

            tickText.Add(text);
        }

        ticks.Add(new Tick(tickTransform, tickImage, angle));
    }

    void MakeSmallTick(int angle)
    {
        GameObject tickGO = Instantiate(tickSmallPrefab, transform);
        Transform tickTransform = tickGO.transform;
        Image tickImage = tickGO.GetComponent<Image>();

        ticks.Add(new Tick(tickTransform, tickImage, angle));
    }

    float ConvertAngle(float angle)
    {
        if (angle > 180f)
        {
            angle -= 360f;
        }

        return angle;
    }

    void LateUpdate()
    {
        if (jetMechanics == null)
        {
            return;
        }

        float yaw = ConvertAngle(jetMechanics.transform.eulerAngles.y + headingOffsetDegrees);

        foreach (var tick in ticks)
        {
            float angle = Mathf.DeltaAngle(yaw, tick.angle);

            if (Mathf.Abs(angle) <= visibleRangeDegrees)
            {
                Vector3 localPos = tick.transform.localPosition;
                localPos.x = angle * unitsPerDegree;
                tick.transform.localPosition = localPos;
                tick.transform.gameObject.SetActive(true);
            }
            else
            {
                tick.transform.gameObject.SetActive(false);
            }
        }
    }
}