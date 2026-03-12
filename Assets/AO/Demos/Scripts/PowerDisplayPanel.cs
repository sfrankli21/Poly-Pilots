using UnityEngine;
using UnityEngine.UI;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Manages an info panel in the scene to display power from a generator.
    /// </summary>
    public class PowerDisplayPanel : MonoBehaviour
    {

        public float measuredPower;
        public Text sensortext;
        public Canvas canvas;
        public Transform panelAnchor, connectedObjectAnchor;
        public LineRenderer lr;
        Camera mainCamera;
        public Generator generator;
        // Start is called before the first frame update
        void Start()
        {
            mainCamera = Camera.main;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            measuredPower = generator.power;
            sensortext.text = Mathf.Round(measuredPower).ToString() + " W";

            canvas.transform.LookAt(mainCamera.transform);
            canvas.transform.Rotate(0, 180, 0);
            canvas.transform.rotation = Quaternion.Euler(Vector3.Scale(new Vector3(0, 1, 1), canvas.transform.rotation.eulerAngles));

            lr.SetPosition(0, connectedObjectAnchor.position);
            lr.SetPosition(1, panelAnchor.position);
            transform.position = connectedObjectAnchor.position;
        }
    }
}
