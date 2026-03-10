using UnityEngine;

public class AutoAdjustingGun : MonoBehaviour
{
    public Transform AimPointOrigin;
    public Transform MuzzlePoint;
    public LayerMask hitMask;
    public GameObject bulletPrefab;
    public float fireRateRPM;
    public InputRouter inputRouter;

    float nextFireTime;

    void Update()
    {
        if (AimPointOrigin == null || MuzzlePoint == null)
        {
            return;
        }

        Ray ray = new Ray(AimPointOrigin.position, AimPointOrigin.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, 2000f, hitMask, QueryTriggerInteraction.Ignore))
        {
            Vector3 localTargetDirection = MuzzlePoint.parent != null
                ? MuzzlePoint.parent.InverseTransformDirection(hit.point - MuzzlePoint.position)
                : hit.point - MuzzlePoint.position;

            float yAngle = Mathf.Atan2(localTargetDirection.x, localTargetDirection.z) * Mathf.Rad2Deg;
            Vector3 localEuler = MuzzlePoint.localEulerAngles;
            localEuler.y = yAngle;
            MuzzlePoint.localEulerAngles = localEuler;
        }
        else
        {
            Vector3 localEuler = MuzzlePoint.localEulerAngles;
            localEuler.y = 0f;
            MuzzlePoint.localEulerAngles = localEuler;
        }

        if (inputRouter != null && inputRouter.ShootGun && fireRateRPM > 0f && Time.time >= nextFireTime)
        {
            Instantiate(bulletPrefab, MuzzlePoint.position, MuzzlePoint.rotation);
            nextFireTime = Time.time + (60f / fireRateRPM);
        }
    }
}