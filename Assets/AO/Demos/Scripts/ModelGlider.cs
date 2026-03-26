using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manager for a model glider.
    /// </summary>
    public class ModelGlider : MonoBehaviour
    {
        public Rigidbody aircraftRigidbody;
        public Transform CoMMarker;
        public ControlSurface rudder, elevator;
        public float elevatorGain, rudderGain;
        float pitchTrim, yawTrim;

        private void Awake()
        {
            aircraftRigidbody.centerOfMass = CoMMarker.localPosition;
        }
    }
}
