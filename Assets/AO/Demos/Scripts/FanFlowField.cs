using AerodynamicObjects.Flow;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manages an area source and line source to create a fan with an entrainment effect which keeps objects in the fan's flow.
    /// The same effect seen when a ping pong ball is held in the flow of a hair dryer.
    /// </summary>
    public class FanFlowField : MonoBehaviour
    {
        AreaSource areaSource;
        LineSource lineSource;
        public float windSpeedAtFanFace;
        /// <summary>
        /// Ratio of inflow velocity to fan velocity. Creates effect of drawing surrounding flow in to the fan jet (entrainment).
        /// </summary>
        [Tooltip("Ratio of inflow velocity to fan velocity. Creates effect of drawing surrounding flow in to the fan jet (entrainment).")]
        public float entrainmentStrength = 0.1f;
        // Start is called before the first frame update
        void Start()
        {
            areaSource = GetComponentInChildren<AreaSource>();
            lineSource = GetComponentInChildren<LineSource>();
            lineSource.coreRadius = areaSource.radius;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            areaSource.speed = windSpeedAtFanFace;
            lineSource.sourceStrength = -entrainmentStrength * windSpeedAtFanFace * areaSource.radius;
        }
    }
}
