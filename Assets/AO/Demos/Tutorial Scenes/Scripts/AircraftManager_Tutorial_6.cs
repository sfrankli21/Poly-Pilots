using UnityEngine;

namespace AerodynamicObjects.Tutorials
{
    public class AircraftManager_Tutorial_6 : MonoBehaviour
    {
        public Transform centreOfMassMarker;
        Rigidbody aircraftRigidBody;
        public float launchSpeed, launchElevation, launchRoll;



        void Start()
        {
            aircraftRigidBody = GetComponent<Rigidbody>();
            aircraftRigidBody.centerOfMass = centreOfMassMarker.localPosition;
            transform.localRotation = Quaternion.Euler(-launchElevation, 0, launchRoll);
            aircraftRigidBody.velocity = transform.TransformDirection(Vector3.forward * launchSpeed);

        }


    }
}