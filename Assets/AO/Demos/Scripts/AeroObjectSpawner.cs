using UnityEngine;

namespace AerodynamicObjects.Demos
{
    /// <summary>
    /// Provides a spawn aero object function which is called on a button press in the Wind Tunnel Demo scene.
    /// </summary>
    public class AeroObjectSpawner : MonoBehaviour
    {
        public GameObject aeroObject;
        public float minDimension, maxDimension, minMass, maxMass, maxCamber, spawnRadius;
        public Transform spawnCentre;

        public void SpawnAeroObject()
        {
            GameObject go = Instantiate(aeroObject, (Random.insideUnitSphere * spawnRadius) + spawnCentre.position, Random.rotation);
            go.name = "Aero Object";

            ConfigureAeroObject(go);
        }

        public void ConfigureAeroObject(GameObject go)
        {
            AeroObject aeroObject = go.GetComponentInChildren<AeroObject>();
            Transform aeroObjectTransform = aeroObject.transform;
            Transform geometryTransform = aeroObjectTransform.parent;
            geometryTransform.localScale = new Vector3(Random.Range(minDimension, maxDimension), Random.Range(minDimension, maxDimension) / Random.Range(2, 20f), Random.Range(minDimension, maxDimension));
            go.GetComponent<Rigidbody>().centerOfMass = Vector3.Scale(Random.insideUnitCircle, geometryTransform.localScale / 2);
            go.GetComponent<Rigidbody>().mass = Random.Range(minMass, maxMass);
            aeroObject.camber = new Vector3(Random.Range(0, maxCamber), Random.Range(0, maxCamber), Random.Range(0, maxCamber));

            // We have to tell the aero object that its dimensions have been changed.
            // An alternative would be to set
            // aeroObject.updateDimensionsInRuntime = true;
            // But this would incur the cost of updating dimensions at every fixed update step, and we are only changing the dimensions once.
            aeroObject.UpdateDimensions();

            go.transform.Find("CentreOfGravity").localPosition = go.GetComponent<Rigidbody>().centerOfMass;
        }
    }
}
