using UnityEngine;

public class FamiliarController : MonoBehaviour
{
    [Header("Familiar Stats")]
    public float maxHealth = 100f;
    public float maxMana = 100f;
    public float maxStamina = 100f;

    // The true current values, exposed publicly so the CombatManager 
    // can read them, but set to private so only this script can modify them.
    public float currentHealth { get; private set; }
    public float currentMana { get; private set; }
    public float currentStamina { get; private set; }

    [Header("Regeneration Rates")]
    public float staminaRegenPerSec = 15f; // BotW style fast regen
    public float manaRegenPerSec = 2f;     // Slower magic regen

    [Header("UI & Feedback")]
    public FamiliarCombatUI combatUI; 
    public GameObject floatingDamageTextPrefab;
    public Transform damageTextSpawnPoint;

    // Example Animator reference for combat
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();

        // Initialize all stats at max
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;

        // Force UI to display full bars on boot
        UpdateAllUI();
    }

    void Update()
    {
        // --- PASSIVE REGENERATION ---
        
        // Regenerate Stamina
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenPerSec * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
        }

        // Regenerate Mana
        if (currentMana < maxMana)
        {
            currentMana += manaRegenPerSec * Time.deltaTime;
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
        }
    }

    // --- HEALTH LOGIC ---
    public void TakeDamage(float damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (combatUI != null) combatUI.UpdateHealthBar(currentHealth, maxHealth);
        ShowDamagePopup(damage);

        if (currentHealth <= 0)
        {
            if(anim != null) anim.SetTrigger("Die");
            // Add any game-over or familiar despawn logic here
        }
    }

    // Called by the InventoryManager when consuming a Health Potion
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (combatUI != null) combatUI.UpdateHealthBar(currentHealth, maxHealth);
    }

    // --- STAMINA LOGIC (Physical Attacks) ---
    public bool TryUseStamina(float cost)
    {
        if (currentStamina >= cost)
        {
            currentStamina -= cost;
            if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
            return true; // We had enough, proceed with attack
        }
        
        // Not enough stamina
        Debug.Log("Familiar is out of Stamina!");
        return false; 
    }

    // Called by the InventoryManager when consuming a Stamina Potion/Food
    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
    }

    // --- MANA LOGIC (Spells) ---
    public bool TryUseMana(float cost)
    {
        if (currentMana >= cost)
        {
            currentMana -= cost;
            if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
            return true; // We had enough, proceed with spell
        }
        
        // Not enough mana
        Debug.Log("Familiar is out of Mana!");
        return false;
    }

    // Called by the InventoryManager when consuming an MP Potion
    public void RestoreMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
    }

    // --- COMBAT EXECUTION (Called via MQTT / Phone Triggers) ---
    public void ExecuteHeavySlash()
    {
        if (TryUseStamina(30f))
        {
            if(anim != null) anim.SetTrigger("HeavySlash");
            // Add actual hit detection/damage output logic to your weapon later
        }
    }

    public void ExecuteFireball()
    {
        if (TryUseMana(25f))
        {
            if(anim != null) anim.SetTrigger("CastSpell");
            // Add projectile spawning logic here later
        }
    }

    // --- HELPER METHODS ---
    private void UpdateAllUI()
    {
        if(combatUI != null)
        {
            combatUI.UpdateHealthBar(currentHealth, maxHealth);
            combatUI.UpdateManaBar(currentMana, maxMana);
            combatUI.UpdateStaminaBar(currentStamina, maxStamina);
        }
    }

    private void ShowDamagePopup(float damage)
    {
        if (floatingDamageTextPrefab != null && damageTextSpawnPoint != null)
        {
            Vector3 jitterOffset = new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.1f, 0.1f), Random.Range(-0.3f, 0.3f));
            GameObject popup = Instantiate(floatingDamageTextPrefab, damageTextSpawnPoint.position + jitterOffset, Quaternion.identity);
            
            FloatingDamageText textScript = popup.GetComponent<FloatingDamageText>();
            if (textScript != null) textScript.Setup(damage);
        }
    }
}