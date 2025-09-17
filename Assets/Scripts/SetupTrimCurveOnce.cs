using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class SetupTrimCurveOnce : MonoBehaviour
{
    public AutoTrimAndSpeedLimiter autoTrim;
    public bool destroyAfterApply = true;

    void Start()
    {
        if (!autoTrim) autoTrim = GetComponent<AutoTrimAndSpeedLimiter>();
        if (!autoTrim) { if (destroyAfterApply) Destroy(this); return; }

        var keys = new[]
        {
            new Keyframe( 46f,  0.24f),
            new Keyframe(150f,  0.30f),
            new Keyframe(250f,  0.31f),
            new Keyframe(349f, -0.03f),
            new Keyframe(444f,  0.00f),
            new Keyframe(551f, -0.05f),
            new Keyframe(655f, -0.19f),
            new Keyframe(755f, -0.13f),
            new Keyframe(820f, -0.04f),
        };

        var curve = new AnimationCurve(keys);

#if UNITY_EDITOR
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve,  i, AnimationUtility.TangentMode.Linear);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
        }
#else
        SetLinearTangents(curve);
#endif

        autoTrim.useFeedForward = true;
        autoTrim.trimVsKnots = curve;
        autoTrim.ffWeight = 1f;

        if (destroyAfterApply) Destroy(this);
    }

    static void SetLinearTangents(AnimationCurve c)
    {
        if (c == null || c.length < 2) return;

        for (int i = 0; i < c.length; i++)
        {
            float inTan, outTan;

            if (i == 0)
            {
                var k0 = c.keys[0];
                var k1 = c.keys[1];
                float dx = Mathf.Max(1e-6f, k1.time - k0.time);
                float slope = (k1.value - k0.value) / dx;
                inTan = slope;
                outTan = slope;
            }
            else if (i == c.length - 1)
            {
                var kN_1 = c.keys[i - 1];
                var kN = c.keys[i];
                float dx = Mathf.Max(1e-6f, kN.time - kN_1.time);
                float slope = (kN.value - kN_1.value) / dx;
                inTan = slope;
                outTan = slope;
            }
            else
            {
                var kL = c.keys[i - 1];
                var kC = c.keys[i];
                var kR = c.keys[i + 1];

                float slopeL = (kC.value - kL.value) / Mathf.Max(1e-6f, kC.time - kL.time);
                float slopeR = (kR.value - kC.value) / Mathf.Max(1e-6f, kR.time - kC.time);

                inTan = slopeL;
                outTan = slopeR;
            }

            var k = c.keys[i];
            k.inTangent = inTan;
            k.outTangent = outTan;
            c.MoveKey(i, k);
        }
    }
}
