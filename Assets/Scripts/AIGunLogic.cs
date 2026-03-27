using System.Collections.Generic;
using UnityEngine;

public class AIGunLogic : MonoBehaviour
{
    public GameObject bulletPrefab;
    public Transform pooledObjectsParent;
    public int poolSize = 50;
    public Transform bulletFirePoint;

    public Transform sphereCastOrigin;
    public LayerMask hitMask;
    public float sphereCastRadius = 1f;
    public float sphereCastRange = 1000f;

    public float burstGap = 1f;
    public float rateOfFireRPM = 600f;
    public int shotsPerBurst = 10;

    public bool hasHit;
    public RaycastHit currentHit;
    public bool isShooting;

    List<GameObject> bulletPool = new List<GameObject>();

    float gapTimer;
    float shotTimer;
    int shotsFiredThisBurst;

    void Start()
    {
        InitializePool();
    }

    void Update()
    {
        UpdateSphereCast();
        UpdateBurstLogic();
    }

    void InitializePool()
    {
        bulletPool.Clear();

        if (bulletPrefab == null)
        {
            return;
        }

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, pooledObjectsParent);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }

    void UpdateSphereCast()
    {
        if (sphereCastOrigin == null)
        {
            hasHit = false;
            return;
        }

        hasHit = Physics.SphereCast(
            sphereCastOrigin.position,
            sphereCastRadius,
            sphereCastOrigin.forward,
            out currentHit,
            sphereCastRange,
            hitMask,
            QueryTriggerInteraction.Ignore
        );
    }

    void UpdateBurstLogic()
    {
        if (!hasHit)
        {
            isShooting = false;
            gapTimer = 0f;
            shotTimer = 0f;
            shotsFiredThisBurst = 0;
            return;
        }

        if (isShooting)
        {
            if (shotsFiredThisBurst >= shotsPerBurst)
            {
                isShooting = false;
                gapTimer = burstGap;
                shotTimer = 0f;
                shotsFiredThisBurst = 0;
                return;
            }

            shotTimer -= Time.deltaTime;

            if (shotTimer <= 0f)
            {
                FireBullet();
                shotsFiredThisBurst++;
                shotTimer = 60f / rateOfFireRPM;
            }
        }
        else
        {
            if (gapTimer > 0f)
            {
                gapTimer -= Time.deltaTime;
            }
            else
            {
                isShooting = true;
                shotTimer = 0f;
                shotsFiredThisBurst = 0;
            }
        }
    }

    void FireBullet()
    {
        if (bulletFirePoint == null)
        {
            return;
        }

        GameObject bullet = GetPooledBullet();

        if (bullet == null)
        {
            return;
        }

        bullet.transform.SetPositionAndRotation(bulletFirePoint.position, bulletFirePoint.rotation);
        bullet.SetActive(true);
    }

    GameObject GetPooledBullet()
    {
        for (int i = 0; i < bulletPool.Count; i++)
        {
            if (!bulletPool[i].activeInHierarchy)
            {
                return bulletPool[i];
            }
        }

        return null;
    }
}