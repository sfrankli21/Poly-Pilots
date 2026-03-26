using AerodynamicObjects.Aerodynamics;
using TMPro;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Used to manage an anemometer, including setting arrow size and sensitivities for the aero objects on the cups and updating a text display of the measured wind speed.
    /// </summary>
    public class CupAnemometer : MonoBehaviour
    {
        /// <summary>
        /// Drag coefficient with the flat face of the cup going into wind.
        /// </summary>
        [Tooltip("Drag coefficient with the flat face of the cup going into wind.")]
        public float cupForwardDragCoefficient = 1.4f;
        /// <summary>
        /// Drag coefficient with the rounded back face of the cup going into wind.
        /// </summary>
        [Tooltip("Drag coefficient with the rounded back face of the cup going into wind.")]
        public float cupReverseDragCoefficient = 0.4f;
        /// <summary>
        /// The drag coefficient of the cup in a flow direction normal to the axis of symmetry of the cup. This makes relatively little difference to the anemometer performance. Default value of 0.5 is a sensible value.
        /// </summary>
        [Tooltip("The drag coefficient of the cup in a flow direction normal to the axis of symmetry of the cup. This makes relatively little difference to the anemometer performance. Default value of 0.5 is a sensible value.")]
        public float offAxisDragCoefficient = 0.5f;
        /// <summary>
        /// Radial distance from the centre of rotation of the anemometer to centre of the cup.
        /// </summary>
        [Tooltip("Radial distance from the centre of rotation of the anemometer to centre of the cup.")]
        public float armRadius = 0.1f;
        Rigidbody rb; // rigid body that represents the whole rotating assembly of the anemometer. The moment of inertia is set using the mass of the body and the attached colliders for the cups, so if the mass is scaled with size, the inertia will be approximately correct also.
        /// <summary>
        /// The mass of the rotating part of the anemometer in kg. Inertia is calculated from attached colliders. The inertia affects the time response of the anemometer, but does not affect the calibration between angular velocity and wind speed.
        /// </summary>
        [Tooltip("The mass of the rotating part of the anemometer in kg. Inertia is calculated from attached colliders. The inertia affects the time response of the anemometer, but does not affect the calibration between angular velocity and wind speed.")]
        public float rotatingMass = .1f;
        TMP_Text speedDisplay;
        /// <summary>
        /// Calculated wind speed in m/s based on rotation rate of anemometer.
        /// </summary>
        [Tooltip("Calculated wind speed in m/s based on rotation rate of anemometer.")]
        public float filteredWindSpeed;
        AeroObject[] aeroObjects;
        UserSuppliedDragModel userSuppliedDragModel;
        /// <summary>
        /// This is a calculated value for information. Physical constant for the anemometer based on the user supplied cup forward and reverse drag coefficients. The theoretical speed of the flow in m/s is the anemometer angular velocity in rad/s multiplied by the anemometer coefficient and the arm radius in m.
        /// </summary>
        [Tooltip("This is a calculated value for information. Physical constant for the anemometer based on the user supplied cup forward and reverse drag coefficients. The theoretical speed of the flow in m/s is the anemometer angular velocity in rad/s multiplied by the anemometer coefficient and the arm radius in m.")]
        public float anemometerCoefficient;
        float currentWindSpeed, previousWindSpeed;
        /// <summary>
        /// Controls the amount of averaging done to obtain a speed value from the anemometer. Default value is 1. A higher value makes the speed measurement more responsive to dynamic changes in wind speed.
        /// </summary>
        [Tooltip("Controls the amount of averaging done to obtain a speed value from the anemometer. Default value is 1. A higher value makes the speed measurement more responsive to dynamic changes in wind speed.")]
        public float filterCoefficient = 1;
        float windArrowSensitivity; //initial scale of wind arrow for transform scale of 1
        float userDragArrowSensitivity; //initial scale of drag arrow for transform scale of 1
        float windArrowDiameter;
        float userDragArrowDiameter;
        float scale;// the lossy scale magnitude of the transform
                    // Start is called before the first frame update
        void Start()
        {
            scale = transform.lossyScale.magnitude;
            aeroObjects = GetComponentsInChildren<AeroObject>();
            rb = GetComponentInChildren<Rigidbody>();
            rb.mass = rotatingMass * scale * scale; // scale the mass with the square of the linear scale
            windArrowSensitivity = GetComponentInChildren<WindArrow>().Sensitivity;
            windArrowDiameter = GetComponentInChildren<WindArrow>().Diameter;
            userDragArrowSensitivity = GetComponentInChildren<UserDragArrow>().Sensitivity;
            userDragArrowDiameter = GetComponentInChildren<UserDragArrow>().Diameter;

            WindArrow[] windArrows = GetComponentsInChildren<WindArrow>();
            UserDragArrow[] userDragArrows = GetComponentsInChildren<UserDragArrow>();

            //scale the arrow lengths so that are consistent with the scale of the anemometer
            foreach (WindArrow windArrow in windArrows)
            {
                windArrow.Sensitivity = windArrowSensitivity * scale;
                windArrow.Diameter = windArrowDiameter * scale;
            }

            foreach (UserDragArrow userDragArrow in userDragArrows)
            {
                userDragArrow.Sensitivity = userDragArrowSensitivity / scale;// Mathf.Pow(scale, 0.33f);
                userDragArrow.Diameter = userDragArrowDiameter * scale;
            }

            for (int i = 0; i < aeroObjects.Length; i++)
            {
                userSuppliedDragModel = aeroObjects[i].GetModel<UserSuppliedDragModel>();
                //Note we are assuming that the off axis drag coefficients CDx and CDy are equal to a nominal value of 0.5 in both directions. The actual value chosen has some impact on the anemometer calibration, but this relatively minor compared to the effect of varying CDz in each direction.
                userSuppliedDragModel.forwardDragCoefficients = new Vector3(offAxisDragCoefficient, offAxisDragCoefficient, cupForwardDragCoefficient);
                userSuppliedDragModel.reverseDragCoefficients = new Vector3(offAxisDragCoefficient, offAxisDragCoefficient, cupReverseDragCoefficient);
            }
            //See https://web1.eng.famu.fsu.edu/~shih/eml3016/lecture-notes/cup%20anemometer.PDF for a simple derivation of anemometer theory
            float alpha = Mathf.Sqrt(cupForwardDragCoefficient / cupReverseDragCoefficient);
            anemometerCoefficient = (alpha + 1) / (alpha - 1);

            speedDisplay = GetComponentInChildren<TMP_Text>();
        }

        void FixedUpdate()
        {
            currentWindSpeed = -anemometerCoefficient * armRadius * scale * rb.angularVelocity.y; //V=K*R*Omega

            filteredWindSpeed = previousWindSpeed + ((currentWindSpeed - previousWindSpeed) * filterCoefficient / 1000);
            speedDisplay.text = "Measured Wind Speed " + (Mathf.Round(10 * filteredWindSpeed) / 10) + "m/s";

            previousWindSpeed = filteredWindSpeed;
        }
    }
}
