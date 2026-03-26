using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Sets the physics fixed time step for the simulation scene. Used for demos which require a smaller time step than Unity's default 0.02 seconds.
    /// </summary>
    public class TimeStepSetter : MonoBehaviour
    {
        public float fixedTimeStep = 0.01f;
        // Start is called before the first frame update
        void Start()
        {
            Debug.Log("Time step is being set by " + name);
            Time.fixedDeltaTime = fixedTimeStep;
        }
    }
}
