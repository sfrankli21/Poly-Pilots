using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Used to hold an object in place and measure forces and moments acting on the object through a configurable joint.
    /// </summary>
    public class Gimbal : MonoBehaviour
    {
        public bool active = false;
        [Header("Motion Settings")]
        public ConfigurableJointMotion rollMotion;
        public ConfigurableJointMotion pitchMotion;
        public ConfigurableJointMotion yawMotion;

        ConfigurableJoint joint;

        [Space(10), Header("Force Measurements")]
        public Vector3 netGlobalForce;
        public Vector3 netGlobalMoment;
        public Vector3 netLocalForce;
        public Vector3 netLocalMoment;

        public float netForce;
        public float netMoment;

        public Vector3 eulerRotation = Vector3.zero;

        // Start is called before the first frame update
        void Awake()
        {
            eulerRotation = transform.eulerAngles;
            if (active)
            {
                AddJoint();
            }
        }

        void AddJoint()
        {
            joint = gameObject.AddComponent<ConfigurableJoint>();

            joint.xMotion = ConfigurableJointMotion.Locked;
            joint.yMotion = ConfigurableJointMotion.Locked;
            joint.zMotion = ConfigurableJointMotion.Locked;

            SetJointProperties();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (!active)
            {
                return;
            }

            SetJointProperties();

            ReadForces();
        }

        void SetJointProperties()
        {
            if (!active || joint == null)
            {
                return;
            }

            joint.anchor = GetComponent<Rigidbody>().centerOfMass;

            joint.angularXMotion = pitchMotion;
            joint.angularYMotion = yawMotion;
            joint.angularZMotion = rollMotion;
        }

        public void SetRotation(Vector3 _eulerRotation)
        {
            eulerRotation = _eulerRotation;

            SetRotation();
        }

        public void SetRotation()
        {
            if (joint == null)
            {
                joint = GetComponent<ConfigurableJoint>();
            }

            if (joint != null)
            {
                DestroyImmediate(joint);
            }

            transform.eulerAngles = eulerRotation;
            AddJoint();
        }

        public void ReadForces()
        {
            // Read forces from the joint
            netGlobalForce = -joint.currentForce;
            netGlobalMoment = -joint.currentTorque;
            netLocalForce = transform.InverseTransformDirection(netGlobalForce);
            netLocalMoment = transform.InverseTransformDirection(netGlobalMoment);

            netForce = netGlobalForce.magnitude;
            netMoment = netGlobalMoment.magnitude;
        }
    }
}
