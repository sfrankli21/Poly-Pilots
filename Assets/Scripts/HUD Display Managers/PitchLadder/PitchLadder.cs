using System.Collections.Generic;
using UnityEngine;

public class PitchLadder : MonoBehaviour
{
    [SerializeField]
    GameObject pitchHorizonPrefab;
    [SerializeField]
    GameObject pitchPositivePrefab;
    [SerializeField]
    GameObject pitchNegativePrefab;
    [SerializeField]
    int barInterval = 5;
    [SerializeField]
    int range = 90;
    [SerializeField]
    float unitsPerDegree = 0.02f;
    [SerializeField]
    float visibleRangeDegrees = 30f;
    [SerializeField]
    JetMechanics jetMechanics;
    [SerializeField]
    float pitchOffsetDegrees;
    [SerializeField]
    float rollOffsetDegrees;
    [SerializeField]
    float verticalOffset;

    struct Bar
    {
        public Transform transform;
        public float angle;
        public PitchBar bar;

        public Bar(Transform transform, float angle, PitchBar bar)
        {
            this.transform = transform;
            this.angle = angle;
            this.bar = bar;
        }
    }

    List<Bar> bars;

    void Start()
    {
        bars = new List<Bar>();

        for (int i = -range; i <= range; i++)
        {
            if (i % barInterval != 0)
            {
                continue;
            }

            if (i == 0 || i == 90 || i == -90)
            {
                CreateBar(i, pitchHorizonPrefab);
            }
            else if (i > 0)
            {
                CreateBar(i, pitchPositivePrefab);
            }
            else
            {
                CreateBar(i, pitchNegativePrefab);
            }
        }
    }

    public void SetJetMechanics(JetMechanics jet)
    {
        jetMechanics = jet;
    }

    public void UpdateColor(Color color)
    {
        foreach (var bar in bars)
        {
            if (bar.bar != null)
            {
                bar.bar.UpdateColor(color);
            }
        }
    }

    void CreateBar(int angle, GameObject prefab)
    {
        GameObject barGO = Instantiate(prefab, transform);
        Transform barTransform = barGO.transform;
        PitchBar bar = barGO.GetComponent<PitchBar>();

        if (bar != null)
        {
            bar.SetNumber(angle);
        }

        bars.Add(new Bar(barTransform, angle, bar));
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

        float pitch = -ConvertAngle(jetMechanics.transform.eulerAngles.x) + pitchOffsetDegrees;
        float roll = ConvertAngle(jetMechanics.transform.eulerAngles.z) + rollOffsetDegrees;

        transform.localEulerAngles = new Vector3(0f, 0f, -roll);

        foreach (var bar in bars)
        {
            float angleDifference = Mathf.DeltaAngle(pitch, bar.angle);

            if (Mathf.Abs(angleDifference) <= visibleRangeDegrees)
            {
                Vector3 localPos = bar.transform.localPosition;
                localPos.y = (angleDifference * unitsPerDegree) + verticalOffset;
                bar.transform.localPosition = localPos;
                bar.transform.gameObject.SetActive(true);

                if (bar.bar != null)
                {
                    bar.bar.UpdateRoll(roll);
                }
            }
            else
            {
                bar.transform.gameObject.SetActive(false);
            }
        }
    }
}