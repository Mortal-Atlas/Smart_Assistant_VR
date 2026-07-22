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
    public bool isDefending { get; private set; }

    [Header("Regeneration Rates")]
    public float staminaRegenPerSec = 15f; 
    public float manaRegenPerSec = 2f;     

    [Header("UI & Feedback")]
    public FamiliarCombatUI combatUI; 
    public GameObject floatingDamageTextPrefab;
    public Transform damageTextSpawnPoint;

    [Header("Physical Hitboxes")]
    public WeaponHitbox swordHitbox;

    [Header("Combat & I-Frames")]
    public float invincibilityTime = 0.5f;
    private float lastHitTime = -1f;

    [Header("Combo System")]
    public float comboResetTime = 1.2f; 
    private int comboStep = 0;
    private float lastAttackTime = 0f;
    private Coroutine currentSwingCoroutine;
    private Animator anim;

    void Start()
    {
        anim = GetComponent<Animator>();
        currentHealth = maxHealth;
        currentStamina = maxStamina;
        
        // NEW: Start with 0 Mana so the player has to earn their ultimate!
        currentMana = 0f; 

        isExhausted = false;
        isDefending = false;

        UpdateAllUI();
    }

    void Update()
    {
        // Stamina logic
        if (currentStamina < maxStamina && !isDefending) // Don't regen while holding shield
        {
            currentStamina += staminaRegenPerSec * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0, maxStamina);
            if (combatUI != null) combatUI.UpdateStaminaBar(currentStamina, maxStamina);
        }

        if (isExhausted && currentStamina >= (maxStamina * 0.05f))
        {
            isExhausted = false;
        }

        // Mana logic
        if (currentMana < maxMana)
        {
            currentMana += manaRegenPerSec * Time.deltaTime;
            currentMana = Mathf.Clamp(currentMana, 0, maxMana);
            if (combatUI != null) combatUI.UpdateManaBar(currentMana, maxMana);
        }

        // Combo reset
        if (comboStep > 0 && Time.time > lastAttackTime + comboResetTime)
        {
            comboStep = 0; 
        }
    }

    public void SetDefending(bool defending)
    {
        if (isExhausted) defending = false; // Can't block if tired

        if (isDefending != defending)
        {
            isDefending = defending;
            if (anim != null) anim.SetBool("IsDefending", isDefending);
        }
    }

    public void TakeDamage(float damage)
    {
        if (Time.time < lastHitTime + invincibilityTime) return;
        
        if (isDefending)
        {
            BlockAttack(damage);
            return;
        }

        lastHitTime = Time.time;
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
        
        if (combatUI != null) combatUI.UpdateHealthBar(currentHealth, maxHealth);
        ShowDamagePopup(damage);

        if (anim != null) 
        {
            anim.SetTrigger("Hit");
            comboStep = 0; 
        }

        if (currentHealth <= 0 && anim != null)
        {
            anim.SetTrigger("Die");
        }
    }

    public void BlockAttack(float damage)
    {
        lastHitTime = Time.time;

        if (TryUseStamina(15f))
        {
            Debug.Log("Shield Blocked the Attack!");
            ShowDamagePopup(0f); 
        }
        else
        {
            Debug.Log("Guard Break!");
            isDefending = false; 
            TakeDamage(damage); 
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

    public void ExecuteMeleeAttack()
    {
        if (TryUseStamina(10f))
        {
            comboStep++;
            if (comboStep > 3) comboStep = 1; 

            lastAttackTime = Time.time;
            if (anim != null) anim.SetTrigger("Attack" + comboStep);
            
            RestartSwordSwing(0.4f); 
        }
    }

    public void ExecuteSpinAttack()
    {
        if (TryUseMana(25f))
        {
            if (anim != null) anim.SetTrigger("SpinAttack");
            RestartSwordSwing(1.0f);
        }
    }

    private void RestartSwordSwing(float duration)
    {
        if (currentSwingCoroutine != null) StopCoroutine(currentSwingCoroutine);
        currentSwingCoroutine = StartCoroutine(SwordSwingRoutine(duration));
    }

    private IEnumerator SwordSwingRoutine(float activeDuration)
    {
        yield return new WaitForSeconds(0.1f); 
        if (swordHitbox != null) swordHitbox.ActivateHitbox();
        yield return new WaitForSeconds(activeDuration); 
        if (swordHitbox != null) swordHitbox.DeactivateHitbox();
    }

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
        
        if (isExhausted && currentStamina >= (maxStamina * 0.05f))
        {
            isExhausted = false;
        }

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
            popup.SendMessage("Setup", damage, SendMessageOptions.DontRequireReceiver); 
        }
    }
}