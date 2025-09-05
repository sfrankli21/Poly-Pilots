using AerodynamicObjects.Utility;
using Unity.VisualScripting;
using UnityEngine;

namespace AerodynamicObjects.Flow
{
    /// <summary>
    /// Implements the Biot-Savart law to model a vortex filament of finite length. Requires two vortex nodes to define the end points of the filament.
    /// </summary>
    [DefaultExecutionOrder(-5)]
    [AddComponentMenu("Aerodynamic Objects/Flow/Vortex Filament")]
    public class VortexFilament : FlowPrimitive
    {
        public VortexNode startNode, endNode;

        public bool isDynamic;
        public bool isTemporal;
        public float initialStrength = 1;
        public float coreRadius = 0.01f;
        public AnimationCurve strengthWithTime;
        public float life;

        Vector3 axis, localStartPosition, localEndPosition;
        Vector3 relativePositionA, relativePositionB, direction;
        float r, length, minLength = 0.05f;
        float cosA, cosB, speed;

        public override void OnEnable()
        {
            base.OnEnable();
            Initialise();
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            RemoveConnections();
        }

        public void RemoveConnections()
        {
            if (!startNode.IsDestroyed())
            {
                startNode.RemoveConnection();
            }

            if (!endNode.IsDestroyed())
            {
                endNode.RemoveConnection();
            }
        }

        void SetNodeDynamic(GameObject node)
        {
            if (!node.TryGetComponent(out MoveWithFlow moveWithFlow))
            {
                moveWithFlow = node.AddComponent<MoveWithFlow>();
                moveWithFlow.IgnoreInteraction(this);
            }
        }

        public void Initialise()
        {
            AddSelfToScene();

            if (startNode == null || endNode == null)
            {
                return;
            }

            if (isDynamic)
            {
                SetNodeDynamic(startNode.gameObject);
                SetNodeDynamic(endNode.gameObject);
            }

            if (isTemporal)
            {
                FlowPrimitiveLifeTimer lifeSpan = gameObject.AddComponent<FlowPrimitiveLifeTimer>();
                lifeSpan.lifeSpanDuration = life;
                lifeSpan.strengthScaleOverTimeCurve = strengthWithTime;
            }

            transform.position = 0.5f * (startNode.transform.position + endNode.transform.position);
        }

        private void FixedUpdate()
        {
            localStartPosition = startNode.transform.position;
            localEndPosition = endNode.transform.position;

            axis = localEndPosition - localStartPosition;
            length = axis.magnitude;

            // I was tired writing this, there's probably a better way...
            if (length < 1e-5f)
            {
                axis = new Vector3(0, 0, 0);
                length = minLength;
            }
            else if (length < minLength)
            {
                length = minLength;
                axis = 1f / length * axis;
            }
            else
            {
                axis = 1f / length * axis;
            }
        }

        float aLength, bLength;
        public override Vector3 VelocityFunction(Vector3 position)
        {

            relativePositionA = position - localStartPosition;
            relativePositionB = position - localEndPosition;

            r = (relativePositionA - (axis * Vector3.Dot(axis, relativePositionA))).magnitude;

            // I'm doing this because we can do a single division after the dot product rather than
            // 3 divisions to normalise the relative position vector before the dot product
            aLength = Vector3.Magnitude(relativePositionA);
            cosA = aLength > 1e-5f ? Vector3.Dot(relativePositionA, axis) / aLength : 0f;
            bLength = Vector3.Magnitude(relativePositionB);
            cosB = bLength > 1e-5f ? Vector3.Dot(relativePositionB, axis) / bLength : 0f;

            if (r < coreRadius)
            {
                speed = r / coreRadius * (initialStrength / (4 * Mathf.PI * coreRadius)) * (cosA - cosB);
            }
            else
            {
                speed = initialStrength / (4 * Mathf.PI * r) * (cosA - cosB);
            }

            // .normalize just calls Vector3.Normalize so when we're in the editor it's faster to skip that extra call
            // The compiler should get rid of this when the app is built so it doesn't matter then
            direction = Vector3.Normalize(Vector3.Cross(axis, relativePositionA));

            return strengthScale * speed * direction;
        }

        private void OnDrawGizmos()
        {
            if (startNode != null && endNode != null)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(startNode.transform.position, endNode.transform.position);
            }
        }
    }
}
