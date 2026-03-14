using System.Collections.Generic;
using UnityEngine;
using TMPro;
using FMODUnity;
using FMOD.Studio;

public class AutoAdjustingGun : MonoBehaviour
{
    public Transform AimPointOrigin;
    public Transform MuzzlePoint;
    public LayerMask hitMask;
    public GameObject bulletPrefab;
    public float fireRateRPM;
    public InputRouter inputRouter;
    public int ammoCapacity;
    public int currentAmmo;
    public int poolSize = 100;
    public EventReference fireLoopEvent;
    public string fireExitParameterName;
    public float fireExitParameterValue = 1f;
    public List<TMP_Text> TextDisplays = new List<TMP_Text>();
    public Transform BulletPool;
    public bool AutoRefill;
    public float autoRefillDelay;
    public int trajectoryStepCount = 120;
    public float trajectoryTimeStep = 0.02f;
    public GameObject closestHitPrefab;
    public Transform closestHitBillboardTarget;
    public Vector3 closestHitMinScale = new Vector3(15f, 15f, 15f);
    public Vector3 closestHitMaxScale = new Vector3(200f, 200f, 200f);
    public float closestHitMinScaleDistance = 0f;
    public float closestHitMaxScaleDistance = 10000f;
    public GameObject toggleObjectWithLineRenderer;

    float fireAccumulator;
    float refillTimer;
    bool refillCountdownActive;
    bool wasTryingToFireLastFrame;
    readonly List<GameObject> bulletPool = new List<GameObject>();
    EventInstance fireLoopInstance;
    bool fireLoopInstanceCreated;
    bool fireLoopExiting;
    GameObject closestHitInstance;
    bulletLogic bulletData;

