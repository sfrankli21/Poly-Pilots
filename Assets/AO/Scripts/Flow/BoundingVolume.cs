using UnityEngine;

namespace AerodynamicObjects
{
    /// <summary>
    /// Used to define the shape and size of the volume for a Fluid Volume.
    /// </summary>
    [System.Serializable]
    public class BoundingVolume
    {
        public enum Shape
        {
            Global,
            Box,
            Sphere,
            Capsule,
            Mesh
        }

        public Vector3 boxSize = new Vector3(1, 1, 1);
        public float sphereRadius = 0.5f;
        public float capsuleRadius = 0.5f;
        public float capsuleHeight = 1f;

        public Shape shape = Shape.Box;
    }
}
