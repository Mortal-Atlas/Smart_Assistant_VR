using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AREnemy : MonoBehaviour
{
    [Header("Enemy Stats")]
    public string enemyName = "Slime";
    public int enemyLevel = 1;
    public float maxHealth = 50f;
    public int expValue = 25;
    private float currentHealth;

    [Header("UI References")]
    public TMP_Text nameText;
    public TMP_Text levelText;
    public Image healthBarFill;

    [Header("Prefabs")]
    public GameObject deathSmokePrefab;
    
    [Header("Damage Popup Settings")]
    public GameObject floatingDamageTextPrefab; 
    public Transform damageTextSpawnPoint; 
    [Tooltip("How far the damage numbers can scatter from the spawn point")]
    public float spawnJitterX = 0.3f;
    public float spawnJitterY = 0.1f;
    public float spawnJitterZ = 0.3f;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        UpdateUI();
        ShowDamagePopup(damage);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void UpdateUI()
    {
        if (nameText != null) nameText.text = enemyName;
        if (levelText != null) levelText.text = "Lv. " + enemyLevel.ToString();
        if (healthBarFill != null) healthBarFill.fillAmount = currentHealth / maxHealth;
    }

    private void ShowDamagePopup(float damage)
    {
        if (floatingDamageTextPrefab != null && damageTextSpawnPoint != null)
        {
            // Calculate a random scatter position
            Vector3 jitterOffset = new Vector3(
                Random.Range(-spawnJitterX, spawnJitterX),
                Random.Range(-spawnJitterY, spawnJitterY),
                Random.Range(-spawnJitterZ, spawnJitterZ)
            );
            
            Vector3 finalSpawnPosition = damageTextSpawnPoint.position + jitterOffset;

            // Spawn the floating text at the jittered position
            GameObject popup = Instantiate(floatingDamageTextPrefab, finalSpawnPosition, Quaternion.identity);
            
            // Pass the damage amount
            FloatingDamageText textScript = popup.GetComponent<FloatingDamageText>();
            if (textScript != null)
            {
                textScript.Setup(damage);
            }
        }
    }

    private void Die()
    {
        // Spawn smoke effect
        if (deathSmokePrefab != null)
        {
            Instantiate(deathSmokePrefab, transform.position, Quaternion.identity);
        }

        // Give player EXP and Loot via the Inventory Manager
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddExp(expValue);
            InventoryManager.Instance.AddLoot("Monster Dust", Random.Range(1, 4));
        }

        Destroy(gameObject);
    }
}