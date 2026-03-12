using UnityEngine;
using UnityEngine.UI;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Script used to update UI elements for any aircraft class which derives from the base aircraft class.
    /// </summary>
    public class FlightInfoDisplay : MonoBehaviour
    {
        public Text airspeed, altitude, throttle;
        public BaseAircraftController aircraft;

        private void Update()
        {
            if (airspeed)
            {
                airspeed.text = Mathf.Round(aircraft.AirSpeed).ToString() + " m/s";
            }

            if (altitude)
            {
                altitude.text = Mathf.Round(aircraft.Altitude).ToString() + " m";
            }

            if (throttle)
            {
                throttle.text = Mathf.Round(aircraft.Throttle).ToString() + " %";
            }
        }
    }
}
