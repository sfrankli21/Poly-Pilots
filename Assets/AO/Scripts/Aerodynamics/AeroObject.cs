
using AerodynamicObjects.Aerodynamics;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to calculate the aerodynamic forces and moments acting on an object based on geometry and flow velocity.
    /// </summary>
    [Serializable]
    [AddComponentMenu("Aerodynamic Objects/Aerodynamics/Aero Object")]
    public class AeroObject : FlowSensor
    {
        public bool hasDrag;
        public bool hasLift;
        public bool hasRotationalDamping;
        public bool hasRotationalLift;
        public bool hasBuoyancy;

        [Tooltip("Set to true if the dimensions of this object will change during play mode. Having this set to false saves a fairly costly update.")]
        public bool updateDimensionsInRuntime = false;

        /// <summary>
        /// Used to describe what shape the reference area of an aerodynamic object is.
        /// </summary>
        public enum ReferenceAreaShape
        {
            Ellipse,
            Rectangle,
            Mesh // NEW
        }

        [Tooltip("Ellipse/Rectangle are built-in; Mesh uses the assigned MeshFilter for projected areas.")]
        public ReferenceAreaShape referenceAreaShape = ReferenceAreaShape.Ellipse;

        [Tooltip("The aerodynamic group that this object belongs to. Groups control the aspect ratio when aero objects are used to create a wing, ensuring that sensible values of lift are produced.")]
        public AeroGroup myGroup;

        // ==== Mesh reference area support ====
        [Header("Custom Mesh Reference Area")]
        [Tooltip("When Reference Area Shape = Mesh, assign the MeshFilter to use for this instance.")]
        public MeshFilter meshFilter;

        [Tooltip("Local-space normal used to define the planform projection plane (wing area).")]
        public Vector3 planformNormal = Vector3.up;

        [Tooltip("Fallback local-space normal used for frontal area when airspeed is near zero.")]
        public Vector3 frontalNormalHint = Vector3.forward;

        [Header("Mesh AR Overrides (optional)")]
        [Tooltip("If > 0, overrides span used for aspect ratio when using Mesh reference areas.")]
        public float meshSpanOverride = -1f;

        [Tooltip("If > 0, overrides chord used for aspect ratio when using Mesh reference areas.")]
        public float meshChordOverride = -1f;

        // ================= Camber and Controls ===================
        [Obsolete("Body Camber is no longer stored as a single value, it is now defined for each axis of the object independently.", true)]
        public float BodyCamber;

        public List<ControlSurface> controlSurfaces = new List<ControlSurface>();

        [Obsolete("Total Camber is no longer used. Control surfaces now contribute a chord and angle of deflection and are used directly by the lift and drag models.", true)]
        public float TotalCamber => BodyCamber + ControlCamber;

        [Obsolete("Control Camber is no longer used. Control surfaces now contribute a chord and angle of deflection and are used directly by the lift and drag models.", true)]
        public float ControlCamber;

        [Tooltip("Overall size of the object in local frame (m)")]
        public Vector3 dimensions;

        [Tooltip("Relative dimensions (scaled by transform).")]
        public Vector3 relativeDimensions = Vector3.one;

        [Tooltip("Camber along each local axis (m).")]
        public Vector3 camber;

        public IAerodynamicModel[] aerodynamicModels = new IAerodynamicModel[0];
        public AerodynamicLoad[] aerodynamicLoads = new AerodynamicLoad[0];
        public AerodynamicLoad netAerodynamicLoad = new AerodynamicLoad();

        /// <summary>
        /// The dynamic pressure due to motion of the object through the fluid.
        /// Given by 0.5 * rho * V^2 (Pa)
        /// </summary>
        public float dynamicPressure;

        public override void Awake()
        {
            base.Awake();
            Initialise();

            if (velocitySource == VelocitySource.Rigidbody && rb == null)
            {
                Debug.LogWarning("Expecting a rigidbody component to be assigned for the AeroObject on " + gameObject.name + " for the velocity source, but one has not been assigned.");
            }
        }

        public void OnValidate() { UpdateDimensions(); }
        private void Reset() { AddCheckedModels(); }

        /// <summary>
        /// Adds the specified models to the object and performs any dimension-related calculations.
        /// Also stores the current transform position and rotation ready for use in velocity calculations which use the object's transform.
        /// </summary>
        public void Initialise()
        {
            AddCheckedModels();
            UpdateDimensions();
            previousPosition = transform.position;
            previousRotation = transform.rotation;
        }

        Vector3 groupSpanAxis;
        public Vector3 groupDimensions;

        /// <summary>
        /// Update the dimensions for the aero object using its transform scale and the relative dimensions.
        /// Then update the dimension values for any aerodynamic models associated with this aero object.
        /// </summary>
        public void UpdateDimensions()
        {
            dimensions = Vector3.Scale(transform.lossyScale, relativeDimensions);

            if (myGroup != null)
            {
                groupSpanAxis = transform.InverseTransformDirection(myGroup.GlobalSpanAxis);
                groupDimensions = dimensions - (Vector3.Dot(dimensions, groupSpanAxis) * groupSpanAxis) + (myGroup.span * groupSpanAxis);
            }
            else
            {
                groupDimensions = dimensions;
            }

            if (aerodynamicModels == null || aerodynamicModels.Length == 0) return;
            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                aerodynamicModels[i].UpdateDimensionValues(this);
            }
        }

        public void SetVelocity(Vector3 velocity)
        {
            this.velocity = velocity;
            localVelocity = transform.InverseTransformDirection(velocity);
            angularVelocity = Vector3.zero;
            localAngularVelocity = Vector3.zero;
        }

        public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
        {
            transform.SetPositionAndRotation(position, rotation);
            previousPosition = position;
            previousRotation = rotation;
        }

        public Vector3 GlobalNetForce() => transform.TransformDirection(netAerodynamicLoad.force);
        public Vector3 LocalNetForce() => netAerodynamicLoad.force;
        public Vector3 GlobalNetTorque() => transform.TransformDirection(netAerodynamicLoad.moment);
        public Vector3 LocalNetTorque() => netAerodynamicLoad.moment;

        public (Vector3 lift, Vector3 drag) GetLocalLiftAndDrag()
        {
            Vector3 windDirection = localRelativeVelocity.normalized;
            Vector3 drag = Vector3.Dot(netAerodynamicLoad.force, windDirection) * windDirection;
            Vector3 lift = netAerodynamicLoad.force - drag;
            return (lift, drag);
        }

        public (Vector3 lift, Vector3 drag) GetGlobalLiftAndDrag()
        {
            Vector3 windDirection = relativeVelocity.normalized;
            Vector3 drag = Vector3.Dot(transform.TransformDirection(netAerodynamicLoad.force), windDirection) * windDirection;
            Vector3 lift = netAerodynamicLoad.force - drag;
            return (lift, drag);
        }

        public float GetDragCoefficient()
        {
            if (!hasDrag) return 0f;
            return GetModel<DragModel>().GetDragCoefficient();
        }

        public float GetDragCoefficient(float referenceArea)
        {
            return Vector3.Dot(netAerodynamicLoad.force, localRelativeVelocity.normalized) / (dynamicPressure * referenceArea);
        }

        public float GetLiftCoefficient()
        {
            if (!hasLift) return 0f;
            return GetModel<LiftModel>().CL;
        }

        public float GetLiftCoefficient(float referenceArea)
        {
            Vector3 windDirection = localRelativeVelocity.normalized;
            return (netAerodynamicLoad.force - (Vector3.Dot(netAerodynamicLoad.force, windDirection) * windDirection)).magnitude / (dynamicPressure * referenceArea);
        }

        public Vector3 GetAerodynamicCentreGlobalPosition()
        {
            if (!hasLift) return transform.position;
            return transform.position + transform.TransformDirection(GetModel<LiftModel>().GetLocalAerodynamicCentre());
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (updateDimensionsInRuntime) UpdateDimensions();
            RunAerodynamics();

            if (rb)
            {
                rb.AddForceAtPosition(GlobalNetForce(), transform.position);
                rb.AddTorque(GlobalNetTorque());
            }
        }

        /// <summary>
        /// Calculate the dynamic pressure, based on the body relative velocity.
        /// </summary>
        public void GetDynamicPressure()
        {
            dynamicPressure = 0.5f * fluid.density * localRelativeVelocity.sqrMagnitude;
        }

        /// <summary>
        /// Iterate through all of the attached aerodynamic models and store the forces they provide.
        /// </summary>
        private void ComputeAerodynamicForces()
        {
            netAerodynamicLoad.force.x = 0; netAerodynamicLoad.force.y = 0; netAerodynamicLoad.force.z = 0;
            netAerodynamicLoad.moment.x = 0; netAerodynamicLoad.moment.y = 0; netAerodynamicLoad.moment.z = 0;

            if (aerodynamicModels == null || aerodynamicModels.Length == 0) return;

            if (aerodynamicLoads.Length != aerodynamicModels.Length)
                aerodynamicLoads = new AerodynamicLoad[aerodynamicModels.Length];

            for (int i = 0; i < aerodynamicModels.Length; i++)
            {
                aerodynamicLoads[i] = aerodynamicModels[i].GetAerodynamicLoad(this);
                netAerodynamicLoad.force += aerodynamicLoads[i].force;
                netAerodynamicLoad.moment += aerodynamicLoads[i].moment;
            }
        }

        /// <summary>
        /// Perform all calculations to find aerodynamic forces based on the state of the object and surrounding fluid.
        /// </summary>
        public void RunAerodynamics()
        {
            GetRelativeVelocities();
            GetDynamicPressure();
            ComputeAerodynamicForces();
        }

        public void AddMonoBehaviourModel<T>() where T : MonoBehaviour, IAerodynamicModel, new()
        {
            if (gameObject.TryGetComponent<T>(out T modelComponent))
            {
                if (aerodynamicModels == null || aerodynamicModels.Length == 0)
                {
                    aerodynamicModels = new IAerodynamicModel[] { modelComponent };
                    return;
                }

                for (int i = 0; i < aerodynamicModels.Length; i++)
                {
                    if (aerodynamicModels[i].GetType() == typeof(T))
                    {
                        if (((T)aerodynamicModels[i]).Equals(modelComponent)) return;
                    }
                }

                Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
                aerodynamicModels[aerodynamicModels.Length - 1] = modelComponent;
            }
            else
            {
                modelComponent = gameObject.AddComponent<T>();
                if (aerodynamicModels == null || aerodynamicModels.Length == 0)
                {
                    aerodynamicModels = new IAerodynamicModel[] { modelComponent };
                    return;
                }
                Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
                aerodynamicModels[aerodynamicModels.Length - 1] = modelComponent;
            }
        }

        public void AddModel<T>() where T : IAerodynamicModel, new()
        {
            if (aerodynamicModels == null || aerodynamicModels.Length == 0)
            {
                aerodynamicModels = new IAerodynamicModel[] { new T() };
                return;
            }

            for (int i = 0; i < aerodynamicModels.Length; i++)
                if (aerodynamicModels[i].GetType() == typeof(T)) return;

            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length + 1);
            aerodynamicModels[aerodynamicModels.Length - 1] = new T();
        }

        public void RemoveMonoBehaviourModel<T>() where T : MonoBehaviour, IAerodynamicModel, new()
        {
            if (gameObject.TryGetComponent(out T component))
                DestroyImmediate(component);

            if (aerodynamicModels == null || aerodynamicModels.Length == 0) return;

            int removeID = -1;
            for (int i = 0; i < aerodynamicModels.Length; i++)
                if (aerodynamicModels[i].GetType() == typeof(T)) { removeID = i; break; }

            if (removeID < 0) return;

            for (int i = removeID; i < aerodynamicModels.Length - 1; i++)
                aerodynamicModels[i] = aerodynamicModels[i + 1];

            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length - 1);
        }

        public void RemoveModel<T>() where T : IAerodynamicModel, new()
        {
            if (aerodynamicModels == null || aerodynamicModels.Length == 0) return;

            int removeID = -1;
            for (int i = 0; i < aerodynamicModels.Length; i++)
                if (aerodynamicModels[i].GetType() == typeof(T)) { removeID = i; break; }

            if (removeID < 0) return;

            for (int i = removeID; i < aerodynamicModels.Length - 1; i++)
                aerodynamicModels[i] = aerodynamicModels[i + 1];

            Array.Resize(ref aerodynamicModels, aerodynamicModels.Length - 1);
        }

        public void ClearModels() { aerodynamicModels = new IAerodynamicModel[0]; }

        public T GetModel<T>() where T : IAerodynamicModel
        {
            for (int i = 0; i < aerodynamicModels.Length; i++)
                if (aerodynamicModels[i].GetType() == typeof(T)) return (T)aerodynamicModels[i];
            return default;
        }

        public int GetModelIndex<T>() where T : IAerodynamicModel
        {
            for (int i = 0; i < aerodynamicModels.Length; i++)
                if (aerodynamicModels[i].GetType() == typeof(T)) return i;
            return -1;
        }

        private void AddCheckedModels()
        {
            if (hasDrag) AddModel<DragModel>();
            if (hasLift) AddModel<LiftModel>();
            if (hasRotationalDamping) AddModel<RotationalDampingModel>();
            if (hasRotationalLift) AddModel<RotationalLiftModel>();
            if (hasBuoyancy) AddModel<BuoyancyModel>();
            aerodynamicLoads = new AerodynamicLoad[aerodynamicModels.Length];
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.blue;

            switch (referenceAreaShape)
            {
                case ReferenceAreaShape.Ellipse:
                    Gizmos.DrawWireMesh(GizmoMeshLoader.EllipsoidMesh, Vector3.zero, Quaternion.identity, relativeDimensions);
                    break;
                case ReferenceAreaShape.Rectangle:
                    Gizmos.DrawWireMesh(GizmoMeshLoader.CubeMesh, Vector3.zero, Quaternion.identity, relativeDimensions);
                    break;
                case ReferenceAreaShape.Mesh:
                    if (meshFilter != null && meshFilter.sharedMesh != null)
                    {
                        Matrix4x4 old = Gizmos.matrix;
                        Gizmos.matrix = meshFilter.transform.localToWorldMatrix;
                        Gizmos.DrawWireMesh(meshFilter.sharedMesh, Vector3.zero, Quaternion.identity, Vector3.one);
                        Gizmos.matrix = old;
                    }
                    else
                    {
                        Gizmos.DrawWireMesh(GizmoMeshLoader.CubeMesh, Vector3.zero, Quaternion.identity, relativeDimensions);
                    }
                    break;
                default:
                    break;
            }
        }
