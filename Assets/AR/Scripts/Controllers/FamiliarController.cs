using UnityEngine;
using System.Collections;

public class FamiliarController : MonoBehaviour, IDamageable
{
    [Header("Familiar Stats")]
    public float maxHealth = 100f;
    public float maxMana = 100f;
    public float maxStamina = 100f;

    public float currentHealth { get; private set; }
    public float currentMana { get; private set; }
    public float currentStamina { get; private set; }
    public bool isExhausted { get; private set; }

    [Header("Regeneration Rates")]
    public float staminaRegenPerSec = 15f; 
    public float manaRegenPerSec = 2f;     

    [Header("UI & Feedback")]
    public FamiliarCombatUI combatUI; 
    public GameObject floatingDamageTextPrefab;
    public Transform damageTextSpawnPoint;

    [Header("Physical Hitboxes")]
    public WeaponHitbox swordHitbox;
    public WeaponHitbox shieldHitbox;

    [Header("Combat & I-Frames")]
    public float invincibilityTime = 0.5f;
    private float lastHitTime = -1f;

    [Header("Combo System")]
    public float comboResetTime = 1.2f; // How long before the combo resets to 1
    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private Coroutine currentSwingCoroutine;

    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;
        isExhausted = false;

        UpdateAllUI();
    }

    void Update()
    {
        // Regenerate Stamina
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenPerSec * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
        }

        // Exhaustion Recovery
        if (isExhausted && currentStamina >= (maxStamina * 0.05f))
        {
            isExhausted = false;
        }

        // Regenerate Mana
        if (currentMana < maxMana)
        {
            currentMana += manaRegenPerSec * Time.deltaTime;
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
        }

        // Combo Timer Reset
        if (comboStep > 0 && Time.time > lastAttackTime + comboResetTime)
        {
            comboStep = 0; 
        }
    }

    public void TakeDamage(float damage)
    {
        if (Time.time < lastHitTime + invincibilityTime) return;
        lastHitTime = Time.time;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (combatUI != null) combatUI.UpdateHealthBar(currentHealth, maxHealth);
        ShowDamagePopup(damage);

        // 1. Interrupt combos and play the Hit animation!
        if (anim != null) 
        {
            anim.SetTrigger("Hit");
            comboStep = 0; // Reset combo if you get punched in the face
        }

        if (currentHealth <= 0 && anim != null)
        {
            anim.SetTrigger("Die");
        }
    }

    public void BlockAttack(float damage)
    {
        if (Time.time < lastHitTime + invincibilityTime) return;
        lastHitTime = Time.time;

        if (TryUseStamina(15f))
        {
            Debug.Log("Shield Blocked the Attack!");
            ShowDamagePopup(0f); 
        }
        else
        {
            Debug.Log("Guard Break!");
            TakeDamage(damage); // Take direct damage if out of stamina
        }
    }

    public bool TryUseStamina(float cost)
    {
        if (isExhausted) return false;

        if (currentStamina >= cost)
        {
            currentStamina -= cost;
            if (currentStamina <= 0f) isExhausted = true;
            
            if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
            return true; 
        }
        return false; 
    }

    public bool TryUseMana(float cost)
    {
        if (currentMana >= cost)
        {
            currentMana -= cost;
            if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
            return true;
        }
        return false;
    }

    // --- NEW COMBO SYSTEM ---
    public void ExecuteMeleeAttack()
    {
        if (TryUseStamina(10f)) // Normal attacks cost less stamina
        {
            // Advance the combo step
            comboStep++;
            if (comboStep > 3) comboStep = 1; // Loop back to start if mashed past 3

            lastAttackTime = Time.time;

            if (anim != null) anim.SetTrigger("Attack" + comboStep);
            
            // Standard attacks keep the hitbox alive for 0.4 seconds
            RestartSwordSwing(0.4f); 
        }
    }

    // --- NEW SPIN ATTACK (MANA) ---
    public void ExecuteSpinAttack()
    {
        if (TryUseMana(25f)) // Spin attack costs Mana instead of Stamina
        {
            if (anim != null) anim.SetTrigger("SpinAttack");
            
            // Spin attacks take much longer, so keep the sword deadly for 1.0 second!
            RestartSwordSwing(1.0f);
        }
    }

    private void RestartSwordSwing(float duration)
    {
        // If we mash attack, stop the old timer and start a fresh one so the sword doesn't accidentally turn off early
        if (currentSwingCoroutine != null) StopCoroutine(currentSwingCoroutine);
        currentSwingCoroutine = StartCoroutine(SwordSwingRoutine(duration));
    }

    private IEnumerator SwordSwingRoutine(float activeDuration)
    {
        yield return new WaitForSeconds(0.1f); // Tiny wind-up delay
        if (swordHitbox != null) swordHitbox.ActivateHitbox();
        
        yield return new WaitForSeconds(activeDuration); 
        
        if (swordHitbox != null) swordHitbox.DeactivateHitbox();
    }

    // ... (Heal, RestoreStamina, RestoreMana, ShowDamagePopup, UpdateAllUI methods remain the same)
    public void Heal(float amount)
    {
        currentHealth += amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        if (combatUI != null) combatUI.UpdateHealthBar(currentHealth, maxHealth);
    }

    public void RestoreStamina(float amount)
    {
        currentStamina += amount;
        currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
        if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
    }

    public void RestoreMana(float amount)
    {
        currentMana += amount;
        currentMana = Mathf.Clamp(currentMana, 0, maxMana);
        if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
    }

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