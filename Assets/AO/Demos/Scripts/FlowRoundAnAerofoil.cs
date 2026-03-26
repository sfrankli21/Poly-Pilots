using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

namespace AerodynamicObjects.Demos
{
    public class FlowRoundAnAerofoil : FlowPrimitive
    {
        //assumes flow is in xy plane
        public float Vinf, Lambda, alpha, rotation;
        public float R;
        [Range(-1f, 1f)]
        public float Zscale;

        Complex i, Z, Z1, W, WTwiddle, Zed, Zed1, mu;
        Vector3 localPosition, circleVelocity, VinfVector;

        Vector3 ZVector;

        public Vector3 center;
        public Transform circle;
        public int pointCount;
        public LineRenderer zlineRenderer, wlineRenderer;
        float theta;
        public float coreRadius;
        public FlowSensor sensor;
        // Start is called before the first frame update
        public void Start()
        {
            zlineRenderer.positionCount = pointCount;
            wlineRenderer.positionCount = pointCount;

            i = new Complex(0, 1);
        }

        public override Vector3 VelocityFunction(Vector3 position)
        {
            localPosition = transform.InverseTransformPoint(position);

            Z1 = new Complex(localPosition.x, localPosition.y);
            //transform to Z plane
            //check which branch we should be on
            if (Z1.Real < 0)
            {
                Z = (Z1 - Complex.Sqrt((Z1 * Z1) - (4 * Zscale))) / 2;
            }
            else
            {
                Z = (Z1 + Complex.Sqrt((Z1 * Z1) - (4 * Zscale))) / 2;
            }

            //take off rotation
            Z *= Complex.Exp(-i * rotation);

            Lambda = 4 * Mathf.PI * Vinf * R * Mathf.Sin(alpha + Mathf.Asin(circle.position.y / R));
            //find velocity in Z plane
            WTwiddle = (Vinf * Complex.Exp(-i * alpha)) + (i * Lambda / (2 * Mathf.PI * (Z - mu))) - (Vinf * R * R * Complex.Exp(i * alpha) / ((Z - mu) * (Z - mu)));
            W = WTwiddle / ((1 - (Zscale / (Z * Z))) * Complex.Exp(i * rotation));

            // ============================ Changed the - VinfVector to + VinfVector ============================
            circleVelocity = new Vector3((float)W.Real, -(float)W.Imaginary, 0) + VinfVector;
            return 1 * circleVelocity;

        }
        void Update()
        {
            //alpha = -Mathf.Deg2Rad * transform.rotation.eulerAngles.z;

            VinfVector = sensor.relativeVelocity;

            Vinf = VinfVector.magnitude;
            mu = new Complex(circle.localPosition.x, circle.localPosition.y);
            R = Mathf.Sqrt(((1 - circle.position.x) * (1 - circle.position.x)) + (circle.position.y * circle.position.y));
            center = circle.position;
            //radius = circle.lossyScale.magnitude;
            for (int j = 0; j < pointCount; j++)
            {
                theta = 2 * Mathf.PI * j / (pointCount - 1);
                ZVector = (R * new Vector3(Mathf.Cos(theta), Mathf.Sin(theta), 0)) + circle.position;
                // zlineRenderer.SetPosition(i, ZVector);
                Zed = new Complex(ZVector.x, ZVector.y);
                //do transform
                Zed1 = Zed + (Zscale / Zed);
                //rotate
                Zed1 = Zed1 * Complex.Exp(i * rotation);
                //convert to vector
                ZVector = new Vector3((float)Zed1.Real, (float)Zed1.Imaginary, 0);
                wlineRenderer.SetPosition(j, ZVector);
            }
        }
    }
}
