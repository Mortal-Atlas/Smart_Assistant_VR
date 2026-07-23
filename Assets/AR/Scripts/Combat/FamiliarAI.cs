using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(FamiliarController))] 
public class FamiliarAI : MonoBehaviour
{
    [Header("VR Input Bindings")]
    public InputActionReference moveAction;
    public InputActionReference attackAction;
    public InputActionReference jumpAction;
    public InputActionReference dashAction;
    public InputActionReference defendAction;
    public InputActionReference cycleTargetAction;
    public InputActionReference ultimateAction;
    public InputActionReference toggleTargetAction;

    [Header("Movement & Physics")]
    public float baseMoveSpeed = 3.0f;
    public float dashSpeedMultiplier = 2.5f;
    public float dashDuration = 0.3f;
    public float jumpForce = 5.0f;
    public float gravity = -9.81f;
    public float rotationSpeed = 10f;
    public int maxJumps = 2;

    // Component References
    private CharacterController controller;
    private Animator animator;
    private FamiliarController statsController;
    private Transform mainCamera;

    // Internal State Variables
    private float verticalVelocity;
    private bool isDashing = false;
    private float dashTimer = 0f;
    private int currentJumps = 0;

    // Targeting State
    private Transform currentTarget = null;
    public Transform CurrentTarget => currentTarget;

    private List<AREnemy> availableTargets = new List<AREnemy>();
    private int currentTargetIndex = -1;
    private bool isCycleFlicked = false;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        statsController = GetComponent<FamiliarController>();
        
