using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// Calculates a translational drag force based on the square of the object's velocity and the planform area.
    /// The drag coefficient for the object is determined according to the ratios of the object's axes, i.e. an object shaped like a flat plate will have a much higher drag coefficient than a spherical object.
    /// </summary>
    [System.Serializable]
    public class DragModel : IAerodynamicModel
    {
        /// <summary>
        /// The vector of drag area coefficients for the object. (dimensionless)
        /// </summary>
        public Vector3 CDS;

        public float Cf;
        public float reynoldsNumber;
        public float resolvedDragArea;

        /// <summary>
        /// The drag coefficient of a flat plate when aligned normal to the fluid flow.
        /// Usually set as 1.2 in the literature. (dimensionless)
        /// </summary>
        public const float CD_normalFlatPlate = 1.2f;

        /// <summary>
        /// The drag coefficient of a rough sphere. Usually set as 0.5 in the literature. (dimensionless)
        /// </summary>
        public const float CD_roughSphere = 0.5f;

        public DragModel2D xAxisModel = new DragModel2D();
        public DragModel2D yAxisModel = new DragModel2D();
        public DragModel2D zAxisModel = new DragModel2D();

        public AerodynamicLoad myAerodynamicLoad = new AerodynamicLoad()
        {
            moment = Vector3.zero,
            force = Vector3.zero
        };

        Vector3 velocityDirection;
        float speed;
        float refLength;
        /// <summary>
        /// Calculate the aerodynamic force of drag acting on the object. This includes pressure and shear drag.
        /// </summary>
        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            speed = ao.localRelativeVelocity.magnitude;

            velocityDirection = speed == 0 ? new Vector3(0, 0, 0) : 1f / speed * ao.localRelativeVelocity;

            // There's an issue here where reynolds number becomes very small but not zero when the flow is aligned with a direction
            // of zero length - we could cut off any reynolds numbers below 1 as a hacky fix
            // I think shear coefficient actually needs to use surface area so that might solve this issue?
            refLength = Vector3.Scale(ao.dimensions, velocityDirection).magnitude;
            // Shear drag depends on reynolds number
            reynoldsNumber = ao.fluid.density * speed * refLength / ao.fluid.dynamicViscosity;

            // Shear coefficient
            Cf = reynoldsNumber == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNumber, 1f / 7f);

            xAxisModel.CalculateCoefficients(ao, Cf);
            yAxisModel.CalculateCoefficients(ao, Cf);
            zAxisModel.CalculateCoefficients(ao, Cf);

            CDS.x = xAxisModel.CDS;
            CDS.y = yAxisModel.CDS;
            CDS.z = zAxisModel.CDS;

            // Resolve the drag area components and compute the net force in the local frame
            resolvedDragArea = CDS.x + ((CDS.y - CDS.x) * velocityDirection.y * velocityDirection.y) + ((CDS.z - CDS.x) * velocityDirection.z * velocityDirection.z);

            // The convention here is to compute the forces in coordinates local to the object as that is
            // generally how they will come - they can also then be used directly in most cases
            myAerodynamicLoad.force = -ao.dynamicPressure * resolvedDragArea * velocityDirection;
            return myAerodynamicLoad;
        }

        /// <summary>
        /// Calculates and stores the object's dimensions in the appropriate nomenclature, i.e. span thickness chord.
        /// </summary>
        public void UpdateDimensionValues(AeroObject ao)
        {
            xAxisModel.GetConstants(ao, this, ao.dimensions.x, ao.dimensions.y, ao.dimensions.z, Vector3.right);
            yAxisModel.GetConstants(ao, this, ao.dimensions.y, ao.dimensions.x, ao.dimensions.z, Vector3.up);
            zAxisModel.GetConstants(ao, this, ao.dimensions.z, ao.dimensions.x, ao.dimensions.y, Vector3.forward);
        }

        /// <summary>
        /// Calculate an approximate drag coefficient based on the pressure drag coefficients in each axis. Resolved using the velocity direction.
        /// </summary>
        /// <returns>Pressure Drag Coefficient</returns>
        public float GetDragCoefficient()
        {
            return xAxisModel.CD_pressure + ((yAxisModel.CD_pressure - xAxisModel.CD_pressure) * velocityDirection.y * velocityDirection.y) + ((zAxisModel.CD_pressure - xAxisModel.CD_pressure) * velocityDirection.z * velocityDirection.z);
        }

        public class DragModel2D
        {
            public float reynoldsNumber;
            public float Cf_linear;
            public float CD_shear, CD_pressure;
            public float CDS;
            public float chord, thickness, span;
            public float adjustedThickness;
            public float frontalArea;
            public float planformArea;
            float thicknessToChord;
            Vector3 axis;
            //public ReferencePlane referencePlane = new ReferencePlane();

            public void GetConstants(AeroObject ao, DragModel dragModel, float chordDimension, float thicknessDimension, float spanDimension, Vector3 axis)
            {
                chord = chordDimension;
                this.axis = axis;

                if (spanDimension > thicknessDimension)
                {
                    thickness = thicknessDimension;
                    span = spanDimension;
                }
                else
                {
                    thickness = spanDimension;
                    span = thicknessDimension;
                }

                switch (ao.referenceAreaShape)
                {
                    case AeroObject.ReferenceAreaShape.Ellipse:
                        planformArea = 0.25f * Mathf.PI * span * chord;

                        break;
                    case AeroObject.ReferenceAreaShape.Rectangle:
                        planformArea = span * chord;

                        break;
                    default:
                        break;
                }
            }
            float controlSurfaceThicknessDelta = 0;
            public void CalculateCoefficients(AeroObject ao, float Cf)
            {
                // Shear drag depends on reynolds number
                //reynoldsNumber = ao.fluid.density * Mathf.Abs(velocity) * chord / ao.fluid.dynamicViscosity;

                // Shear coefficient
                Cf_linear = Cf; // reynoldsNumber == 0 ? 0 : 0.027f / Mathf.Pow(reynoldsNumber, 1f / 7f);
                CD_shear = 2 * Cf_linear;
                //CD_shear = 2 * Mathf.Abs(Cf);

                controlSurfaceThicknessDelta = 0;
                for (int i = 0; i < ao.controlSurfaces.Count; i++)
                {
                    // Control surfaces don't stack on each other, so just use the one which has the biggest effect on the thickness
                    controlSurfaceThicknessDelta = Mathf.Max(controlSurfaceThicknessDelta, Mathf.Abs(chord * ao.controlSurfaces[i].surfaceChordRatio * Mathf.Sin(ao.controlSurfaces[i].deflectionAngle) * Vector3.Dot(axis, ao.controlSurfaces[i].forwardAxis)));
                }

                adjustedThickness = thickness + controlSurfaceThicknessDelta;
                thicknessToChord = adjustedThickness / chord;
                // Frontal area, somewhat..

                switch (ao.referenceAreaShape)
                {
                    case AeroObject.ReferenceAreaShape.Ellipse:
                        frontalArea = 0.25f * Mathf.PI * span * adjustedThickness;

                        break;
                    case AeroObject.ReferenceAreaShape.Rectangle:
                        frontalArea = span * adjustedThickness;

                        break;
                    default:
                        break;
                }

                // Chord is smaller than thickness which means t/c > 1 - or c/t < 1 so the object is tending towards behaving as
                // a flat plate normal to the flow in this dimension/axis
                if (chord < adjustedThickness)
                {
                    // If chord is smaller than thickness, then a reducing value of c/t means we're on the way
                    // to becoming a normal flat plate
                    CD_pressure = CD_normalFlatPlate - ((CD_normalFlatPlate - CD_roughSphere) * (chord / adjustedThickness));
                }
                else
                {
                    // Otherwise, t/c <= 1 and the object in this dimension is some kind of sphere that is squashed
                    // As t/c becomes smaller, we are on the way to becoming infinitely thin and producing no pressure drag

                    // Hoerner gives a relationship between pressure CD and c/t which flattens out at around c/t = 4
                    // Or t/c = 0.25
                    // We can rely on frontal area to reduce the drag to zero
                    //CD_pressure = CD_roughSphere * Mathf.Max(adjustedThickness / chord, 0.25f);

                    // Based on the model used in AO 1, we have this relation for t/c <= 1
                    // Notice the (t/c)^2, which gives a slow increase to pressure drag for low t/c compared to t/c close to 1
                    // This doesn't change the two end points, t/c = 0 gives CD = 0 and t/c = 1 gives CD = rough sphere
                    CD_pressure = CD_roughSphere * thicknessToChord;
                }

                // Pressure coefficients are independent of reynolds number (we assume)
                // When t/c = 1, we have a perfect sphere (or a cube)
                // When t/c = 0, we have a flat plate, but it's not normal to the flow => so pressure drag should be zero
                // When c/t < 1, we are becoming a normal flat plate

                // Using frontal area as a reference for pressure drag - maybe worth double checking this one
                CDS = (CD_shear * planformArea) + (CD_pressure * frontalArea);
            }
        }
    }
}