    void Start()
    {
        currentAmmo = ammoCapacity;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab, BulletPool);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }

        SetLineRendererState(false);
        UpdateAmmoDisplays();
    }

    void Update()
    {
        if (AimPointOrigin == null || MuzzlePoint == null)
        {
            SetLineRendererState(false);
            return;
        }

        UpdateAimFromTrajectory();
        HandleAutoRefill();
        UpdateFireAudio();

        if (inputRouter == null || !inputRouter.ShootGun || fireRateRPM <= 0f || currentAmmo <= 0 || bulletPrefab == null)
        {
            UpdateAmmoDisplays();
            return;
        }

        float secondsPerShot = 60f / fireRateRPM;
        fireAccumulator += Time.deltaTime;

        while (fireAccumulator >= secondsPerShot && currentAmmo > 0)
        {
            GameObject bullet = GetPooledBullet();
            if (bullet == null)
            {
                break;
            }

            bullet.transform.SetParent(null, true);
            bullet.transform.SetPositionAndRotation(MuzzlePoint.position, MuzzlePoint.rotation);
            bullet.SetActive(true);

            currentAmmo--;
            fireAccumulator -= secondsPerShot;
            UpdateAmmoDisplays();

            if (currentAmmo <= 0)
            {
                break;
            }
        }

        UpdateAmmoDisplays();
    }

    void LateUpdate()
    {
        UpdateClosestHitVisuals();
    }

    void UpdateAimFromTrajectory()
    {
        if (bulletPrefab == null)
        {
            ResetMuzzleYawAndHitPrefab();
            SetLineRendererState(false);
            return;
        }

        if (bulletData == null || bulletData.gameObject != bulletPrefab)
        {
            bulletData = bulletPrefab.GetComponent<bulletLogic>();
        }

        if (bulletData == null)
        {
            ResetMuzzleYawAndHitPrefab();
            SetLineRendererState(false);
            return;
        }

        Vector3 currentPosition = AimPointOrigin.position;
        Vector3 currentVelocity = AimPointOrigin.forward * bulletData.speed;
        Vector3 gravity = Physics.gravity;

        float maxLifeTime = bulletData.lifeTime;
        int maxStepsFromLifetime = trajectoryTimeStep > 0f ? Mathf.CeilToInt(maxLifeTime / trajectoryTimeStep) : trajectoryStepCount;
        int finalStepCount = Mathf.Max(2, Mathf.Min(trajectoryStepCount, maxStepsFromLifetime));

        List<Vector3> points = new List<Vector3>();
        points.Add(currentPosition);

        RaycastHit firstHit = default;
        bool foundHit = false;

        for (int i = 0; i < finalStepCount; i++)
        {
            Vector3 nextPosition = currentPosition + currentVelocity * trajectoryTimeStep + 0.5f * gravity * trajectoryTimeStep * trajectoryTimeStep;
            Vector3 nextVelocity = currentVelocity + gravity * trajectoryTimeStep;
            Vector3 segment = nextPosition - currentPosition;
            float segmentDistance = segment.magnitude;

            if (segmentDistance > 0f)
            {
                if (Physics.Raycast(currentPosition, segment.normalized, out RaycastHit hit, segmentDistance, hitMask, QueryTriggerInteraction.Ignore))
                {
                    firstHit = hit;
                    foundHit = true;
                    points.Add(hit.point);
                    break;
                }
            }

            points.Add(nextPosition);
            currentPosition = nextPosition;
            currentVelocity = nextVelocity;
        }

        Vector3 localEuler = MuzzlePoint.localEulerAngles;

        if (foundHit)
        {
            Vector3 worldDirectionToHit = firstHit.point - MuzzlePoint.position;

            if (MuzzlePoint.parent != null)
            {
                Vector3 localDirectionToHit = MuzzlePoint.parent.InverseTransformDirection(worldDirectionToHit.normalized);
                float yAngle = Mathf.Atan2(localDirectionToHit.x, localDirectionToHit.z) * Mathf.Rad2Deg;
                localEuler.y = yAngle;
            }
            else
            {
                float yAngle = Mathf.Atan2(worldDirectionToHit.x, worldDirectionToHit.z) * Mathf.Rad2Deg;
                localEuler.y = yAngle;
            }

            UpdateClosestHitPrefab(firstHit.point);
            SetLineRendererState(true);
        }
        else
        {
            localEuler.y = 0f;
            DestroyClosestHitPrefab();
            SetLineRendererState(false);
        }

        MuzzlePoint.localEulerAngles = localEuler;
    }

    void ResetMuzzleYawAndHitPrefab()
    {
        Vector3 localEuler = MuzzlePoint.localEulerAngles;
        localEuler.y = 0f;
        MuzzlePoint.localEulerAngles = localEuler;
        DestroyClosestHitPrefab();
    }

    void UpdateClosestHitPrefab(Vector3 hitPoint)
    {
        if (closestHitPrefab == null)
        {
            return;
        }

        if (closestHitInstance == null)
        {
            closestHitInstance = Instantiate(closestHitPrefab, hitPoint, Quaternion.identity);
        }
        else
        {
            closestHitInstance.transform.position = hitPoint;
        }

        UpdateClosestHitVisuals();
    }

    void UpdateClosestHitVisuals()
    {
        if (closestHitInstance == null)
        {
            return;
        }

        if (closestHitBillboardTarget != null)
        {
            Vector3 directionToTarget = closestHitBillboardTarget.position - closestHitInstance.transform.position;

            if (directionToTarget.sqrMagnitude > 0.0001f)
            {
                closestHitInstance.transform.rotation = Quaternion.LookRotation(directionToTarget.normalized, Vector3.up);
            }

            float distanceToTarget = Vector3.Distance(closestHitBillboardTarget.position, closestHitInstance.transform.position);
            float scaleT = closestHitMaxScaleDistance <= closestHitMinScaleDistance
                ? 1f
                : Mathf.InverseLerp(closestHitMinScaleDistance, closestHitMaxScaleDistance, distanceToTarget);

            closestHitInstance.transform.localScale = Vector3.Lerp(closestHitMinScale, closestHitMaxScale, scaleT);
        }
    }

    void DestroyClosestHitPrefab()
    {
        if (closestHitInstance != null)
        {
            Destroy(closestHitInstance);
            closestHitInstance = null;
        }
    }

    void SetLineRendererState(bool enabled)
    {
        if (toggleObjectWithLineRenderer != null)
        {
            toggleObjectWithLineRenderer.SetActive(!enabled);
        }
    }

    void HandleAutoRefill()
    {
        if (!AutoRefill)
        {
            refillCountdownActive = false;
            refillTimer = 0f;
            return;
        }

        if (currentAmmo > 0)
        {
            refillCountdownActive = false;
            refillTimer = 0f;
            return;
        }

        if (!refillCountdownActive)
        {
            refillCountdownActive = true;
            refillTimer = autoRefillDelay;
        }

        refillTimer -= Time.deltaTime;

        if (refillTimer <= 0f)
        {
            currentAmmo = ammoCapacity;
            refillCountdownActive = false;
            refillTimer = 0f;
            UpdateAmmoDisplays();
        }
    }

    void UpdateFireAudio()
    {
        bool wantsToFire = inputRouter != null && inputRouter.ShootGun && currentAmmo > 0;
        bool fireStartedThisFrame = wantsToFire && !wasTryingToFireLastFrame;

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

            PLAYBACK_STATE playbackState;
            fireLoopInstance.getPlaybackState(out playbackState);

            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                fireLoopInstance.release();
                fireLoopInstanceCreated = false;
                fireLoopExiting = false;
            }
        }

        if (wantsToFire)
        {
            if (fireStartedThisFrame)
            {
                RestartFireLoop();
            }
            else if (!fireLoopInstanceCreated)
            {
                StartFireLoop();
            }
        }
        else
        {
            ExitFireLoop();
        }

        wasTryingToFireLastFrame = wantsToFire;
    }

    void StartFireLoop()
    {
        if (fireLoopEvent.IsNull)
        {
            return;
        }

        if (fireLoopInstanceCreated)
        {
            PLAYBACK_STATE playbackState;
            fireLoopInstance.getPlaybackState(out playbackState);

            if (playbackState != PLAYBACK_STATE.STOPPED)
            {
                return;
            }

            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }

        fireLoopInstance = RuntimeManager.CreateInstance(fireLoopEvent);
        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, 0f);
        }

        fireLoopInstance.start();
        fireLoopInstanceCreated = true;
        fireLoopExiting = false;
    }

    void RestartFireLoop()
    {
        if (fireLoopEvent.IsNull)
        {
            return;
        }

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }

        fireLoopInstance = RuntimeManager.CreateInstance(fireLoopEvent);
        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, 0f);
        }

        fireLoopInstance.start();
        fireLoopInstanceCreated = true;
        fireLoopExiting = false;
    }

    void ExitFireLoop()
    {
        if (!fireLoopInstanceCreated || fireLoopExiting)
        {
            return;
        }

        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));

        if (!string.IsNullOrEmpty(fireExitParameterName))
        {
            fireLoopInstance.setParameterByName(fireExitParameterName, fireExitParameterValue);
        }
        else
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        }

        fireLoopExiting = true;
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

        GameObject bullet = Instantiate(bulletPrefab, BulletPool);
        bullet.SetActive(false);
        bulletPool.Add(bullet);
        return bullet;
    }

    void UpdateAmmoDisplays()
    {
        string ammoText = currentAmmo.ToString();

        for (int i = 0; i < TextDisplays.Count; i++)
        {
            if (TextDisplays[i] != null)
            {
                TextDisplays[i].text = ammoText;
            }
        }
    }

    public void ReturnBulletToPool(GameObject bullet)
    {
        if (bullet == null)
        {
            return;
        }

        bullet.SetActive(false);

        if (BulletPool != null)
        {
            bullet.transform.SetParent(BulletPool, false);
        }
        else
        {
            bullet.transform.SetParent(transform, false);
        }
    }

    void OnDisable()
    {
        wasTryingToFireLastFrame = false;
        DestroyClosestHitPrefab();
        SetLineRendererState(false);

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }
    }

    void OnDestroy()
    {
        DestroyClosestHitPrefab();

        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }
    }
}