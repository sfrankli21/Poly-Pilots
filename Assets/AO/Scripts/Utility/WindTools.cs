using UnityEngine;

namespace AerodynamicObjects
{
    public static class WindTools
    {
        /// <summary>
        /// Calculates the vector velocity using speed, angle about the Y axis in degrees (azimuth), and elevation angle in degrees.
        /// </summary>
        /// <param name="speed">Wind speed (m/s)</param>
        /// <param name="azimuth">Angle about the Y axis (degrees)</param>
        /// <param name="elevation">Angle between the wind and the horizontal plane (degrees)</param>
        /// <returns>Vector of velocity (m/s)</returns>
        public static Vector3 GetVelocityFromSpeedAndAngles(float speed, float azimuth, float elevation)
        {
            return new Vector3(speed * Mathf.Sin(Mathf.Deg2Rad * azimuth) * Mathf.Cos(Mathf.Deg2Rad * elevation),
                                   speed * Mathf.Sin(Mathf.Deg2Rad * elevation),
                                   speed * Mathf.Cos(Mathf.Deg2Rad * azimuth) * Mathf.Cos(Mathf.Deg2Rad * elevation));
        }
    }
}
