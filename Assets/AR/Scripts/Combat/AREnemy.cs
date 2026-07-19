using UnityEngine;

public class AREnemy : MonoBehaviour
{
    public float health = 50f;
    public int expValue = 25;
    public GameObject deathSmokePrefab; // Assign a particle system here

    public void TakeDamage(float damage)
    {
        health -= damage;
        
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        // Spawn smoke effect
        if (deathSmokePrefab != null)
        {
            Instantiate(deathSmokePrefab, transform.position, Quaternion.identity);
        }

        // Give player EXP and Loot via the new Inventory Manager
        InventoryManager.Instance.AddExp(expValue);
        InventoryManager.Instance.AddLoot("Monster Dust", Random.Range(1, 4));

        // Destroy the enemy model
        Destroy(gameObject);
    }
}