#endif

        // ===================== Surface Area ========================

        /// <summary>
        /// The approximate surface area of the ellipsoid  (m^2)
        /// </summary>
        public float GetEllipsoidSurfaceArea()
        {
            return 4f * Mathf.PI * Mathf.Pow(1f / 3f * (Mathf.Pow(0.25f * dimensions.x * dimensions.y, 1.6f)
                                                              + Mathf.Pow(0.25f * dimensions.x * dimensions.z, 1.6f)
                                                              + Mathf.Pow(0.25f * dimensions.y * dimensions.z, 1.6f)), 1f / 1.6f);
        }

        /// <summary>
        /// Compute projected area of the assigned mesh onto a plane with the given local-space normal.
        /// Returns 0 if no mesh is assigned.
        /// </summary>
        public float GetMeshProjectedArea(Vector3 planeNormalLocal)
        {
            if (meshFilter == null) return 0f;
            var mesh = meshFilter.sharedMesh;
            if (mesh == null) return 0f;

            var verts = mesh.vertices;
            var tris = mesh.triangles;

            Vector3 n = planeNormalLocal.normalized;
            Vector3 s = meshFilter.transform.lossyScale;

            float area = 0f;

            for (int i = 0; i < tris.Length; i += 3)
            {
                Vector3 a = Vector3.Scale(verts[tris[i]], s);
                Vector3 b = Vector3.Scale(verts[tris[i + 1]], s);
                Vector3 c = Vector3.Scale(verts[tris[i + 2]], s);

                Vector3 cross = Vector3.Cross(b - a, c - a);
                float mag = cross.magnitude;
                if (mag <= 1e-8f) continue;

                float triArea = 0.5f * mag;
                float cos = Mathf.Abs(Vector3.Dot(cross / mag, n));
                area += triArea * cos;
            }

            return area;
        }
    }
}
