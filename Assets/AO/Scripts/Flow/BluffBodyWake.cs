using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Uses the drag model on an aero object to produce a turbulent wake for the aero object.
    /// Must be attached to an object with an AeroObject component and the AeroObject must have the default DragModel enabled.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Bluff Body Wake")]
    public class BluffBodyWake : MonoBehaviour
    {
        /// <summary>
        /// The strength of the wake is based on physics so that it is of plausible strength for the body size and speed. The intensity parameter is a multiplier to this. Default value is 1.
        /// </summary>
        [Tooltip("The strength of the wake is based on physics so that it is of plausible strength for the body size and speed. The intensity parameter is a multiplier to this. Default value is 1.")]
        [Range(0f, 3f)]
        public float StrengthMultiplier = 1f;

        /// <summary>
        /// Length of the wake in the downstream direction relative to the size of the body. Computational cost increases in proportion to wake length. The way in which strength varies over the lenght the length of the wake is controlled by the strength over time curve.  
        /// </summary>
        [Tooltip("Length of the wake in the downstream direction relative to the size of the body. Computational cost increases in proportion to wake length. The way in which strength varies over the lenght the length of the wake is controlled by the strength over time curve.  ")]
        [Range(1, 10)]
        public float length = 5;

        /// <summary>
        /// Starting width of the wake relative to the width of the body. Use higher values for bluff bodies like cubes, values closer to 1 for more streamlined bodies. Can be used to increase the size of the wake for effect.
        /// </summary>
        [Tooltip("Starting width of the wake relative to the width of the body. Use higher values for bluff bodies like cubes, values closer to 1 for more streamlined bodies. Can be used to increase the size of the wake for effect.")]
        [Range(0.5f, 2)]
        public float width = 1.0f;

        /// <summary>
        /// The wake vortex shedding frequency is based on a dimensionless physical constant called the Strouhal Number. Higher values mean a faster shedding frequency, and the wake is made up of a greater number of vortex filaments of reduced strength. A lower Strouhal Number means the wake is more lumpy, but at reduced computational cost.
        /// </summary>
        [Tooltip("The wake vortex shedding frequency is based on a dimensionless physical constant called the Strouhal Number. Higher values mean a faster shedding frequency, and the wake is made up of a greater number of vortex filaments of reduced strength. A lower Strouhal Number means the wake is more lumpy, but at reduced computational cost.")]
        [Range(1f, 10)]
        public float StrouhalNumber = 4;

        /// <summary>
        /// The arc length of filaments at spawn time in degrees. Decreasing the arc length reduces the length scale of the turbulence, but increases computational cost for the same strength of wake
        /// </summary>
        [Tooltip("The arc length of filaments at spawn time in degrees. Decreasing the arc length reduces the length scale of the turbulence, but increases computational cost for the same strength of wake")]
        [Range(15, 90)]
        public float filamentArc = 45; // this sets the circumferential length of the vortex filaments

        /// <summary>
        /// Realistic wakes increase in strength just downstream of the body then eventually fade to zero. The time scale is linked to the wake length so that end point of the curve is the end of the wake.
        /// </summary>
        [Tooltip("Realistic wakes increase in strength just downstream of the body then eventually fade to zero. The time scale is linked to the wake length so that end point of the curve is the end of the wake.")]
        public AnimationCurve strengthOverTime = new AnimationCurve(new Keyframe[] { new(0, 0), new(.1f, 1), new(1, 0) });

        /// <summary>
        /// Spawn wake for upper half of body only (y local > 0). Useful for making more efficient ground vehicle wakes.
        /// </summary>
        [Tooltip("Spawn wake for upper half of body only (y local > 0). Useful for making more efficient ground vehicle wakes.")]
        public bool halfWake; // Note that this assumes that the body producing the wake is moving in the horizontal plane, and that it is the top half of the body that is exposed.

        [Tooltip("Container transform for the wake filament objects.")]
        public Transform parentTransformForWakeObjects;

        /// <summary>
        /// The drag coefficient for the wake is obtained from the AeroObject.
        /// </summary>
        private float dragCoefficient;

        float spawnRate; // this is the rate at which vortex elements are spawned in spawns/second
        float flowSpeed; //obtained from flow sensor
        float life;// life span of vortex filaments in seconds
        float timer = 0; // spawn timer
        private VortexFilament filament;
        Vector3 startNodePosition, endNodePosition;
        Transform filamentContainerTransform;
        Vector3 relativeFluidVelocity;// This is obtained from a sibling aeroObject if it exists, or a flow sensor which will be added if necessary
        AeroObject ao;
        DisplacementBody displacementBody;

        //public FlowSensor flowSensor;
        void Start()
        {
            displacementBody = GetComponent<DisplacementBody>();
            ao = GetComponent<AeroObject>();

            filamentContainerTransform = new GameObject("Bluff Body Wake Filaments").transform;//create container for spawned filaments
            filamentContainerTransform.parent = parentTransformForWakeObjects;
        }

        float centreAngle, spawnArcRadians, spawnAngle;
        float sizeReference;
        Quaternion localVelocityRotation;
        Vector3 circularSpawnPosition;
        void FixedUpdate()
        {
            dragCoefficient = ao.GetDragCoefficient();

            relativeFluidVelocity = ao.relativeVelocity;

            sizeReference = ao.dimensions.magnitude;

            flowSpeed = relativeFluidVelocity.magnitude;// Mathf.Clamp(relativeFluidVelocity.magnitude, 0f, 100f); // It is unfortunately necessary to clamp the max speed because the the initial speed obtained from the flow sensor when using the transform position can initially be very high due to a numerical transient in the differentiation.

            // Ignore tiny speeds as they produce infinite life
            if (flowSpeed < 0.001f)
            {
                return;
            }

            //set the life of the particles based on user supplied wake length, the approximate size of the body and free stream speed
            life = length * sizeReference / flowSpeed;

            //Set spawn rate based on Strouhal number  https://en.wikipedia.org/wiki/Strouhal_number. Factor by the user supplied frequency scale parameter
            spawnRate = StrouhalNumber * flowSpeed / sizeReference;

            if (halfWake)
            {
                spawnRate /= 2; // divide spawn rate by 2 if only half wake is being used
            }

            timer += Time.fixedDeltaTime;
            if (timer >= 1f / spawnRate)
            {
                spawnArcRadians = Mathf.Deg2Rad * (filamentArc / 2f);

                if (halfWake)
                {
                    centreAngle = Random.Range(spawnArcRadians, Mathf.PI - spawnArcRadians); // angular location of filament start node
                }
                else
                {
                    centreAngle = Random.Range(0, 2f * Mathf.PI);
                }

                localVelocityRotation = Quaternion.LookRotation(ao.localRelativeVelocity);

                spawnAngle = centreAngle + spawnArcRadians;
                circularSpawnPosition = localVelocityRotation * new Vector3(0.5f * width * Mathf.Cos(spawnAngle), 0.5f * width * Mathf.Sin(spawnAngle), 0);
                startNodePosition = transform.TransformPoint(circularSpawnPosition);

                spawnAngle = centreAngle - spawnArcRadians;
                circularSpawnPosition = localVelocityRotation * new Vector3(0.5f * width * Mathf.Cos(spawnAngle), 0.5f * width * Mathf.Sin(spawnAngle), 0);
                endNodePosition = transform.TransformPoint(circularSpawnPosition);

                filament = CreateFlowPrimitiveTools.CreateVortexFilament(startNodePosition, endNodePosition, true, life, StrengthMultiplier * GetFilamentStrength(), strengthOverTime, 0.1f, filamentContainerTransform);
                if (displacementBody)
                {
                    filament.startNode.GetComponent<MoveWithFlow>().IgnoreInteraction(displacementBody);
                    filament.endNode.GetComponent<MoveWithFlow>().IgnoreInteraction(displacementBody);
                }

                filament.IgnoreInteraction(ao);

                timer -= 1f / spawnRate;
            }
        }

        float GetFilamentStrength()
        {
            //The strength of the filament is the strenght of a complete vortex ring multiplied by the fraction of ring being spawned. I.e. if you are spawning part of a ring it needs to be stronger.
            return Random.Range(0.5f, 1.5f) * (360 / filamentArc) * dragCoefficient * flowSpeed * flowSpeed / (2 * spawnRate);

        }
    }
}
