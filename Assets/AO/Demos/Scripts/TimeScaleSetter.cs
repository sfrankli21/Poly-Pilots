using UnityEngine;

namespace AerodynamicObjects.Demos
{
    public class TimeScaleSetter : MonoBehaviour
    {
        public float timeScale = 1f;

        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Time scale is being controlled by " + name);
            Time.timeScale = timeScale;
        }

        // Update is called once per frame
        void Update()
        {
            Time.timeScale = timeScale;
        }
    }
}
