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

    public void CheckAlive()
    {
        
        if (currentHealth == 0)
        {
            isAlive = false;

            if (isAlive == false)
            {
                AliveVisual.SetActive(false);
                DeadVisual.SetActive(true);
                if(destroyOnDeath==true)
                {
                    StartCoroutine(Death());
                }
            }
            if (isAlive == true)
            {
                AliveVisual.SetActive(true);
                DeadVisual.SetActive(false);
            }
        }
    }

    public void ApplyDamage(float damage)
    {
        if (damage <= currentHealth)
        {
            currentHealth -= damage;
        }

        if (damage >= currentHealth)
        {
            currentHealth = 0;
        }
        CheckAlive();
    }
    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag(bulletTag))
        {
            ApplyDamage(bulletDamage);
        }
    }

    IEnumerator Death()
    {
        yield return new WaitForSeconds(10.00f);
        Destroy(this.gameObject);
    }





    void Start()
    {
        currentHealth = health;
        isAlive = true;
        
    }


    
    
    void Update()
    {
        
    }
}
