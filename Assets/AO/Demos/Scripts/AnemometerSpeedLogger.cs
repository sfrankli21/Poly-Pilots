using TMPro;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Displays the speed measured by an anemometer.
    /// </summary>
    public class AnemometerSpeedLogger : MonoBehaviour
    {
        Rigidbody body;
        public float angularVelocity;
        TMP_Text speed_text;
        float currentWindSpeed, oldWindSpeed, newWindSpeed;

        public float filterCoefficient;

        void Start()
        {
            body = GetComponentInChildren<Rigidbody>();
            speed_text = GetComponentInChildren<TMP_Text>();
        }

        // Update is called once per frame
        void Update()
        {
            angularVelocity = body.angularVelocity.y;
            currentWindSpeed = -4f * angularVelocity * 0.071f; //V=K*omega*R

            newWindSpeed = oldWindSpeed + ((currentWindSpeed - oldWindSpeed) * filterCoefficient);

            newWindSpeed = Mathf.Round(10 * newWindSpeed) / 10; // 1dp
            speed_text.text = "Speed = " + newWindSpeed + "m/s";
            oldWindSpeed = newWindSpeed;
        }
    }
}