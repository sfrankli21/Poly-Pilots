using UnityEngine;
using UnityEngine.UI;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Displays text for the force on a configurable joint using a canvas in the scene.
    /// </summary>
    public class StringForceSensor : MonoBehaviour
    {
        public ConfigurableJoint cj;
        public Vector3 smoothedForce;
        Vector3 currentForce, oldForce;
        public float smoothingConstant; // increase this value to smooth out the force measurement time history. Default 1000
        public Text sensortext;
        public Canvas canvas;
        public Transform panelAnchor, connectedObjectAnchor;
        public LineRenderer lr;
        Camera mainCamera;
        // Start is called before the first frame update
        void Start()
        {
            mainCamera = Camera.main;
        }

        void FixedUpdate()
        {
            if (cj != null)
            {
                currentForce = cj.currentForce;
                smoothedForce = oldForce + (1 / smoothingConstant * (currentForce - oldForce));
                oldForce = smoothedForce;
                sensortext.text = (Mathf.Round(10f * smoothedForce.magnitude) / 10).ToString() + " N";
            }

            canvas.transform.LookAt(mainCamera.transform);
            canvas.transform.Rotate(0, 180, 0);
            //Zero the rotation about x so that the info panel looks towards the camera but is always upright
            canvas.transform.rotation = Quaternion.Euler(Vector3.Scale(new Vector3(0, 1, 1), canvas.transform.rotation.eulerAngles));

            lr.SetPosition(0, connectedObjectAnchor.position);
            lr.SetPosition(1, panelAnchor.position);
            transform.position = connectedObjectAnchor.position;
        }
    }
}
