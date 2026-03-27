using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using FMODUnity;

[RequireComponent(typeof(Rigidbody))]
public class EnemyBulletLogic : MonoBehaviour
{
    [System.Serializable]
    public class ImpactPerMaterial
    {
        public string tagName;
        public GameObject impactPrefab;
        public EventReference impactFMODEvent;
    }

    public float speed;
    public float lifeTime;
    public LayerMask hitDetectionMask;
    public ImpactPerMaterial[] ImpactsPerMaterial;
    public UnityEvent OnBeforeReturnToPool;

    Rigidbody rb;
    float timer;
    bool isReturning;

    static readonly Dictionary<string, GameObject> pooledImpacts = new Dictionary<string, GameObject>();
    static readonly HashSet<string> initializedPools = new HashSet<string>();

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InitializeImpactPools();
    }

    void OnEnable()
    {
        timer = lifeTime;
        isReturning = false;
        rb.useGravity = true;
        rb.linearVelocity = transform.forward * speed;
        rb.angularVelocity = Vector3.zero;
    }

    void Update()
    {
        if (isReturning)
        {
            return;
        }

        timer -= Time.deltaTime;

        if (timer <= 0f)
        {
            StartCoroutine(DelayedReturnToPool());
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (isReturning)
        {
            return;
        }

        Collider other = collision.collider;

        if (other == null)
        {
            return;
        }

        if (other.isTrigger)
        {
            return;
        }

        if (((1 << other.gameObject.layer) & hitDetectionMask.value) == 0)
        {
            return;
        }

        ContactPoint contact = collision.GetContact(0);

        for (int i = 0; i < ImpactsPerMaterial.Length; i++)
        {
            if (ImpactsPerMaterial[i].tagName == other.tag)
            {
                if (ImpactsPerMaterial[i].impactPrefab != null)
                {
                    string key = GetImpactKey(ImpactsPerMaterial[i].tagName, ImpactsPerMaterial[i].impactPrefab);

                    if (!pooledImpacts.ContainsKey(key) || pooledImpacts[key] == null)
                    {
                        GameObject newImpact = Instantiate(ImpactsPerMaterial[i].impactPrefab);
                        newImpact.SetActive(false);
                        pooledImpacts[key] = newImpact;
                    }

                    GameObject impact = pooledImpacts[key];
                    impact.SetActive(false);
                    impact.transform.SetPositionAndRotation(contact.point, Quaternion.LookRotation(contact.normal));
                    impact.SetActive(true);
                }

                if (!ImpactsPerMaterial[i].impactFMODEvent.IsNull)
                {
                    RuntimeManager.PlayOneShot(ImpactsPerMaterial[i].impactFMODEvent, contact.point);
                }

                break;
            }
        }

        StartCoroutine(DelayedReturnToPool());
    }

    void OnDisable()
    {
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
    }

    IEnumerator DelayedReturnToPool()
    {
        isReturning = true;
        yield return new WaitForSeconds(0.5f);
        OnBeforeReturnToPool.Invoke();
        gameObject.SetActive(false);
    }

    void InitializeImpactPools()
    {
        for (int i = 0; i < ImpactsPerMaterial.Length; i++)
        {
            if (ImpactsPerMaterial[i].impactPrefab == null || string.IsNullOrEmpty(ImpactsPerMaterial[i].tagName))
            {
                continue;
            }

            string key = GetImpactKey(ImpactsPerMaterial[i].tagName, ImpactsPerMaterial[i].impactPrefab);

            if (initializedPools.Contains(key))
            {
                continue;
            }

            GameObject impact = Instantiate(ImpactsPerMaterial[i].impactPrefab);
            impact.SetActive(false);
            pooledImpacts[key] = impact;
            initializedPools.Add(key);
        }
    }

    string GetImpactKey(string tagName, GameObject prefab)
    {
        return tagName + "_" + prefab.GetInstanceID();
    }
}