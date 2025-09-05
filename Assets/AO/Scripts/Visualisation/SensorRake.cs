using UnityEngine;

namespace AerodynamicObjects
{
    [RequireComponent(typeof(LineRenderer))]
    public class SensorRake : FlowAffected
    {
        /// <summary>
        /// Number of sample stations in the rake.
        /// </summary>
        [Tooltip("Number of sample stations in the rake.")]
        public int sampleStationsCount = 10;
        /// <summary>
        /// Length of rake, m
        /// </summary>
        [Tooltip("Length of rake, m")]
        public float rakeLength = 1;

        /// <summary>
        /// Controls the length of time overwhich signals are averaged. A value of 1 gives the raw signal, smaller values progressively increase the amount of averaging. Averaging over a longer period smooths the data but makes the sensor laggy to changes in steady conditions.
        /// </summary>
        [Tooltip("Controls the length of time overwhich signals are averaged. A value of 1 gives the raw signal, smaller values progressively increase the amount of averaging. Averaging over a longer period smooths the data but makes the sensor laggy to changes in steady conditions.")]
        public float filterCoefficient = 1f;
        /// <summary>
        /// Controls the relative length of arrows in world space. Increase to make arrows longer for a given velocity measurement
        /// </summary>
        [Tooltip("Controls the relative length of arrows in world space. Increase to make arrows longer for a given velocity measurement")]
        public float sensitivity = 1;
        /// <summary>
        /// Thickness of line used to render wake graphics
        /// </summary>
        [Tooltip("Thickness of line used to render wake graphics")]
        public float lineWidth = 0.02f;
        Transform[] samplePoints;
        FlowSensor[] flowSensors;
        Vector3[] samples;
        Vector3[] oldSamples;

        LineRenderer profileLineRenderer;
        LineRenderer[] arrowLineRenderers;

        void Start()
        {
            SetUpRake();
        }

        public override void FixedUpdate()
        {
            for (int i = 0; i < samplePoints.Length; i++)
            {
                samples[i] += 0.001f * filterCoefficient * (-GetFluidVelocity(samplePoints[i].position) - oldSamples[i]);

                oldSamples[i] = samples[i];
                //Set profile line points
                profileLineRenderer.SetPosition(i, samplePoints[i].transform.position - (sensitivity * samples[i]));
                //set arrow points. 
                arrowLineRenderers[i].SetPosition(0, samplePoints[i].transform.position);
                arrowLineRenderers[i].SetPosition(1, samplePoints[i].transform.position - (sensitivity * samples[i])); //arrow shaft
            }
        }

        public void SetUpRake()
        {
            profileLineRenderer = GetComponent<LineRenderer>();
            profileLineRenderer.positionCount = sampleStationsCount;
            profileLineRenderer.widthMultiplier = lineWidth;
            samplePoints = new Transform[sampleStationsCount];
            samples = new Vector3[sampleStationsCount];
            oldSamples = new Vector3[sampleStationsCount];
            flowSensors = new FlowSensor[sampleStationsCount];
            arrowLineRenderers = new LineRenderer[sampleStationsCount];
            profileLineRenderer.material = Resources.Load("Line material") as Material;
            for (int i = 0; i < samplePoints.Length; i++)
            {
                samplePoints[i] = new GameObject("Sample point " + i.ToString()).transform;
                samplePoints[i].parent = transform;
                samplePoints[i].position = transform.TransformPoint(new Vector3(rakeLength * (-0.5f + ((float)i / (sampleStationsCount - 1))), 0, 0));
                arrowLineRenderers[i] = samplePoints[i].gameObject.AddComponent<LineRenderer>();
                arrowLineRenderers[i].widthMultiplier = lineWidth;
                arrowLineRenderers[i].positionCount = 2; //use 5 if drawing arrow head, otherwise 2
                arrowLineRenderers[i].material = Resources.Load("Line material") as Material;
            }
        }
    }
}
