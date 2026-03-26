using UnityEngine;
using System.Collections.Generic;

public class ExplosiveObject : MonoBehaviour
{
    public float maxDamage = 100f;
    public float minDamage = 10f;

    public enum DamageFalloff
    {
        LinearFalloff,
        CubicFalloff,
        NoFalloff
    }

    public DamageFalloff damageFalloff = DamageFalloff.LinearFalloff;

    public float explosiveRadius = 10f;
    public bool detonateOnAwake = false;
    public bool allowMultipleDetonations = false;

    bool hasDetonated;

    void Awake()
    {
        if (detonateOnAwake)
        {
            Detonate();
        }
    }

    void OnEnable()
    {
        hasDetonated = false;
    }

    public void Detonate()
    {
        if (!allowMultipleDetonations && hasDetonated)
        {
            return;
        }

        hasDetonated = true;

        Collider[] hitColliders = Physics.OverlapSphere(transform.position, explosiveRadius);
        HashSet<ObjectHealth> damagedObjects = new HashSet<ObjectHealth>();

        for (int i = 0; i < hitColliders.Length; i++)
        {
            ObjectHealth targetHealth = hitColliders[i].GetComponentInParent<ObjectHealth>();

            if (targetHealth == null)
            {
                continue;
            }

            if (damagedObjects.Contains(targetHealth))
            {
                continue;
            }

            damagedObjects.Add(targetHealth);

            float distance = Vector3.Distance(transform.position, targetHealth.transform.position);
            float damageToApply = CalculateDamage(distance);

            targetHealth.ApplyDamage(damageToApply);
        }
    }

    float CalculateDamage(float distance)
    {
        if (damageFalloff == DamageFalloff.NoFalloff)
        {
            return maxDamage;
        }

        if (explosiveRadius <= 0f)
        {
            return maxDamage;
        }

        float normalizedDistance = Mathf.Clamp01(distance / explosiveRadius);

        if (damageFalloff == DamageFalloff.LinearFalloff)
        {
            return Mathf.Lerp(maxDamage, minDamage, normalizedDistance);
        }

        if (damageFalloff == DamageFalloff.CubicFalloff)
        {
            float cubicT = normalizedDistance * normalizedDistance * normalizedDistance;
            return Mathf.Lerp(maxDamage, minDamage, cubicT);
        }

        return maxDamage;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, explosiveRadius);
    }
}