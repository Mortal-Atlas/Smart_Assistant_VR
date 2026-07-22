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
    [Tooltip("Bind to Left Thumbstick")]
    public InputActionReference moveAction;    
    [Tooltip("Bind to Right Controller 'A' Button")]
    public InputActionReference attackAction;  
    [Tooltip("Bind to Right Controller 'B' Button")]
    public InputActionReference defendAction;    
    [Tooltip("Bind to Right Grip (or Thumbstick Click) to Lock On")]
    public InputActionReference lockOnAction;   

    [Header("Movement Settings")]
    public float baseMoveSpeed = 3.0f;
    public float exhaustedSpeed = 0.8f;
    public float gravity = -9.81f;

    [Header("Z-Targeting Settings")]
    public float rotationSpeed = 10f;
    
    private CharacterController controller;
    private Animator animator;
    private FamiliarController statsController;
    private Transform mainCamera;

    private float verticalVelocity;

    private Transform currentTarget = null;
    private List<AREnemy> availableTargets = new List<AREnemy>();

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
        if (attackAction != null)
        {
            attackAction.action.Enable();
            attackAction.action.performed += OnAttackPressed;
        }
        if (lockOnAction != null)
        {
            lockOnAction.action.Enable();
            lockOnAction.action.performed += OnLockOnPressed;
        }
        if (defendAction != null) defendAction.action.Enable();
    }

    void OnDisable()
    {
        if (moveAction != null) moveAction.action.Disable();
        if (attackAction != null)
        {
            attackAction.action.Disable();
            attackAction.action.performed -= OnAttackPressed;
        }
        if (lockOnAction != null)
        {
            lockOnAction.action.Disable();
            lockOnAction.action.performed -= OnLockOnPressed;
        }
        if (defendAction != null) defendAction.action.Disable();
    }

    void Update()
    {
        // 1. Handle Defend Button (Held down)
        if (defendAction != null && statsController != null)
        {
            bool isHoldingDefend = defendAction.action.IsPressed();
            statsController.SetDefending(isHoldingDefend);
        }

        // 2. Validate Target (Drop target if it dies)
        if (currentTarget != null && !currentTarget.gameObject.activeInHierarchy)
        {
            currentTarget = null;
        }

        HandleMovementAndGravity();
    }

    private void HandleMovementAndGravity()
    {
        Vector2 inputDir = moveAction != null ? moveAction.action.ReadValue<Vector2>() : Vector2.zero;
        Vector3 moveVector = Vector3.zero;

        // Block movement if actively defending and not exhausted
        bool canMove = statsController == null || !statsController.isDefending;

        if (canMove)
        {
            if (currentTarget != null)
            {
                // Z-TARGETING STRAFE
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
                // FREE ROAM
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

        float currentSpeed = baseMoveSpeed;
        if (statsController != null && statsController.isExhausted)
        {
            currentSpeed = exhaustedSpeed;
            animator.speed = 0.6f;
        }
        else 
        {
            animator.speed = 1.0f;
        }

        moveVector *= currentSpeed;

        if (controller.isGrounded)
        {
            if (verticalVelocity < 0) verticalVelocity = -2f; 
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
        // Don't allow attacking while holding the shield up
        if (statsController != null && !statsController.isDefending) 
        {
            statsController.ExecuteMeleeAttack(); 
        }
    }

    private void OnLockOnPressed(InputAction.CallbackContext context)
    {
        if (currentTarget != null)
        {
            currentTarget = null;
            Debug.Log("Unlocked Target");
        }
        else
        {
            availableTargets = FindObjectsByType<AREnemy>(FindObjectsSortMode.None).ToList();
            availableTargets.RemoveAll(enemy => enemy == null || !enemy.gameObject.activeInHierarchy);
            
            if (availableTargets.Count > 0)
            {
                // Find closest
                availableTargets.Sort((a, b) => 
                    Vector3.Distance(transform.position, a.transform.position)
                    .CompareTo(Vector3.Distance(transform.position, b.transform.position))
                );
                
                currentTarget = availableTargets[0].transform;
                Debug.Log($"Locked onto: {availableTargets[0].enemyName}");
            }
        }
    }
}