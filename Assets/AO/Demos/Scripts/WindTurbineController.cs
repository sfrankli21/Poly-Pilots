using AerodynamicObjects.Flow;
using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manages size and orientation of wind turbine blades.
    /// </summary>
    public class WindTurbineController : MonoBehaviour
    {
        [Range(-90, 90)]
        public float bladePitchAngle;
        [Range(0, 6)]
        public float windSpeed;
        [Range(-180, 180)]
        public float windDirection;
        [Range(2, 10)]
        public float bladeAspectRatio;
        public Transform bladeRoot1, bladeRoot2, bladeRoot3;
        Transform bladeGeometry1, bladeGeometry2, bladeGeometry3;
        public UniformFlow fluidZone;

        float bladeArea = 3; // area in m^2
        float bladeSpan, bladeChord; //blade dimensions

        void Start()
        {
            bladeGeometry1 = bladeRoot1.Find("Blade Geometry");
            bladeGeometry2 = bladeRoot2.Find("Blade Geometry");
            bladeGeometry3 = bladeRoot3.Find("Blade Geometry");
        }

        void FixedUpdate()
        {
            fluidZone.windVelocity.speed = windSpeed;
            fluidZone.windVelocity.azimuth = windDirection;

            bladeRoot1.transform.localRotation = Quaternion.Euler(bladePitchAngle + 90, 0, 0);
            bladeRoot2.transform.localRotation = Quaternion.Euler(bladePitchAngle + 90, 0, 0);
            bladeRoot3.transform.localRotation = Quaternion.Euler(bladePitchAngle + 90, 0, 0);

            bladeSpan = Mathf.Sqrt(bladeAspectRatio * bladeArea);
            bladeChord = bladeSpan / bladeAspectRatio;
            bladeGeometry1.localScale = new Vector3(bladeSpan, bladeChord / 20, bladeChord);
            bladeGeometry1.localPosition = new Vector3(bladeSpan / 2, 0, 0);
            bladeGeometry2.localScale = new Vector3(bladeSpan, bladeChord / 20, bladeChord);
            bladeGeometry2.localPosition = new Vector3(bladeSpan / 2, 0, 0);
            bladeGeometry3.localScale = new Vector3(bladeSpan, bladeChord / 20, bladeChord);
            bladeGeometry3.localPosition = new Vector3(bladeSpan / 2, 0, 0);

        }
    }
}
