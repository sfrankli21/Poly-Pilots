using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Rotates a fan hub with angular velocity proportional to the wind speed produced by a Fan Flow Field.
    /// </summary>
    public class FanController : MonoBehaviour
    {
        FanFlowField fanFlowField;
        Transform fanHub;
        /// <summary>
        /// Relates fan rotational speed to the set the flow speed. For graphical effect only. Flow is produced by an area source primitive in this demo, not the motion of the fan.
        /// </summary>
        [Tooltip("Relates fan rotational speed to the set the flow speed. For graphical effect only. Flow is produced by an area source primitive in this demo, not the motion of the fan.")]
        public float fanRotationSpeedToWindSpeed = 1;

        void Start()
        {
            fanFlowField = GetComponentInChildren<FanFlowField>();
            fanHub = transform.Find("Hub");
        }

        // Update is called once per frame
        void Update()
        {
            fanHub.Rotate(0, 0, fanFlowField.windSpeedAtFanFace * fanRotationSpeedToWindSpeed * Time.deltaTime);
        }

        public void ChangeFanSpeed(float newValue)
        {
            fanFlowField.windSpeedAtFanFace = newValue;
        }
    }
}
