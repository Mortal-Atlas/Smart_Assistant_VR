using UnityEngine;
using UnityEngine.InputSystem;

public class FamiliarController : MonoBehaviour
{
    [Header("RPG Stats")]
    public float maxHealth = 100f;
    public float maxMana = 50f;
    public float maxStamina = 100f;
    
    public float currentHealth { get; private set; }
    public float currentMana { get; private set; }
    public float currentStamina { get; private set; }

    [Header("Movement & Physics")]
    public float moveSpeed = 3f;
    public float jumpForce = 5f;
    public float dashForce = 10f;
    public float staminaRegenRate = 15f;
    
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
        RegenStamina();
    }

    private void HandleMovement()
    {
        if (thumbstickAxis == null) return;
        Vector2 input = thumbstickAxis.action.ReadValue<Vector2>();
        
        // Map 2D thumbstick to 3D world space (X and Z)
        Vector3 move = new Vector3(input.x, 0, input.y) * moveSpeed;
        
        // Apply movement while preserving vertical physics (gravity/jumping)
        rb.linearVelocity = new Vector3(move.x, rb.linearVelocity.y, move.z);

        // Rotate familiar to face movement direction
        if (move.magnitude > 0.1f)
        {
            transform.rotation = Quaternion.LookRotation(new Vector3(move.x, 0, move.z));
        }
    }

    private void RegenStamina()
    {
        if (currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Min(currentStamina, maxStamina);
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
            
            // Rumble phone physically via MQTT to feel the dash
            MqttQuestBridge.Instance.PublishMessage(MargoTopics.PhoneRumble, "DASH");
        }
    }

    private void DoAttack(InputAction.CallbackContext ctx)
    {
        if (currentStamina >= 15f)
        {
            currentStamina -= 15f;
            Debug.Log("Familiar Swings Sword!");
            
            // Cast a sphere in front of the familiar to hit enemies
            Collider[] hitEnemies = Physics.OverlapSphere(transform.position + transform.forward, 1f);
            foreach(var hit in hitEnemies)
            {
                AREnemy enemy = hit.GetComponent<AREnemy>();
                if (enemy != null) enemy.TakeDamage(25f);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Simple ground detection
        if (collision.gameObject.CompareTag("Ground")) isGrounded = true;
    }
}