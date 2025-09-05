using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Example controller for the main rotor of a helicopter.
    /// </summary>
    public class MainRotorController : MonoBehaviour
    {

        public Rigidbody hubrb, fuselagerb;
        [HideInInspector]
        public float angularVelocityDemand, avGain;
        float torque;
        [HideInInspector]
        public float collective;
        [HideInInspector]
        public float phase;
        [HideInInspector]
        public float cyclic;
        public Transform[] blades;
        Transform[] bladePanels;
        AeroObject[] aeroObjects;
        float[] startPhase;
        //[HideInInspector]
        public float hubAngularVelocity;
        float bladePitchAngle;
        [HideInInspector]
        public HingeJoint mainRotorRevoluteJoint;

        void Start()
        {
            mainRotorRevoluteJoint = hubrb.transform.GetComponent<HingeJoint>(); // main rotor shaft revolute joint
            startPhase = new float[blades.Length];
            bladePanels = new Transform[blades.Length];
            aeroObjects = new AeroObject[blades.Length];

            for (int i = 0; i < blades.Length; i++)
            {
                startPhase[i] = blades[i].localEulerAngles.y; // Need to know the initial blade azimuth angle to correctly apply the cyclic phase
                aeroObjects[i] = blades[i].GetComponentInChildren<AeroObject>();
                bladePanels[i] = aeroObjects[i].transform.parent; // This is the blade geometry object to which the aerodynamics is attached

            }
        }

        void FixedUpdate()
        {

            hubAngularVelocity = mainRotorRevoluteJoint.velocity * Mathf.Deg2Rad;
            torque = (angularVelocityDemand - hubAngularVelocity) * avGain;
            hubrb.AddRelativeTorque(0, torque, 0);

            // adjust blade pitch angles depending on user input
            for (int i = 0; i < blades.Length; i++)
            {
                bladePitchAngle = collective - (cyclic * Mathf.Cos((mainRotorRevoluteJoint.angle + startPhase[i] + phase) * Mathf.Deg2Rad));
                bladePanels[i].localRotation = Quaternion.Euler(new Vector3(bladePitchAngle, 0, 0)); //Rotate the blade panel geometry according  to the combined collective and cylcic control inputs
            }
        }
    }
}
