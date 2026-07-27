using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class RikaWander : MonoBehaviour
{
    [Header("Wander Settings")]
    [Tooltip("How far Rika can wander from her current spot in one trip.")]
    public float wanderRadius = 3f;
    
    [Tooltip("How long she waits before picking a new destination.")]
    public float waitTime = 4f;

    private NavMeshAgent agent;
    private float timer;

    // Optional: If you have an Animator, drag it here to make her walk!
    [Header("Animation (Optional)")]
    public Animator rikaAnimator;
    public string speedParameter = "Speed"; 

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        timer = waitTime; // Start the timer immediately
    }

    void Update()
    {
        // Update animation speed if you have one attached
        if (rikaAnimator != null)
        {
            // agent.velocity.magnitude is 0 when still, and higher when moving
            rikaAnimator.SetFloat(speedParameter, agent.velocity.magnitude);
        }

        // If she is still pathfinding, don't pick a new spot yet
        if (agent.pathPending || agent.remainingDistance > 0.2f)
            return;

        // Timer counts up while she is standing still
        timer += Time.deltaTime;

        // When the timer hits the limit, find a new spot!
        if (timer >= waitTime)
        {
            Vector3 newDestination = GetRandomPointInRoom(transform.position, wanderRadius);
            
            // Tell the NavMeshAgent to walk to the new spot
            agent.SetDestination(newDestination);
            
            // Reset the timer
            timer = 0;
        }
    }

    // This mathematical magic picks a random point within a sphere, 
    // and then forces that point to snap to the nearest walkable floor.
    private Vector3 GetRandomPointInRoom(Vector3 origin, float distance)
    {
        Vector3 randomDirection = Random.insideUnitSphere * distance;
        randomDirection += origin;

        NavMeshHit navHit;
        // SamplePosition looks for the closest valid spot on the NavMesh.
        // -1 means "All Areas" (Walkable)
        if (NavMesh.SamplePosition(randomDirection, out navHit, distance, -1))
        {
            return navHit.position;
        }
        
        // If it somehow fails, just stay put.
        return origin;
    }

    // A handy method so you can call it from MqttQuestBridge when you talk to her!
    public void WalkToPlayer(Transform playerTransform)
    {
        if (agent != null)
        {
            agent.SetDestination(playerTransform.position);
        }
    }
}