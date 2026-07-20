using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

// Automatically adds a Character Controller for physics/gravity
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(FamiliarController))] // We need the stats script to use stamina
public class FamiliarAI : MonoBehaviour
{
    [Header("VR Input Bindings (Right Controller)")]
    public InputActionReference moveAction;    // Right Thumbstick
    public InputActionReference attackAction;  // Right Trigger
    public InputActionReference jumpAction;    // B Button
    public InputActionReference dashAction;    // A Button

    [Header("Movement Settings")]
    public float baseMoveSpeed = 3.0f;
    public float dashSpeedMultiplier = 2.5f;
    public float dashDuration = 0.3f;
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;

    [Header("Z-Targeting Settings")]
    [Tooltip("How fast the familiar rotates to face you or the enemy")]
    public float rotationSpeed = 10f;
    
    // Internal State
    private CharacterController controller;
    private Animator animator;
    private FamiliarController statsController;
    private Transform mainCamera;

    private float verticalVelocity;
    private bool isDashing = false;
    private float dashTimer = 0f;

    // Targeting State
    private Transform currentTarget = null;
    private List<AREnemy> availableTargets = new List<AREnemy>();
    private int currentTargetIndex = -1;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        statsController = GetComponent<FamiliarController>();
        
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void OnEnable()
    {
        // Enable input reading
        if (moveAction != null) moveAction.action.Enable();
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttackPressed;
        }
        if (jumpAction != null)
        {
            jumpAction.action.Enable();
            jumpAction.action.performed += OnJumpPressed;
        }
        if (dashAction != null)
        {
            dashAction.action.Enable();
            dashAction.action.performed += OnDashPressed;
        }
    }

    void OnDisable()
    {
        // Clean up inputs to prevent memory leaks
        if (moveAction != null) moveAction.action.Disable();
        if (attackAction != null)
        {
            attackAction.action.Disable();
            attackAction.action.performed -= OnAttackPressed;
        }
        if (jumpAction != null)
        {
            jumpAction.action.Disable();
            jumpAction.action.performed -= OnJumpPressed;
        }
        if (dashAction != null)
        {
            dashAction.action.Disable();
            dashAction.action.performed -= OnDashPressed;
        }
    }

    void Update()
    {
        HandleDashTimer();
        HandleMovementAndGravity();
    }

    private void HandleMovementAndGravity()
    {
        // 1. Read Thumbstick Input
        Vector2 inputDir = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveVector = Vector3.zero;

        // 2. Determine Movement Direction based on Targeting State
        if (currentTarget != null)
        {
            // --- Z-TARGETING MODE ---
            // Always face the enemy
            Vector3 lookDir = (currentTarget.position - transform.position).normalized;
            lookDir.y = 0; // Keep rotation strictly horizontal
            if (lookDir != Vector3.zero)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * rotationSpeed);
            }

            // Move relative to the familiar's own facing direction (Strafe / Advance)
            moveVector = transform.right * inputDir.x + transform.forward * inputDir.y;
        }
        else
        {
            // --- FREE ROAM MODE ---
            // Move relative to where the VR Camera is looking
            if (mainCamera != null)
            {
                Vector3 camForward = mainCamera.forward;
                Vector3 camRight = mainCamera.right;
                camForward.y = 0; // Flatten on XZ plane
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                moveVector = camRight * inputDir.x + camForward * inputDir.y;

                // Rotate familiar to face the direction it is running
                if (moveVector != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(moveVector);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                }
            }
        }

        // 3. Apply Speed (check if dashing)
        float currentSpeed = isDashing ? (baseMoveSpeed * dashSpeedMultiplier) : baseMoveSpeed;
        moveVector *= currentSpeed;

        // 4. Handle Gravity
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f; // Stick to ground
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime; // Apply falling
        }

        // Apply vertical velocity to final movement
        moveVector.y = verticalVelocity;

        // 5. Move the Character
        controller.Move(moveVector * Time.deltaTime);

        // 6. Update Animator
        Vector2 horizontalVelocity = new Vector2(controller.velocity.x, controller.velocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
    }

    private void OnAttackPressed(InputAction.CallbackContext context)
    {
        // Trigger the attack via the stats controller
        if (statsController != null)
        {
            // ExecuteHeavySlash handles the stamina check and animator trigger!
            statsController.ExecuteHeavySlash(); 
        }
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (controller.isGrounded)
        {
            // If the user wants jumping to cost stamina, you can wrap this in a TryUseStamina check!
            verticalVelocity = jumpForce;
            animator.SetTrigger("Jump"); // Add a "Jump" trigger to your Animator
        }
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        if (!isDashing && controller.isGrounded)
        {
            // Dash costs 20 Stamina
            if (statsController != null && statsController.TryUseStamina(20f))
            {
                isDashing = true;
                dashTimer = dashDuration;
                
                // Add a "Dash" trigger or bool to your animator if you have a dodge roll animation!
                // animator.SetTrigger("Dash"); 
            }
        }
    }

    private void HandleDashTimer()
    {
        if (isDashing)
        {
            dashTimer -= Time.deltaTime;
            if (dashTimer <= 0)
            {
                isDashing = false;
            }
        }
    }

    
    // Link this to your "Lock On" UI Button
    public void ToggleLockOn()
    {
        if (currentTarget != null)
        {
            // Unlock
            currentTarget = null;
            currentTargetIndex = -1;
            Debug.Log("Targeting Disabled");
        }
        else
        {
            // Try to find a target
            RefreshTargetList();
            if (availableTargets.Count > 0)
            {
                currentTargetIndex = 0;
                currentTarget = availableTargets[currentTargetIndex].transform;
                Debug.Log($"Locked onto: {availableTargets[currentTargetIndex].enemyName}");
            }
        }
    }

    // Link this to your "Switch Target Left" UI Button
    public void CycleTargetLeft()
    {
        if (currentTarget == null) return;
        
        RefreshTargetList();
        if (availableTargets.Count <= 1) return;

        currentTargetIndex--;
        if (currentTargetIndex < 0) currentTargetIndex = availableTargets.Count - 1; // Wrap around

        currentTarget = availableTargets[currentTargetIndex].transform;
    }

    // Link this to your "Switch Target Right" UI Button
    public void CycleTargetRight()
    {
        if (currentTarget == null) return;
        
        RefreshTargetList();
        if (availableTargets.Count <= 1) return;

        currentTargetIndex++;
        if (currentTargetIndex >= availableTargets.Count) currentTargetIndex = 0; // Wrap around

        currentTarget = availableTargets[currentTargetIndex].transform;
    }

    private void RefreshTargetList()
    {
        // Find all active enemies in the scene
        availableTargets = FindObjectsOfType<AREnemy>().ToList();
        
        // Remove dead enemies just in case
        availableTargets.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);

        // Sort them by distance to the familiar so Target 0 is always the closest
        availableTargets.Sort((a, b) => 
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );
    }
}