using UnityEditor;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Provides flow velocity from either a circular or rectangular area.
    /// </summary>
    [AddComponentMenu("Aerodynamic Objects/Flow/Area Source")]
    public class AreaSource : FlowPrimitive
    {
        /// <summary>
        /// The flow speed at the source plane
        /// </summary>
        [Tooltip("The flow speed at the source plane")]
        public float speed = 1;

        public enum AreaShape
        {
            Circle,
            Rectangle
        }

        public AreaShape shape = AreaShape.Circle;
        /// <summary>
        /// Used for circle only
        /// </summary>
        [Tooltip("Used for circle only")]
        public float radius = 0.5f;
        /// <summary>
        /// Used for rectangle only
        /// </summary>
        [Tooltip("Used for rectangle only")]
        public float width = 0.5f;
        /// <summary>
        /// Used for rectangle only
        /// </summary>
        [Tooltip("Used for rectangle only")]
        public float height = 0.5f;

        public Fade fade = Fade.DistanceLinear;
        float magnitude, axialDistance;
        Vector3 localPosition;

        public override Vector3 VelocityFunction(Vector3 position)
        {
            localPosition = transform.InverseTransformPoint(position);

            // Check the shape for the area source
            switch (shape)
            {
                case AreaShape.Circle:
                    // If the position is outside the cylinder of the source return zero
                    if ((localPosition.x * localPosition.x) + (localPosition.y * localPosition.y) > radius * radius || localPosition.z * speed < 0)
                    {
                        return Vector3.zero;
                    }

                    break;

                case AreaShape.Rectangle:
                    // If the position is outside the cylinder of the source return zero
                    if (Mathf.Abs(localPosition.x) > 0.5f * width || Mathf.Abs(localPosition.y) > 0.5f * height || localPosition.z * speed < 0)
                    {
                        return Vector3.zero;
                    }

                    break;

                default:
                    break;
            }

            axialDistance = Mathf.Abs(localPosition.z);

            switch (fade)
            {
                case Fade.Constant:
                    magnitude = speed;
                    break;
                case Fade.DistanceLinear:
                    if (axialDistance == 0)
                    {
                        axialDistance = 0.01f;
                    }

                    magnitude = speed / axialDistance;
                    break;
                case Fade.DistanceSquared:
                    if (axialDistance == 0)
                    {
                        axialDistance = 0.01f;
                    }

                    magnitude = speed / (axialDistance * axialDistance);
                    break;

            }

            // Always using the local z axis for the area
            return transform.TransformDirection(new Vector3(0, 0, strengthScale * magnitude));
        }

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Color transWhite = Color.white;
            transWhite.a = 0.2f;
            Handles.color = transWhite;
            switch (shape)
            {
                case AreaShape.Circle:

                    Handles.DrawSolidDisc(transform.position, transform.forward, radius);
                    break;

                case AreaShape.Rectangle:
                    Handles.DrawAAConvexPolygon(new Vector3[]
                    {
                        transform.position + (0.5f * width * transform.right) + (0.5f * height * transform.up),
                        transform.position - (0.5f * width * transform.right) + (0.5f * height * transform.up),
                        transform.position - (0.5f * width * transform.right) - (0.5f * height * transform.up),
                        transform.position + (0.5f * width * transform.right) - (0.5f * height * transform.up),
                    });
                    break;

                default:
                    break;
            }
        }
    }
#endif
}
