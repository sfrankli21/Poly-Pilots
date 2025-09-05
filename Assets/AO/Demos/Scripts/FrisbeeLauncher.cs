using AerodynamicObjects.Aerodynamics;
using AerodynamicObjects.Utility;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to periodically launch frisbees with some random variation to launch conditions.
    /// </summary>
    public class FrisbeeLauncher : ObjectSpawner
    {
        public GameObject frisbeePrefab;
        public Transform launchPosition;

        /// <summary>
        /// How long the frisbee exists before it is destoryed. (s)
        /// </summary>
        [Tooltip("How long the frisbee exists before it is destoryed. (s)")]
        [Range(1, 10)]
        public float life = 2.5f;

        /// <summary>
        /// Relative magnitude of variability between each launch
        /// </summary>
        [Tooltip("Relative magnitude of variability between each launch")]
        [Range(0, 1)]
        public float launchVariability = 0.1f;

        /// <summary>
        /// Launch elevation angle, positive up. (degrees)
        /// </summary>
        [Tooltip("Launch elevation angle, positive up. (degrees)")]
        [Range(-10, 90)]
        public float launchPitchAngle = 25;

        /// <summary>
        /// Bank angle of disc at launch. Positive is right hand down. (degrees)
        /// </summary>
        [Tooltip("Bank angle of disc at launch. Positive is right hand down. (degrees)")]
        [Range(-45, 45)]
        public float launchRollAngle = 25;

        /// <summary>
        /// Ratio of the disc rim speed due to its spin rate to the forward velocity of the disc. For a normal frisbee throw the advance ratio is 1,  and the spin rate is equivalent to the rate of rotation of the disc if were rolling along the ground at the speed thrown.
        /// </summary>
        [Tooltip("Ratio of the disc rim speed due to its spin rate to the forward velocity of the disc. For a normal frisbee throw the advance ratio is 1,  and the spin rate is equivalent to the rate of rotation of the disc if were rolling along the ground at the speed thrown.")]
        [Range(0, 2)]
        public float advanceRatio = 1;

        /// <summary>
        /// Speed in m/s.
        /// </summary>
        [Tooltip("Speed in m/s.")]
        [Range(0, 30)]
        public float launchSpeed = 11;

        /// <summary>
        /// All discs are unstable in pitch, however good discs are less unstable than bad discs. A stability margin of 0 represents an ideal disc in which the aerodynamic centre is located at the centre of the disc. For practical discs, the aerodynamic centre will be some distance forward of the centre of the disc. The stability margin is the radial distance between the disc aerodynamic centre and the centre of the disc as a fraction of the disc diameter. It is a negative quantity recognising the disc is always unstable in pitch (the aerodynamic centre is ahead of the centre of mass). Instability in pitch causes the disc to roll during flight due to gyroscopic effects. 
        /// </summary>
        [Tooltip("All discs are unstable in pitch, however good discs are less unstable than bad discs. A stability margin of 0 represents an ideal disc in which the aerodynamic centre is located at the centre of the disc. For practical discs, the aerodynamic centre will be some distance forward of the centre of the disc. The stability margin is the radial distance between the disc aerodynamic centre and the centre of the disc as a fraction of the disc diameter. It is a negative quantity recognising the disc is always unstable in pitch (the aerodynamic centre is ahead of the centre of mass). Instability in pitch causes the disc to roll during flight due to gyroscopic effects. ")]
        [Range(-0.25f, 0)]
        public float stabilityMargin;

        Rigidbody rb;
        const float discRadius = 0.138f;
        GameObject go;
        Transform frisbeeContainerTransform; //container transform for spawned frisbies
        Quaternion launchRotation;

        OrbitalCamera orbitalCamera;

        // Start is called before the first frame update
        void Start()
        {
            frisbeeContainerTransform = new GameObject("Frisbees").transform;
            frisbeeContainerTransform.parent = transform;
            orbitalCamera = FindObjectOfType<OrbitalCamera>();
        }

        public override void Spawn()
        {
            launchRotation = Quaternion.Euler(-launchPitchAngle + (launchVariability * Random.Range(-30f, 30f)), 0, -launchRollAngle + (launchVariability * Random.Range(-30f, 30f)));
            go = Instantiate(frisbeePrefab, launchPosition.position, launchRotation);
            go.transform.parent = frisbeeContainerTransform;

            go.GetComponentInChildren<AeroObject>().GetModel<LiftModel>().aerodynamicCentre_z = -stabilityMargin * discRadius * 2; //measured positive forward from the disc centre

            rb = go.GetComponent<Rigidbody>();

            rb.angularVelocity = go.transform.rotation * new Vector3(0, advanceRatio * launchSpeed / discRadius, 0);

            rb.linearVelocity = go.transform.rotation * new Vector3(0, 0, launchSpeed + (launchVariability * Random.Range(-1f, 1f)));

            go.GetComponent<LifeTimer>().lifeSpanDuration = life;

            if (orbitalCamera != null)
            {
                orbitalCamera.target = go.transform;
            }
        }
    }
}
