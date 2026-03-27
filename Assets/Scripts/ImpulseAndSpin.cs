using UnityEngine;

public class ImpulseAndSpin : MonoBehaviour
{
    public Rigidbody targetRigidbody;

    public float minTorque = 1f;
    public float maxTorque = 10f;

    public float minImpulse = 1f;
    public float maxImpulse = 10f;

    public void Apply()
    {
        if (targetRigidbody == null)
        {
            return;
        }

        float randomTorque = Random.Range(minTorque, maxTorque) * (Random.value < 0.5f ? -1f : 1f);
        float randomImpulse = Random.Range(minImpulse, maxImpulse);

        targetRigidbody.AddRelativeForce(Vector3.back * randomImpulse, ForceMode.Impulse);
        targetRigidbody.AddRelativeTorque(Vector3.forward * randomTorque, ForceMode.Impulse);
    }
}