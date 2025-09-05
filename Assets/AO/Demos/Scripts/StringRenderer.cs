using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Utility script to render a line between two transforms.
    /// </summary>
    public class StringRenderer : MonoBehaviour
    {
        public Transform stringStart, stringEnd;
        LineRenderer lineRenderer;
        // Start is called before the first frame update
        void Start()
        {
            lineRenderer = GetComponent<LineRenderer>();
        }

        // Update is called once per frame
        void Update()
        {
            lineRenderer.SetPosition(0, stringStart.position);
            lineRenderer.SetPosition(1, stringEnd.position);
        }
    }
}
