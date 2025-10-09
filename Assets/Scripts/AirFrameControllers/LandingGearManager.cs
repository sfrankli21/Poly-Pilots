using UnityEngine;

public class LGManager : MonoBehaviour
{
    [Header("State")]
    public bool GearDeployed = true;
    public bool GearDestroyed = false;

    [Header("Landing Gear")]
    public GameObject[] LandingGear;
    public GameObject HookBody;

    [System.Serializable]
    public class DestroyLandingGearSection
    {
        public Rigidbody RootAircraft;
        public float MaxSpeedKnots;
        public float WarningSpeedKnots;
    }

    [Header("Destroy Landing Gear")]
    public DestroyLandingGearSection DestroyLandingGear;

    public void DeployGear()
    {
        GearDeployed = true;
        for (int i = 0; i < LandingGear.Length; i++) LandingGear[i].SetActive(true);
        HookBody.SetActive(true);
    }

    public void RetractGear()
    {
        GearDeployed = false;
        for (int i = 0; i < LandingGear.Length; i++) LandingGear[i].SetActive(false);
        HookBody.SetActive(false);
    }
}
