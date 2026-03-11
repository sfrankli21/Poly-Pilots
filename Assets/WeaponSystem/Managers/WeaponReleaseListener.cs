using UnityEngine;
using UnityEngine.Events;

public class WeaponReleaseListener : MonoBehaviour
{
    public int pylonIndex;
    public AircraftWeaponData.SelectedWeaponType weaponType;
    public UnityEvent OnReleaseApproved;

    bool hasReleased;

    public void Setup(int newPylonIndex, AircraftWeaponData.SelectedWeaponType newWeaponType)
    {
        pylonIndex = newPylonIndex;
        weaponType = newWeaponType;
    }

    public void TriggerReleaseApproved()
    {
        if (weaponType == AircraftWeaponData.SelectedWeaponType.MK84 && hasReleased)
        {
            return;
        }

        if (weaponType == AircraftWeaponData.SelectedWeaponType.MK84)
        {
            hasReleased = true;
        }

        Debug.Log("Weapon release approved event fired on " + gameObject.name + " | Pylon: " + pylonIndex + " | Weapon: " + weaponType);
        OnReleaseApproved.Invoke();
    }
}