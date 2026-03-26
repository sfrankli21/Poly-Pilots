using UnityEngine;

namespace AerodynamicObjects.Utility
{
    /// <summary>
    /// Applies a life timer to a flow primitive, destroying the entire GameObject after a set amount of time.
    /// Also applies a strength over time curve to the strength scale of the flow primitive.
    /// Will affect all flow primitives that are children of the object that this component is attached to (including the object itself).
    /// </summary>
    public class FlowPrimitiveLifeTimer : MonoBehaviour
    {
        FlowPrimitive[] flowPrimitives;
        public AnimationCurve strengthScaleOverTimeCurve = new AnimationCurve(new Keyframe[] { new(0, 0), new(.1f, 1), new(1, 0) });
        public float lifeSpanDuration = 10f;
        float age;

        private void OnEnable()
        {
            age = 0;
            flowPrimitives = GetComponentsInChildren<FlowPrimitive>();
        }

        void FixedUpdate()
        {
            age += Time.fixedDeltaTime;

            if (age >= lifeSpanDuration)
            {
                Destroy(gameObject);
                return;
            }

            for (int i = 0; i < flowPrimitives.Length; i++)
            {
                flowPrimitives[i].strengthScale = strengthScaleOverTimeCurve.Evaluate(age / lifeSpanDuration);
            }
        }
    }
}
