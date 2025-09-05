using UnityEngine;

namespace AerodynamicObjects.Aerodynamics
{
    /// <summary>
    /// Calculates the damping torque due to the rotational velocity of the object.
    /// </summary>
    [System.Serializable]
    public class RotationalDampingModel : IAerodynamicModel
    {
        public static float CD_normalFlatPlate = 1.2f;
        public DampingAxisModel xModel = new DampingAxisModel();
        public DampingAxisModel yModel = new DampingAxisModel();
        public DampingAxisModel zModel = new DampingAxisModel();
        public Vector3 dampingTorque;
        public AerodynamicLoad aerodynamicLoad = new AerodynamicLoad();

        /// <summary>
        /// Calculate the moment due to rotational damping on the aerodynamic object.
        /// </summary>
        public AerodynamicLoad GetAerodynamicLoad(AeroObject ao)
        {
            //dampingTorque = new Vector3(xModel.GetDampingTorque(ao.angularVelocity.x, ao), yModel.GetDampingTorque(ao.angularVelocity.y, ao), zModel.GetDampingTorque(ao.angularVelocity.z, ao));
            dampingTorque.x = xModel.GetDampingTorque(ao.localAngularVelocity.x, ao);
            dampingTorque.y = yModel.GetDampingTorque(ao.localAngularVelocity.y, ao);
            dampingTorque.z = zModel.GetDampingTorque(ao.localAngularVelocity.z, ao);

            // Compute the resulting force and moment
            return new AerodynamicLoad
            {
                force = Vector3.zero,
                moment = dampingTorque
            };
        }

        public void UpdateDimensionValues(AeroObject ao)
        {
            xModel.UpdateDimensionValues(ao.dimensions.x, ao.dimensions.y, ao.dimensions.z, ao);
            yModel.UpdateDimensionValues(ao.dimensions.y, ao.dimensions.x, ao.dimensions.z, ao);
            zModel.UpdateDimensionValues(ao.dimensions.z, ao.dimensions.y, ao.dimensions.x, ao);

            xModel.surfaceArea = ao.GetEllipsoidSurfaceArea();
            yModel.surfaceArea = xModel.surfaceArea;
            zModel.surfaceArea = yModel.surfaceArea;
        }

        public class DampingAxisModel
        {
            public float lamda;
            public float majorDiameter, minorDiameter;
            public float majorDiameter3;
            public float radiusOfAction;
            public float circumference;
            public float pressureArea;
            public float surfaceArea;
            public float pressureTorque;
            public float skinFrictionTorque;
            public float Re, Cf;
            const float oneOver128 = 1f / 128f;
            private float angularVel2;

            public void UpdateDimensionValues(float rotationAxisDimension, float x, float y, AeroObject ao)
            {
                if (x > y)
                {
                    majorDiameter = x;
                    minorDiameter = y;
                }
                else
                {
                    majorDiameter = y;
                    minorDiameter = x;
                }

                majorDiameter3 = majorDiameter * majorDiameter * majorDiameter;
                radiusOfAction = majorDiameter / 4f;

                switch (ao.referenceAreaShape)
                {
                    case AeroObject.ReferenceAreaShape.Ellipse:
                        pressureArea = 0.25f * Mathf.PI * rotationAxisDimension * majorDiameter;

                        break;
                    case AeroObject.ReferenceAreaShape.Rectangle:
                        pressureArea = majorDiameter * rotationAxisDimension;

                        break;
                    default:
                        break;
                }

                // This is handled by the overall model so we don't waste 2 calculations of this
                //surfaceArea = ao.GetEllipsoidSurfaceArea();
                circumference = 0.25f * Mathf.PI * majorDiameter * minorDiameter;
                lamda = minorDiameter / majorDiameter;
            }

            public float GetDampingTorque(float angularVelocity, AeroObject ao)
            {
                // This is the readable version of the calculations
                //angularVel2 = angularVelocity * angularVelocity;
                //pressureTorque = oneOver128 * CD_normalFlatPlate * ao.fluid.density * pressureArea * majorDiameter3 * angularVel2;
                //Re = ao.fluid.density * angularVelocity * radiusOfAction * majorDiameter / ao.fluid.dynamicViscosity;
                //Cf = Re == 0 ? 0 : 0.027f / Mathf.Pow(Re, 1f / 7f);
                //skinFrictionTorque = oneOver128 * Cf * ao.fluid.density * surfaceArea * majorDiameter3 * angularVel2;

                //return ((1f - lamda) * pressureTorque) + (lamda * skinFrictionTorque);

                // This is a slightly more optimal version
                angularVel2 = angularVelocity * angularVelocity;
                float rotationalPressure = 0.5f * 0.125f * majorDiameter3 * ao.fluid.density * angularVel2;

                // Reynolds number for rotation is using the circumference of the rotating ellipse as reference length
                Re = ao.fluid.density * Mathf.Abs(angularVelocity) * circumference / ao.fluid.dynamicViscosity;
                Cf = Re == 0 ? 0 : 0.027f / Mathf.Pow(Re, 1f / 7f);

                return -Mathf.Sign(angularVelocity) * rotationalPressure * (((1f - lamda) * CD_normalFlatPlate * pressureArea) + (lamda * Cf * surfaceArea));
            }
        }
    }
}