        if (Camera.main != null) mainCamera = Camera.main.transform;
    }

    void OnEnable()
    {
        if (moveAction != null) moveAction.action.Enable();
        if (cycleTargetAction != null) cycleTargetAction.action.Enable();
        if (defendAction != null) defendAction.action.Enable();

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
        if (ultimateAction != null)
        {
            ultimateAction.action.Enable();
            ultimateAction.action.performed += OnUltimatePressed;
        }
        if (toggleTargetAction != null)
        {
            toggleTargetAction.action.Enable();
            toggleTargetAction.action.performed += OnToggleTargetPressed;
        }
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (cycleTargetAction != null) cycleTargetAction.action.Disable();
        if (defendAction != null) defendAction.action.Disable();

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
        if (ultimateAction != null)
        {
            ultimateAction.action.Disable();
            ultimateAction.action.performed -= OnUltimatePressed;
        }
        if (toggleTargetAction != null)
        {
            toggleTargetAction.action.Disable();
            toggleTargetAction.action.performed -= OnToggleTargetPressed;
        }
    }

    void Update()
    {
        if (!controller.enabled) return;

        // Shield logic (Right Trigger)
        if (defendAction != null && statsController != null)
        {
            bool isHoldingDefend = defendAction.action.IsPressed();
            statsController.SetDefending(isHoldingDefend);
        }

        // Target Cycling Logic (Right Stick Flick)
        HandleTargetCyclingInput();

        // Validate target (Drop lock-on if enemy dies)
        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }

        HandleDashTimer();
        HandleMovementAndGravity();
    }

    private void HandleMovementAndGravity()
    {
        Vector2 inputDir = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveVector = Vector3.zero;

        bool canMove = statsController == null || !statsController.isDefending;

        if (canMove)
        {
            if (currentTarget != null)
            {
                // Z-TARGETING MODE
                Vector3 lookDir = (currentTarget.position - transform.position).normalized;
                lookDir.y = 0; 
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(lookDir), Time.deltaTime * rotationSpeed);
                }
                moveVector = transform.right * inputDir.x + transform.forward * inputDir.y;
            }
            else
            {
                // FREE ROAM MODE
                if (mainCamera != null)
                {
                    Vector3 camForward = mainCamera.forward;
                    Vector3 camRight = mainCamera.right;
                    camForward.y = 0; 
                    camRight.y = 0;
                    camForward.Normalize();
                    camRight.Normalize();

                    moveVector = camRight * inputDir.x + camForward * inputDir.y;

                    if (moveVector != Vector3.zero)
                    {
                        Quaternion targetRot = Quaternion.LookRotation(moveVector);
                        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
                    }
                }
            }
        }

        float currentSpeed = isDashing ? (baseMoveSpeed * dashSpeedMultiplier) : baseMoveSpeed;
        
        // Exhaustion override
        if (statsController != null && statsController.isExhausted)
        {
            currentSpeed = 0.8f; 
            animator.speed = 0.6f;
        }
        else 
        {
            animator.speed = 1.0f;
        }

        moveVector *= currentSpeed;

        // Double Jump & Gravity Logic
        if (controller.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f; 
            currentJumps = 0; 
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime; 
        }

        moveVector.y = verticalVelocity;
        controller.Move(moveVector * Time.deltaTime);

        Vector2 horizontalVelocity = new Vector2(controller.velocity.x, controller.velocity.z);
        bool isMoving = horizontalVelocity.magnitude > 0.1f;
        animator.SetBool("IsMoving", isMoving);
        animator.SetFloat("MoveSpeed", horizontalVelocity.magnitude);
    }

    private void OnAttackPressed(InputAction.CallbackContext context)
    {
        if (!controller.enabled) return;
        if (statsController != null && !statsController.isDefending)
        {
            statsController.ExecuteMeleeAttack(); 
        }
    }

    private void OnUltimatePressed(InputAction.CallbackContext context)
    {
        if (!controller.enabled) return;
        if (statsController != null)
        {
            statsController.ExecuteSpinAttack(); 
        }
    }

    private void OnJumpPressed(InputAction.CallbackContext context)
    {
        if (!controller.enabled) return;

        if (currentJumps < maxJumps)
        {
            verticalVelocity = jumpForce;
            currentJumps++;
            animator.SetTrigger("Jump");
        }
    }

    private void OnDashPressed(InputAction.CallbackContext context)
    {
        if (!controller.enabled) return;

        if (!isDashing)
        {
            if (statsController != null && statsController.TryUseStamina(20f))
            {
                isDashing = true;
                dashTimer = dashDuration;
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

    private void OnToggleTargetPressed(InputAction.CallbackContext context)
    {
        if (!controller.enabled) return;
        ToggleLockOn();
    }

    private void HandleTargetCyclingInput()
    {
        if (cycleTargetAction == null) return;

        Vector2 cycleInput = cycleTargetAction.action.ReadValue<Vector2>();

        if (Mathf.Abs(cycleInput.x) > 0.5f)
        {
            if (!isCycleFlicked)
            {
                isCycleFlicked = true; 
                if (cycleInput.x > 0) CycleTargetRight();
                else CycleTargetLeft();
            }
        }
        else if (Mathf.Abs(cycleInput.x) < 0.2f)
        {
            isCycleFlicked = false;
        }
    }
    
    public void ToggleLockOn()
    {
        if (currentTarget != null)
        {
            currentTarget = null;
            currentTargetIndex = -1;
        }
        else
        {
            RefreshTargetList();
            if (availableTargets.Count > 0)
            {
                currentTargetIndex = 0;
                currentTarget = availableTargets[currentTargetIndex].transform;
            }
        }
    }

    public void CycleTargetLeft()
    {
        if (currentTarget == null) return;
        RefreshTargetList();
        if (availableTargets.Count <= 1) return;

        currentTargetIndex--;
        if (currentTargetIndex < 0) currentTargetIndex = availableTargets.Count - 1;
        currentTarget = availableTargets[currentTargetIndex].transform;
    }

    public void CycleTargetRight()
    {
        if (currentTarget == null) return;
        RefreshTargetList();
        if (availableTargets.Count <= 1) return;

        currentTargetIndex++;
        if (currentTargetIndex >= availableTargets.Count) currentTargetIndex = 0;
        currentTarget = availableTargets[currentTargetIndex].transform;
    }

    private void RefreshTargetList()
    {
        availableTargets = FindObjectsByType<AREnemy>(FindObjectsSortMode.None).ToList();
        availableTargets.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
        availableTargets.Sort((a, b) => 
            Vector3.Distance(transform.position, a.transform.position)
            .CompareTo(Vector3.Distance(transform.position, b.transform.position))
        );
    }
}