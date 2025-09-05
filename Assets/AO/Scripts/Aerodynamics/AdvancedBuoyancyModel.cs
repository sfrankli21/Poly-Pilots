using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// Uses the point of intersection between the fluid and this game object's collider - provided by Unity's physics.
    /// The point of intersection is used to split the ellipsoid body into two sections when the object is partially
    /// submerged in a fluid zone, e.g. when floating on water.
    /// If the object is completely submerged then the volume of the ellipsoid is used to determine the buoyant force acting on the object.
    /// </summary>
    public class AdvancedBuoyancyModel : MonoBehaviour, IAerodynamicModel
    {
        public Rigidbody rb;

        // Checks for how submerged the object is in a fluid zone   
        public bool inFluid = false;

        // This is set to false whenever we're not in a fluid zone
        // It's necessary to make sure the previous values for volumes don't jump when transitioning into a new fluid zone
        public bool initialised = false;

        // If we're inside the bounds, we might not be inside the ellipsoid!
        public bool inBounds;

        public Vector3 scaleToSphere, scaleFromSphere;
        public float sphereRadius;
        public float capHeight;
        public float distanceFromSphereCentre;
        public float remainderVolumeRate;
        public float capVolumeRate;

        public float ellipsoidDimensionProduct;
        public float sphereDiameter3;
        public float sphereDiameter;
        // Point of intersection between the object and the fluid zone
        public Vector3 worldPointOfIntersection, objectPointOfIntersection, sphericalPointOfIntersection;

        // These details are reluctantly given by the physics engine
        public float collisionPenetration;
        public Vector3 worldCollisionNormal, objectCollisionNormal;

        // We need to store our own collider so we can get the penetration between it and the fluid zone we're in contact with
        public Collider thisCollider;

        // Density of the fluid zone
        public float fluidZoneDensity;

        // Keeping track of the current volumes and their positions
        // As well as the previous values.
        // This allows us to apply drag due to added mass as the object displaces the fluid
        public float capVolume, previousCapVolume;
        public float remainderVolume, previousRemainderVolume;
        public Vector3 capCentreOfVolume, previousCapCentreOfVolume;
        public Vector3 remainderCentreOfVolume, previousRemainderCentreOfVolume;
        public Vector3 capVolumeVelocityDirection;
        public Vector3 remainderVolumeVelocityDirection;

        // Does the spherical cap represent the fluid zone?
        public bool capIsInsideFluid;
        // Need to keep track of if the cap used to represent the fluid zone
        // If we transition from it being so to not or vice versa, then we need
        // to take care that the previous positions of the cap and remainder volumes
        // are swapped when the transition occurs
        public bool capWasColliderFluid;

        // Use this to determine if the cap is in the fluid zone or not
        public Vector3 fluidCentre;

        // Forces and moments and such
        public Vector3 objectGravity;
        public Vector3 capForce, capBuoyantForce, capDragForce, capMoment;
        public Vector3 remainderForce, remainderBuoyantForce, remainderDragForce, remainderMoment;

        public Vector3 objectPointOnEllipsoid1, objectPointOnEllipsoid2;
        public Vector3 objectPenetrationCentre;
        public Vector3 objectCollisionOrthogonal;
        public Vector3 objectAxisAlignedCollisionNormal;

        public AeroObject aeroObject;

        private void Reset()
        {
            GetAttachedComponents();
        }

        private void Awake()
        {
            GetAttachedComponents();
        }

        void GetAttachedComponents()
        {
            aeroObject = GetComponent<AeroObject>();
            aeroObject.AddMonoBehaviourModel<AdvancedBuoyancyModel>();
            rb = aeroObject.rb;
            thisCollider = GetComponent<Collider>();
        }

        void GetFluidInfo(Collider other)
        {
            if (other.gameObject.TryGetComponent(out FluidVolume fluidZone))
            {
                fluidZoneDensity = aeroObject.fluid.density;
                inFluid = Physics.ComputePenetration(other, other.transform.position, other.transform.rotation, thisCollider, transform.position, transform.rotation, out worldCollisionNormal, out collisionPenetration);
                fluidCentre = other.transform.position;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            GetFluidInfo(other);
        }

        private void OnTriggerStay(Collider other)
        {
            GetFluidInfo(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.TryGetComponent(out FlowPrimitive _))
            {
                inFluid = false;
                capIsInsideFluid = false;
                initialised = false;
            }
        }

        void GetRegularForce(AeroObject ao, float density)
        {
            // We aren't intersecting a fluid boundary so we only need the volume of the entire ellipsoid
            capVolume = 4f / 3f * Mathf.PI * ao.dimensions.x * ao.dimensions.y * ao.dimensions.z * 0.125f;
            remainderVolume = 0;

            capForce = -density * capVolume * objectGravity;
            remainderForce = Vector3.zero;

            capMoment = Vector3.zero;
            remainderMoment = Vector3.zero;

            // Make sure the visuals can be drawn
            capHeight = 0;
            capCentreOfVolume = Vector3.zero;
            remainderCentreOfVolume = Vector3.zero;
        }

        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            // Get gravity in object frame
            objectGravity = transform.InverseTransformDirection(Physics.gravity);

            // If we're not intersecting a fluid zone
            if (!inFluid)
            {
                initialised = false;
                GetRegularForce(ao, GlobalFluid.FluidProperties.density);
                return new AerodynamicLoad()
                {
                    force = capForce,
                    moment = capMoment
                };
            }

            // Get the point of intersection based on the collider's bounds.
            // This isn't a foolproof method for every collider but it kind of works...
            // It's perfect when using a box collider!
            worldPointOfIntersection = transform.position + Vector3.Scale(worldCollisionNormal.normalized, thisCollider.bounds.extents) - (collisionPenetration * worldCollisionNormal);

            // If the intersection point is not inside the bounds, then we are fully submerged in the fluid
            inBounds = thisCollider.bounds.Contains(worldPointOfIntersection);
            if (!inBounds)
            {
                initialised = false;
                GetRegularForce(ao, fluidZoneDensity);
                return new AerodynamicLoad()
                {
                    force = capForce,
                    moment = capMoment
                };
            }

            // Get the point of intersection in object and body frames of reference
            objectPointOfIntersection = transform.InverseTransformDirection(worldPointOfIntersection - transform.position);
            // Get the collision normal in object and body frames - though we only need it in body frame
            objectCollisionNormal = transform.InverseTransformDirection(worldCollisionNormal);

            sphericalPointOfIntersection = Vector3.Scale(objectPointOfIntersection, scaleToSphere);
            distanceFromSphereCentre = Vector3.Dot(objectPointOfIntersection, objectCollisionNormal);

            // Checking if the cap of the ellipsoid is in the fluid or if the majority of the ellipsoid is inside
            capIsInsideFluid = Vector3.Distance(fluidCentre, worldPointOfIntersection) < Vector3.Distance(fluidCentre, transform.position);

            // XOR - checking that the two conditions are not the same
            // I.e. we have transitioned from/to the cap being in the fluid
            if (capIsInsideFluid ^ capWasColliderFluid)
            {
                // Swap the position values - this prevents a huge spike in the transition
                (previousRemainderCentreOfVolume, previousCapCentreOfVolume) = (previousCapCentreOfVolume, previousRemainderCentreOfVolume);
            }

            capWasColliderFluid = capIsInsideFluid;

            // If we're outside of the sphere then we'll ignore the point
            if (Mathf.Abs(distanceFromSphereCentre) >= sphereRadius)
            {
                initialised = false;
                // If the cap is the fluid zone then when we aren't inside the ellipsoid we can say we're above the fluid zone
                if (capIsInsideFluid)
                {
                    Debug.Log("Applying global fluid density");
                    GetRegularForce(ao, GlobalFluid.FluidProperties.density);
                }
                else
                {
                    Debug.Log("Applying local fluid density");

                    // Otherwise, the point is outside of the ellipsoid, but the ellipsoid itself is submerged!
                    GetRegularForce(ao, fluidZoneDensity);
                }

                return new AerodynamicLoad()
                {
                    force = capForce,
                    moment = capMoment
                };
            }

            // Reaching here means that we're both inside the bounds of the collider to also inside of the spherical representation of the body

            // Distance between the sphere surface and the point of intersection forms the height of a spherical cap
            capHeight = sphereRadius - sphericalPointOfIntersection.magnitude;

            // Make sure we're moving in the correct direction
            objectAxisAlignedCollisionNormal = Vector3.Normalize(Vector3.Scale(objectCollisionNormal, -ao.dimensions));
            if (capIsInsideFluid)
            {
                capCentreOfVolume = sphericalPointOfIntersection - (3f / 8f * capHeight * objectAxisAlignedCollisionNormal);
            }
            else
            {
                capCentreOfVolume = sphericalPointOfIntersection + (3f / 8f * capHeight * objectAxisAlignedCollisionNormal);
            }

            // This is a real fudge factor - I feel like it puts the remainder volume too far back
            // but we need it to move such that it's at the same point as the cap centre of volume when the
            // cap height is equal to the sphere radius (in that case the cap and remainder are both halves of the sphere)
            remainderCentreOfVolume = -(capHeight / sphereRadius) * capCentreOfVolume;

            // At this point I've stopped using separate variables for each frame of reference...
            // Going from sphere to object frame here
            capCentreOfVolume = Vector3.Scale(capCentreOfVolume, scaleFromSphere);
            remainderCentreOfVolume = Vector3.Scale(remainderCentreOfVolume, scaleFromSphere);

            // Calculate the volume of the spherical cap
            capVolume = 1f / 3f * Mathf.PI * capHeight * capHeight * ((3f * sphereRadius) - capHeight);

            // Scale the volume up for the ellipsoid
            capVolume *= ellipsoidDimensionProduct / sphereDiameter3;

            // Get the remaining volume for the other part of the ellipsoid
            remainderVolume = (4f / 3f * Mathf.PI * (0.125f * ellipsoidDimensionProduct)) - capVolume;

            // This stops the very first step from being a big jump
            // If we haven't initialised then the following values will all be zero or whatever they
            // were last time we were in a fluid zone. Make sure they don't jump when we enter a new fluid zone
            if (!initialised)
            {
                previousCapCentreOfVolume = capCentreOfVolume;
                previousCapVolume = capVolume;
                previousRemainderCentreOfVolume = remainderCentreOfVolume;
                previousRemainderVolume = remainderVolume;

                initialised = true;
            }

            // Force due to buoyancy (not including density here as it depends on the cap being in the fluid or not
            capBuoyantForce = -capVolume * objectGravity;
            remainderBuoyantForce = -remainderVolume * objectGravity;

            // Get the drag due to the volume of fluid that is being displaced by the object's motion
            // Again, we're not including the fluid density just yet
            capVolumeVelocityDirection = Vector3.Normalize(transform.InverseTransformDirection(rb.GetRelativePointVelocity(capCentreOfVolume))); // Vector3.Normalize(previousCapCentreOfVolume - capCentreOfVolume);
            capVolumeRate = (capVolume - previousCapVolume) / Time.fixedDeltaTime;

            // This should be 0.5x the values but I'm trying to include more drag for a better look
            capDragForce = -CD * capVolumeRate * capVolumeRate * capVolumeVelocityDirection;

            remainderVolumeVelocityDirection = Vector3.Normalize(transform.InverseTransformDirection(rb.GetRelativePointVelocity(remainderCentreOfVolume))); // Vector3.Normalize(previousRemainderCentreOfVolume - remainderCentreOfVolume);
            remainderVolumeRate = (remainderVolume - previousRemainderVolume) / Time.fixedDeltaTime;

            // This should be 0.5x the values but I'm trying to include more drag for a better look
            remainderDragForce = -CD * remainderVolumeRate * remainderVolumeRate * remainderVolumeVelocityDirection;

            // Add the drag to the buoyancy force
            capForce = capBuoyantForce + capDragForce;
            remainderForce = remainderBuoyantForce + remainderDragForce;

            if (capIsInsideFluid)
            {
                capForce *= fluidZoneDensity;
                remainderForce *= GlobalFluid.FluidProperties.density;
            }
            else
            {
                capForce *= GlobalFluid.FluidProperties.density;
                remainderForce *= fluidZoneDensity;
            }

            // Record the previous volumes and positions of the volumes for the drag calculations
            previousCapCentreOfVolume = capCentreOfVolume;
            previousCapVolume = capVolume;
            previousRemainderCentreOfVolume = remainderCentreOfVolume;
            previousRemainderVolume = remainderVolume;

            // Calculate the moments due to the centre of volumes not being at the centre of mass
            capMoment = Vector3.Cross(capCentreOfVolume, capForce);
            remainderMoment = Vector3.Cross(remainderCentreOfVolume, remainderForce);

            // REMEMBER FORCES ARE IN THE OBJECT FRAME (i.e. Unity's object frame)
            return new AerodynamicLoad()
            {
                force = capForce + remainderForce,
                moment = capMoment + remainderMoment
            };
        }

        public void UpdateDimensionValues(AeroObject ao)
        {
            // The geometry is simplified by scaling down to a sphere with radius equal to the minor axis of the ellipsoid
            sphereDiameter = Mathf.Min(ao.dimensions.x, ao.dimensions.y, ao.dimensions.z);
            sphereDiameter3 = sphereDiameter * sphereDiameter * sphereDiameter;
            sphereRadius = 0.5f * sphereDiameter;
            scaleToSphere = new Vector3(sphereDiameter / ao.dimensions.x, sphereDiameter / ao.dimensions.y, sphereDiameter / ao.dimensions.z);
            scaleFromSphere = new Vector3(ao.dimensions.x / sphereDiameter, ao.dimensions.y / sphereDiameter, ao.dimensions.z / sphereDiameter);
            ellipsoidDimensionProduct = ao.dimensions.x * ao.dimensions.y * ao.dimensions.z;
        }

        private float CD = 0.5f;

        private void OnDrawGizmos()
        {
            //Vector3 worldGeoCentre1 = transform.position + transform.TransformDirection(capCentreOfVolume);
            //Vector3 worldGeoCentre2 = transform.position + transform.TransformDirection(remainderCentreOfVolume);

            //Vector3 worldEllispoid1 = transform.position + transform.TransformDirection(rotationFromBodyFrame * bodyPointOnEllipsoid1);
            //Vector3 worldEllispoid2 = transform.position + transform.TransformDirection(rotationFromBodyFrame * bodyPointOnEllipsoid2);
            //Vector3 penetrationPoint = transform.position + transform.TransformDirection(rotationFromBodyFrame * bodyPointOfIntersection);

            //Gizmos.DrawSphere(worldEllispoid1, 0.1f);
            //Gizmos.DrawSphere(worldEllispoid2, 0.1f);
            //Gizmos.DrawSphere(worldGeoCentre1, 0.1f);
            //Gizmos.DrawSphere(worldGeoCentre2, 0.1f);
            //Gizmos.DrawSphere(penetrationPoint, 0.1f);

            //Gizmos.color = Color.gray;
            //Gizmos.DrawLine(worldGeoCentre1, worldGeoCentre1 + 0.02f * transform.TransformDirection(capBuoyantForce));
            //Gizmos.DrawLine(worldGeoCentre2, worldGeoCentre2 + 0.02f * transform.TransformDirection(remainderBuoyantForce));

            //Gizmos.color = Color.blue;
            //Gizmos.DrawLine(worldGeoCentre1, worldGeoCentre1 + 0.02f * transform.TransformDirection(capDragForce));
            //Gizmos.DrawLine(worldGeoCentre2, worldGeoCentre2 + 0.02f * transform.TransformDirection(remainderDragForce));

            //Gizmos.color = Color.red;
            //Gizmos.DrawLine(worldGeoCentre1, worldGeoCentre1 + transform.TransformDirection(capVolumeVelocityDirection));
            //Gizmos.DrawLine(worldGeoCentre2, worldGeoCentre2 + transform.TransformDirection(remainderVolumeVelocityDirection));
        }
    }
}
