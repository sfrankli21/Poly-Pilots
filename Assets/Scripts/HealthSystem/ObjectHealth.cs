using UnityEngine;
using System.Collections;

public class ObjectHealth : MonoBehaviour
{
    public float health;
    public float currentHealth;
    public GameObject AliveVisual;
    public GameObject DeadVisual;
    public bool isAlive;
    public bool destroyOnDeath;
    public string bulletTag;
    public float bulletDamage;

    bool deathStarted;

    void Start()
    {
        currentHealth = health;
        isAlive = true;
        UpdateVisualState();
    }

    public void CheckAlive()
    {
        if (currentHealth <= 0f)
        {
            currentHealth = 0f;
            isAlive = false;

            UpdateVisualState();

            if (destroyOnDeath && !deathStarted)
            {
                deathStarted = true;
                StartCoroutine(Death());
            }
        }
        else
        {
            isAlive = true;
            UpdateVisualState();
        }
    }

    public void ApplyDamage(float damage)
    {
        if (!isAlive)
        {
            return;
        }

        currentHealth -= damage;
        CheckAlive();
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(bulletTag))
        {
            ApplyDamage(bulletDamage);
        }
    }

    void UpdateVisualState()
    {
        if (AliveVisual != null)
        {
            AliveVisual.SetActive(isAlive);
        }

        if (DeadVisual != null)
        {
            DeadVisual.SetActive(!isAlive);
        }
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(10f);
        Destroy(gameObject);
    }
}