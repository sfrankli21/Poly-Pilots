using UnityEngine;
using TMPro;
using AerodynamicObjects;

public class LiftTracker : MonoBehaviour
{
    [System.Serializable]
    public class LiftItem
    {
        public AeroObject aeroObject;
        public TMP_Text display;
    }

    public LiftItem[] items;
    public TMP_Text netLiftDisplay;
    public int decimals = 2;

    void Update()
    {
        float netLiftN = 0f;

        for (int i = 0; i < items.Length; i++)
        {
            var ao = items[i].aeroObject;
            var txt = items[i].display;

            Vector3 forceN = ao.netAerodynamicLoad.force;        // current AO force in Newtons (engine-side)
            float liftN = Vector3.Dot(forceN, Vector3.up);        // vertical component (+up)
            float liftkN = liftN / 1000f;

            if (txt != null)
                txt.text = FormatKN(liftkN);

            netLiftN += liftN;
        }

        if (netLiftDisplay != null)
            netLiftDisplay.text = FormatKN(netLiftN / 1000f);
    }

    string FormatKN(float value)
    {
        if (decimals < 0) decimals = 0;
        return value.ToString("F" + decimals) + " kN";
    }
}
