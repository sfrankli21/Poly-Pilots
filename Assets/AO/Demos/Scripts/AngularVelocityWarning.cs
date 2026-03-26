using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Warns the user when their physics setting for the max angular speed of rigidbodies is too low.
    /// Particularly important in demo scenes with propellers or objects which will rotate quickly.
    /// </summary>
    public class AngularVelocityWarning : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {
            if (Physics.defaultMaxAngularSpeed < 10000)
            {
                Debug.Log(Physics.defaultMaxAngularSpeed);
                Debug.LogError("Max Angular Speed is too low. Please go to: Edit > Project Settings > Physics > Default Max Angular Speed, and set the value to something greater than 10,000. Consider using Infinity.");
            }
        }
    }
}
