using UnityEngine;

[RequireComponent(typeof(Animator))]
public class FamiliarAI : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("The object the familiar should follow (e.g., the Main Camera / Headset)")]
    public Transform targetToFollow;
    [Tooltip("How close the familiar gets before stopping")]
    public float stoppingDistance = 1.5f;
    [Tooltip("How fast the familiar runs")]
    public float moveSpeed = 2.5f;

    // Internal trackers
    private Animator animator;
    private bool isMoving = false;

    void Start()
    {
        animator = GetComponent<Animator>();
        
        // Auto-find the VR headset if a target isn't manually assigned
        if (targetToFollow == null && Camera.main != null)
        {
            targetToFollow = Camera.main.transform;
        }

        // Force the animator into an Idle state on boot to stop the crazy looping
        if (animator != null)
        {
            animator.Play("Idle_Normal_SwordAndShield"); 
        }
    }

    void Update()
    {
        if (targetToFollow == null) return;

        // Calculate distance on the X/Z plane (ignore Y so he doesn't float up to your head)
        Vector3 targetPosition = new Vector3(targetToFollow.position.x, transform.position.y, targetToFollow.position.z);
        float distanceToTarget = Vector3.Distance(transform.position, targetPosition);

        if (distanceToTarget > stoppingDistance)
        {
            // Move the familiar towards the target
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // Rotate to face the target smoothly
            Vector3 direction = (targetPosition - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
            }

            // Tell the animator we are moving
            if (!isMoving)
            {
                isMoving = true;
                animator.SetBool("IsMoving", true); // Create a boolean called "IsMoving" in your Animator!
            }
        }
        else
        {
            // We have reached the target
            if (isMoving)
            {
                isMoving = false;
                animator.SetBool("IsMoving", false);
            }
        }
    }

    public void SetNewTarget(Transform newTarget)
    {
        targetToFollow = newTarget;
    }

    public void StopMoving()
    {
        targetToFollow = null;
        if (isMoving)
        {
            isMoving = false;
            animator.SetBool("IsMoving", false);
        }
    }
}