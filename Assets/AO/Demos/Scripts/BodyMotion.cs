using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to move the cars in the ground vehicle wakes demo scene.
    /// </summary>
    public class BodyMotion : MonoBehaviour
    {
        /// <summary>
        /// Use to control relative size of movement
        /// </summary>
        [Tooltip("Use to control relative size of movement")]
        public float positionAmplitude = 10;
        /// <summary>
        /// Use to control relative speed of movement
        /// </summary>
        [Tooltip("Use to control relative speed of movement")]
        public float velocityAmplitude = 2;
        /// <summary>
        /// Rate at which position amplitude changes over time.
        /// </summary>
        [Tooltip("Rate at which position amplitude changes over time.")]
        public float positionAmplitudeFrequency;
        /// <summary>
        /// Rate at which velocity amplitude changes over time.
        /// </summary>
        [Tooltip("Rate at which velocity amplitude changes over time.")]
        public float velocityAmplitudeFrequency;
        float speed, radius;
        public enum motionType
        {
            circular,
            linear,
            rotary,
            size
        }
        public motionType type = motionType.linear;
        float angle;
        Vector3 startPosition;
        // Start is called before the first frame update
        void Start()
        {
            startPosition = transform.position;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            speed = velocityAmplitude * Mathf.Cos(velocityAmplitudeFrequency * Time.time);
            radius = positionAmplitude * Mathf.Cos(positionAmplitudeFrequency * Time.time);

            switch (type)
            {
                case motionType.circular:
                    angle += speed / radius * Time.fixedDeltaTime;
                    transform.position = startPosition - (radius * Vector3.right) + (radius * new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)));
                    transform.rotation = Quaternion.Euler(0, -Mathf.Rad2Deg * angle, 0);
                    break;
                case motionType.linear:
                    angle += speed / radius * Time.fixedDeltaTime;
                    transform.position = startPosition + (radius * new Vector3(0, 0, Mathf.Sin(angle)));
                    transform.rotation = Quaternion.identity;
                    break;
                case motionType.rotary:
                    angle += speed * Time.fixedDeltaTime;
                    transform.rotation = Quaternion.Euler(angle, 0, 0);
                    break;
                case motionType.size:
                    transform.localScale = 0.5f * (radius + positionAmplitude + .25f) * Vector3.one;
                    break;

            }
        }
    }
}