using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Destroys the GameObject that this script is attached to after a given amount of time. Uses the fixed update thread.
    /// </summary>
    public class LifeTimer : MonoBehaviour
    {
        public float lifeSpanDuration = 10f;
        float age;

        private void OnEnable()
        {
            age = 0;
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;

            if (age >= lifeSpanDuration)
            {
                Destroy(gameObject);
            }
        }
    }
}
