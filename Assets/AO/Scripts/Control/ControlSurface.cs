using System;
using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// A control surface is defined as an articulated region (flap) on an aero object that can be deflected to change the aerodynamic properties of the body.
    /// A typical example would be an elevator on the trailing edge of a tailplane which, when deflected, changes the lift on the tailplane.
    /// Control surfaces assume that the AeroObject they are controlling is attached to the same GameObject as they are.
    /// </summary>
    [Serializable]
    [AddComponentMenu("Aerodynamic Objects/Control/Control Surface")]
    public class ControlSurface : MonoBehaviour
    {
        // Need to fix the hinge rotation for control surface so that the attached transform is rotated about the correct place
        [Serializable]
        public enum ControlSurfaceAxis
        {
            Forward,
            Back,
            Right,
            Left,
            Up,
            Down
        }

        public ControlSurfaceAxis Axis
        {
            get => axis;
            set
            {
                axis = value;

                UpdateAxes();
            }
        }

        [SerializeField, HideInInspector]
        private ControlSurfaceAxis axis;

        /// <summary>
        /// Forward direction of the control surface. Surfaces are assumed to act at the rear of an object.
        /// </summary>
        [SerializeField, HideInInspector]
        public Vector3 forwardAxis = Vector3.forward;

        /// <summary>
        /// Rotation axis of the control surface. Surfaces are assumed to act at the rear of an object.
        /// </summary>
        [SerializeField, HideInInspector]
        public Vector3 rotationAxis = Vector3.right;
        AeroObject aeroObject;

        const float liftEffectivenessScale = 0.05f;

        /// <summary>
        /// The ratio of the control surface's chord to the overall chord of the Aerodynamic Object (inclusive of the control surface).
        /// This value is limited to half of the overall chord of the object, anything larger than this would be equivalent to the control surface acting in the opposite direction.
        /// </summary>
        [Range(0f, 0.5f)]
        public float surfaceChordRatio = 0.2f;

        [HideInInspector]
        public Transform controlSurfaceHinge;

        [Tooltip("Assign an existing control surface graphic here. If one is assigned, the control surface will create a hinge and rotate the graphic according to the deflection angle.")]
        public Transform controlSurfaceGraphic;

        float theta_f;
        float delta_alpha;
        float surfaceChordRatioScaling;

        /// <summary>
        /// The deflection angle of the control surface. (radians)
        /// </summary>
        [Range(-1f, 1f)]
        public float deflectionAngle;

        /// <summary>
        /// The deflection angle of the control surface to the power of 4. (radians^4)
        /// </summary>
        private float deflectionAngle4;

        public void OnValidate()
        {
            UpdateDimensionalValues(surfaceChordRatio);
        }

        private void OnEnable()
        {
            ConnectToAeroObject();
            UpdateDimensionalValues(surfaceChordRatio);
            UpdateHinge();
        }

        private void OnDisable()
        {
            RemoveConnectionFromAeroObject();
        }

        private void OnDestroy()
        {
            RemoveConnectionFromAeroObject();
        }

        void ConnectToAeroObject()
        {
            if (TryGetComponent(out aeroObject))
            {
                // Don't want to add multiple instances of the same control surface!
                if (aeroObject.controlSurfaces.Contains(this))
                {
                    return;
                }

                aeroObject.controlSurfaces.Add(this);
            }
            else
            {
                Debug.LogWarning("No AeroObject found for control surface on " + gameObject.name);
            }
        }

        int maxIndex;
        float maxDimension;
        private void FixedUpdate()
        {
            if (controlSurfaceHinge)
            {
                controlSurfaceHinge.localRotation = Quaternion.Euler(-Mathf.Rad2Deg * deflectionAngle * rotationAxis);
            }
        }

        public void UpdateHinge()
        {
            // If we don't have a graphic for the control surface don't bother making a hinge
            // If we already have a hinge, we don't need to make a new one
            if (controlSurfaceGraphic == null || controlSurfaceHinge != null)
            {
                return;
            }

            UpdateAxes();

            Transform unscale = new GameObject("Control surface").transform;
            unscale.parent = transform;
            unscale.localPosition = Vector3.zero;
            unscale.localScale = new Vector3(1 / transform.lossyScale.x, 1 / transform.lossyScale.y, 1 / transform.lossyScale.z); // invert the scale of the parent aero object
            unscale.localRotation = Quaternion.identity;

            controlSurfaceHinge = new GameObject("Control surface hinge").transform;
            controlSurfaceHinge.parent = unscale;
            controlSurfaceHinge.localScale = new Vector3(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z); // revert to the scale of the parent aero 
            controlSurfaceHinge.localPosition = Vector3.Scale(forwardAxis, transform.lossyScale).magnitude * (surfaceChordRatio - 0.5f) * forwardAxis;
            controlSurfaceHinge.localRotation = Quaternion.Euler(-Mathf.Rad2Deg * deflectionAngle * rotationAxis);

            controlSurfaceGraphic.parent = controlSurfaceHinge;
        }

        void RemoveConnectionFromAeroObject()
        {
            if (TryGetComponent(out AeroObject aeroObject))
            {
                aeroObject.controlSurfaces.Remove(this);
            }
        }

        public void UpdateAxes()
        {
            if (aeroObject == null)
            {
                aeroObject = GetComponent<AeroObject>();
            }

            aeroObject.UpdateDimensions();

            switch (axis)
            {
                case ControlSurfaceAxis.Forward:
                    forwardAxis = Vector3.forward;
                    if (aeroObject.groupDimensions.x > aeroObject.groupDimensions.y)
                    {
                        rotationAxis = Vector3.right;
                    }
                    else
                    {
                        rotationAxis = Vector3.up;
                    }

                    break;
                case ControlSurfaceAxis.Back:
                    forwardAxis = Vector3.back;
                    if (aeroObject.groupDimensions.x > aeroObject.groupDimensions.y)
                    {
                        rotationAxis = Vector3.left;
                    }
                    else
                    {
                        rotationAxis = Vector3.down;
                    }

                    break;
                case ControlSurfaceAxis.Right:
                    forwardAxis = Vector3.right;
                    if (aeroObject.groupDimensions.z > aeroObject.groupDimensions.y)
                    {
                        rotationAxis = Vector3.back;
                    }
                    else
                    {
                        rotationAxis = Vector3.up;
                    }

                    break;
                case ControlSurfaceAxis.Left:
                    forwardAxis = Vector3.left;
                    if (aeroObject.groupDimensions.z > aeroObject.groupDimensions.y)
                    {
                        rotationAxis = Vector3.forward;
                    }
                    else
                    {
                        rotationAxis = Vector3.down;
                    }

                    break;
                case ControlSurfaceAxis.Up:
                    forwardAxis = Vector3.up;
                    if (aeroObject.groupDimensions.z > aeroObject.groupDimensions.x)
                    {
                        rotationAxis = Vector3.forward;
                    }
                    else
                    {
                        rotationAxis = Vector3.left;
                    }

                    break;
                case ControlSurfaceAxis.Down:
                    forwardAxis = Vector3.down;
                    if (aeroObject.groupDimensions.z > aeroObject.groupDimensions.x)
                    {
                        rotationAxis = Vector3.back;
                    }
                    else
                    {
                        rotationAxis = Vector3.right;
                    }

                    break;
                default:
                    break;
            }
        }

        void UpdateForwardAxis()
        {
            switch (axis)
            {
                case ControlSurfaceAxis.Forward:
                    forwardAxis = Vector3.forward;
                    break;
                case ControlSurfaceAxis.Back:
                    forwardAxis = Vector3.back;
                    break;
                case ControlSurfaceAxis.Right:
                    forwardAxis = Vector3.right;
                    break;
                case ControlSurfaceAxis.Left:
                    forwardAxis = Vector3.left;
                    break;
                case ControlSurfaceAxis.Up:
                    forwardAxis = Vector3.up;
                    break;
                case ControlSurfaceAxis.Down:
                    forwardAxis = Vector3.down;
                    break;
                default:
                    break;
            }
        }

        void UpdateRotationAxis()
        {
            if (aeroObject == null)
            {
                aeroObject = GetComponent<AeroObject>();
            }

            rotationAxis.x = 0;
            rotationAxis.y = 0;
            rotationAxis.z = 0;

            maxIndex = 0;
            maxDimension = float.MinValue;
            for (int i = 0; i < 3; i++)
            {
                if (forwardAxis[i] != 0)
                {
                    continue;
                }

                if (aeroObject.groupDimensions[i] > maxDimension)
                {
                    maxIndex = i;
                    maxDimension = aeroObject.groupDimensions[i];
                }
            }

            rotationAxis[maxIndex] = 1;
        }

        void UpdateDimensionalValues(float surfaceToChordRatio)
        {
            theta_f = Mathf.Acos((2f * surfaceToChordRatio) - 1f);
            delta_alpha = 1 - ((theta_f - Mathf.Sin(theta_f)) / Mathf.PI);
            surfaceChordRatioScaling = Mathf.Pow(surfaceToChordRatio, 0.12f);
        }

        /// <summary>
        /// Empirical function which determines how much the control surface changes the lift coefficient of the object. (dimensionless)
        /// </summary>
        /// <returns>Empirical scaling to be applied to the change in lift of a lifting body.</returns>
        public float LiftEffectiveness()
        {
            deflectionAngle4 = deflectionAngle * deflectionAngle * deflectionAngle * deflectionAngle;
            return (0.7f + (0.3f * (liftEffectivenessScale - deflectionAngle4) / (liftEffectivenessScale + deflectionAngle4))) * surfaceChordRatioScaling;
        }

        public float GetDeltaAlpha()
        {
            deflectionAngle4 = deflectionAngle * deflectionAngle * deflectionAngle * deflectionAngle;
            // Delta alpha * deflection of control surface * effectiveness of control surface as a function of chord ratio and deflection angle
            // Using an empirical function for effectiveness based on experimental data from literature:
            // https://ntrs.nasa.gov/api/citations/20150006019/downloads/20150006019.pdf 
            return delta_alpha * Mathf.Sin(deflectionAngle) * (0.7f + (0.3f * (liftEffectivenessScale - deflectionAngle4) / (liftEffectivenessScale + deflectionAngle4))) * surfaceChordRatioScaling;
        }

        private void OnDrawGizmosSelected()
        {
            //Gizmos.matrix = Matrix4x4.TRS(transform.position + (transform.rotation * (-0.5f * (1f - (1.5f * surfaceChordRatio)) * forwardAxis)), transform.rotation, transform.lossyScale); // transform.localToWorldMatrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.red;

            Gizmos.DrawWireCube(-0.5f * (1f - surfaceChordRatio) * forwardAxis,
                Vector3.Scale(Vector3.one - new Vector3(Mathf.Abs((1f - surfaceChordRatio) * forwardAxis.x), Mathf.Abs((1f - surfaceChordRatio) * forwardAxis.y), Mathf.Abs((1f - surfaceChordRatio) * forwardAxis.z)), GetComponent<AeroObject>().relativeDimensions));
        }

        ///// <summary>
        ///// Elliptical transformation used to determine change in effective alpha.
        ///// </summary>
        //public float theta_f { get { return Mathf.Acos(2.0 * surfaceChordRatio - 1.0); } }
    }
}
