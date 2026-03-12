using UnityEngine;

namespace AerodynamicObjects.Tutorials
{
    public class AircraftManager_Tutorial_5 : MonoBehaviour
    {
        public Transform centreOfMassMarker;
        Rigidbody aircraftRigidBody;

        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();


        }
        private void FixedUpdate()
        {
            aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;
        }


    }
}