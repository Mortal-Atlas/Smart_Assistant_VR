using UnityEngine;
using System.Collections;

public class FishShadow : MonoBehaviour
{
    public enum FishSize { Small, Medium, Large }
    
    [Header("Fish Properties")]
    public FishSize size = FishSize.Medium;
    public float swimSpeed = 1.0f;
    public float turnSpeed = 2.0f;
    public float wanderRadius = 3.0f;

    private Vector3 startPosition;
    private Vector3 currentTarget;
    private bool isInvestigating = false;
    private Transform lureTarget;

    void Start()
    {
        startPosition = transform.position;
        PickNewWanderTarget();
    }

    void Update()
    {
        // If we are investigating a lure, our target is the lure's position
        if (isInvestigating && lureTarget != null)
        {
            currentTarget = new Vector3(lureTarget.position.x, transform.position.y, lureTarget.position.z);
        }

        // Move towards the target
        transform.position = Vector3.MoveTowards(transform.position, currentTarget, swimSpeed * Time.deltaTime);

        // Rotate smoothly towards the target
        Vector3 direction = (currentTarget - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            // Because our quad is laying flat (X = 90), we might need to adjust the rotation.
            // Typically, rotating the Y axis handles left/right steering for a flat object.
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, turnSpeed * Time.deltaTime);
            // Ensure it stays flat
            transform.eulerAngles = new Vector3(90f, transform.eulerAngles.y, 0f); 
        }

        // If we reached our wander point and we aren't chasing a lure, pick a new point
        if (!isInvestigating && Vector3.Distance(transform.position, currentTarget) < 0.2f)
        {
            PickNewWanderTarget();
        }
    }

    private void PickNewWanderTarget()
    {
        // Pick a random point in a circle around the start position
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        currentTarget = startPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }

    /// <summary>
    /// Called by the FishingController when the lure hits the water nearby.
    /// </summary>
    public void Investigate(Transform lure)
    {
        isInvestigating = true;
        lureTarget = lure;
        swimSpeed *= 1.5f; // Swim a little faster when excited
    }
}