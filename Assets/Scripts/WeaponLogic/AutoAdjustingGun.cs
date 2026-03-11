using System.Collections.Generic;
using UnityEngine;
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
    public float fireSoundExitTimeSeconds;

    float fireAccumulator;
    readonly List<GameObject> bulletPool = new List<GameObject>();
    EventInstance fireLoopInstance;
    bool fireLoopInstanceCreated;
    bool fireLoopExiting;

    void Start()
    {
        currentAmmo = ammoCapacity;

        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            bulletPool.Add(bullet);
        }
    }

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

        UpdateFireAudio();

        if (inputRouter == null || !inputRouter.ShootGun || fireRateRPM <= 0f || currentAmmo <= 0 || bulletPrefab == null)
        {
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

            bullet.transform.SetPositionAndRotation(MuzzlePoint.position, MuzzlePoint.rotation);
            bullet.SetActive(true);

            currentAmmo--;
            fireAccumulator -= secondsPerShot;
        }
    }

    void UpdateFireAudio()
    {
        bool wantsToFire = inputRouter != null && inputRouter.ShootGun && currentAmmo > 0;

        if (wantsToFire)
        {
            StartFireLoop();
        }
        else
        {
            ExitFireLoop();
        }

        if (fireLoopInstanceCreated)
        {
            PLAYBACK_STATE playbackState;
            fireLoopInstance.getPlaybackState(out playbackState);

            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                fireLoopInstance.release();
                fireLoopInstanceCreated = false;
                fireLoopExiting = false;
            }
        }
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

            if (playbackState == PLAYBACK_STATE.STOPPED)
            {
                fireLoopInstance.release();
                fireLoopInstanceCreated = false;
                fireLoopExiting = false;
            }
            else if (fireLoopExiting)
            {
                fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
                fireLoopInstance.release();
                fireLoopInstanceCreated = false;
                fireLoopExiting = false;
            }
        }

        if (!fireLoopInstanceCreated)
        {
            fireLoopInstance = RuntimeManager.CreateInstance(fireLoopEvent);
            fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
            fireLoopInstance.start();
            fireLoopInstanceCreated = true;
            fireLoopExiting = false;
        }
        else
        {
            fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        }
    }

    void ExitFireLoop()
    {
        if (!fireLoopInstanceCreated || fireLoopExiting)
        {
            return;
        }

        fireLoopInstance.set3DAttributes(RuntimeUtils.To3DAttributes(transform));
        fireLoopInstance.setTimelinePosition(Mathf.RoundToInt(fireSoundExitTimeSeconds * 1000f));
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

        GameObject bullet = Instantiate(bulletPrefab);
        bullet.SetActive(false);
        bulletPool.Add(bullet);
        return bullet;
    }

    void OnDisable()
    {
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
        if (fireLoopInstanceCreated)
        {
            fireLoopInstance.stop(FMOD.Studio.STOP_MODE.IMMEDIATE);
            fireLoopInstance.release();
            fireLoopInstanceCreated = false;
            fireLoopExiting = false;
        }
    }
}