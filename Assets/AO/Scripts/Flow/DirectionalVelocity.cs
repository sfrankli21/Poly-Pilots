using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Provides a conversion between (speed, azimuth, direction) and a velocity vector.
    /// </summary>
    [System.Serializable]
    public class DirectionalVelocity
    {
        public float speed = 1f;

        public float azimuth;

        public float elevation;

        public Vector3 GetVelocity()
        {
            return new Vector3(speed * -Mathf.Sin(Mathf.Deg2Rad * azimuth) * Mathf.Cos(Mathf.Deg2Rad * elevation),
                                   speed * Mathf.Sin(Mathf.Deg2Rad * elevation),
                                   speed * -Mathf.Cos(Mathf.Deg2Rad * azimuth) * Mathf.Cos(Mathf.Deg2Rad * elevation));
        }
    }
}
