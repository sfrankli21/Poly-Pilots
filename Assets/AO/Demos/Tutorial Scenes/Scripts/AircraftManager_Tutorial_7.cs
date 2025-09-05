using UnityEngine;

namespace AerodynamicObjects.Tutorials
{
    public class AircraftManager_Tutorial_7 : MonoBehaviour
    {
        public Transform centreOfMassMarker;
        Rigidbody aircraftRigidBody;
        [Range(0, 100)]
        public float propSpeed;

        public Transform propHub;

        // Start is called before the first frame update
        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();
            aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;
        }

        void FixedUpdate()
        {
            propHub.localRotation *= Quaternion.Euler(0, 0, -15 * propSpeed * Time.fixedDeltaTime);
        }
    }
}