using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class FamiliarController : MonoBehaviour
{
    [Header("RPG Stats")]
    public float maxHealth = 100f;
    public float maxMana = 50f;
    public float maxStamina = 100f;
    
    public float currentHealth { get; private set; }
    public float currentMana { get; private set; }
    public float currentStamina { get; private set; }

    [Header("Visual Placeholders (Greybox)")]
    [Tooltip("Drag the Cube representing your sword here. It should be a child of the Familiar.")]
    public GameObject swordCube;

    [Header("Movement & Physics")]
    public float moveSpeed = 3f;
    public float jumpForce = 5f;
    public float dashForce = 10f;
    public float staminaRegenRate = 15f;
    public float manaRegenRate = 2f;
    
    [Header("Controller Inputs (Right Hand)")]
    public InputActionReference thumbstickAxis;
    public InputActionReference jumpButton; // Usually 'A'
    public InputActionReference attackButton; // Usually 'B'
    public InputActionReference dashTrigger; // Right Trigger

    private Rigidbody rb;
    private bool isGrounded = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        currentHealth = maxHealth;
        currentMana = maxMana;
        currentStamina = maxStamina;

        // Make sure sword is hidden by default
        if (swordCube != null) swordCube.SetActive(false);
    }

    private void OnEnable()
    {
        if (jumpButton != null) { jumpButton.action.Enable(); jumpButton.action.performed += DoJump; }
        if (attackButton != null) { attackButton.action.Enable(); attackButton.action.performed += DoAttack; }
        if (dashTrigger != null) { dashTrigger.action.Enable(); dashTrigger.action.performed += DoDash; }
        if (thumbstickAxis != null) thumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        if (jumpButton != null) { jumpButton.action.performed -= DoJump; jumpButton.action.Disable(); }
        if (attackButton != null) { attackButton.action.performed -= DoAttack; attackButton.action.Disable(); }
        if (dashTrigger != null) { dashTrigger.action.performed -= DoDash; dashTrigger.action.Disable(); }
        if (thumbstickAxis != null) thumbstickAxis.action.Disable();
    }

    private void Update()
    {
        HandleMovement();
        RegenStats();
    }

    private void HandleMovement()
    {
        if (thumbstickAxis == null) return;
        Vector2 input = thumbstickAxis.action.ReadValue<Vector2>();
        
        Vector3 move = new Vector3(input.x, 0, input.y) * moveSpeed;
        
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
        }
    }

    private void RegenStats()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina = Mathf.Min(currentStamina + (staminaRegenRate * Time.deltaTime), maxStamina);
        }
        
        // Slow passive mana regeneration (based on manaRegenRate, which is currently set to 2 per second)
        if (currentMana < maxMana)
        {
            currentMana = Mathf.Min(currentMana + (manaRegenRate * Time.deltaTime), maxMana);
        }
    }

    private void DoJump(InputAction.CallbackContext ctx)
    {
        if (isGrounded && currentStamina >= 20f)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            currentStamina -= 20f;
            isGrounded = false;
        }
    }

    private void DoDash(InputAction.CallbackContext ctx)
    {
        if (currentStamina >= 30f)
        {
            Vector3 dashDir = transform.forward * dashForce;
            rb.AddForce(dashDir, ForceMode.Impulse);
            currentStamina -= 30f;
        }
    }

    private void DoAttack(InputAction.CallbackContext ctx)
    {
        if (currentStamina >= 15f)
        {
            currentStamina -= 15f;
            StartCoroutine(ShowSwordAndDamage(25f, 1.5f)); // 25 dmg, 1.5m range
        }
    }

    // Link these 4 methods directly to your Meta Poke Interactables!

    public void CastHeavySlash()
    {
        if (currentMana >= 15f)
        {
            currentMana -= 15f;
            LogToPhone("<color=#FF5555>Used Heavy Slash!</color>");
            StartCoroutine(ShowSwordAndDamage(50f, 2.0f)); // Double damage, slightly longer range
        }
        else LogToPhone("Not enough Mana!");
    }

    public void CastSpinAttack()
    {
        if (currentMana >= 25f)
        {
            currentMana -= 25f;
            LogToPhone("<color=#FFAA00>Used Spin Attack!</color>");
            
            // AOE Damage all around the familiar
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position, 3f);
            foreach(var hit in hitEnemies)
            {
                AREnemy enemy = hit.GetComponent<AREnemy>();
                if (enemy != null) enemy.TakeDamage(20f);
            }
        }
        else LogToPhone("Not enough Mana!");
    }

    public void CastHealPulse()
    {
        if (currentMana >= 30f)
        {
            currentMana -= 30f;
            Heal(40f);
            LogToPhone("<color=#55FF55>Cast Heal Pulse!</color>");
        }
        else LogToPhone("Not enough Mana!");
    }

    public void CastExecute()
    {
        // Ultimate requires a full mana bar (50)
        if (currentMana >= maxMana)
        {
            currentMana = 0; // Burn all mana
            LogToPhone("<color=#FF00FF>EXECUTE ULTIMATE!</color>");
            
            // Push forward rapidly and deal massive AOE damage
            rb.AddForce(transform.forward * (dashForce * 1.5f), ForceMode.Impulse);
            StartCoroutine(ShowSwordAndDamage(150f, 3.5f)); // 150 damage, huge range!
        }
        else LogToPhone($"Need MAX Mana for Execute! ({currentMana}/{maxMana})");
    }

    private IEnumerator ShowSwordAndDamage(float damage, float range)
    {
        // 1. Show the greybox sword cube
        if (swordCube != null) swordCube.SetActive(true);

        // 2. Deal the damage in front of the familiar
        Collider[] hitEnemies = Physics.OverlapSphere(transform.position + (transform.forward * range), range);
        bool hitSomeone = false;
        
        foreach(var hit in hitEnemies)
        {
            AREnemy enemy = hit.GetComponent<AREnemy>();
            if (enemy != null) 
            {
                enemy.TakeDamage(damage);
                hitSomeone = true;
            }
        }

        // 3. Gain Mana if we successfully hit an enemy!
        if (hitSomeone)
        {
            GainMana(5f); 
        }

        // 4. Wait a tiny fraction of a second so the user can see the sword flash
        yield return new WaitForSeconds(0.2f);

        // 5. Hide the sword again
        if (swordCube != null) swordCube.SetActive(false);
    }

    public void TakeDamage(float amount)
    {
        currentHealth = Mathf.Max(0, currentHealth - amount);
        LogToPhone($"<color=red>Familiar took {amount} damage!</color>");
        
        // Gain a chunk of Mana when taking a hit!
        GainMana(10f);
    }
    
    private void GainMana(float amount)
    {
        currentMana = Mathf.Min(currentMana + amount, maxMana);
    }

    public void Heal(float amount)
    {
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);
    }
    
    public void RestoreStamina(float amount)
    {
        currentStamina = Mathf.Min(currentStamina + amount, maxStamina);
    }

    private void LogToPhone(string msg)
    {
        // We reuse the ItemPickedUp event to cleanly send text to the fading combat log on the phone!
        InventoryManager.Instance?.AddLogMessage(msg);
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Simple ground detection
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }
}