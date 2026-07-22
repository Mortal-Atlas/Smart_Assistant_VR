using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class AREnemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    public string enemyName = "Skeleton";
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Movement & Combat")]
    public float walkSpeed = 1.0f;
    public float runSpeed = 2.5f;
    public float aggroRadius = 4.0f;
    public float attackRadius = 1.5f;
    public float attackDamage = 15f;
    public float attackCooldown = 2.5f;

    private Transform target;
    private Animator anim;
    private Rigidbody rb;

    private enum State { Wandering, Chasing, Attacking, Dead }
    private State currentState;

    private Vector3 wanderTarget;
    private float wanderTimer;
    private float lastAttackTime;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        
        // Physics Settings for Skeletons
        rb.isKinematic = true; 
        rb.useGravity = false;
        
        currentState = State.Wandering;
        PickNewWanderTarget();
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        FindFamiliarTarget();

        switch (currentState)
        {
            case State.Wandering:
                HandleWandering();
                break;
            case State.Chasing:
                HandleChasing();
                break;
            case State.Attacking:
                // If attack animation is playing, wait for the cooldown to finish
                if (Time.time >= lastAttackTime + attackCooldown)
                {
                    currentState = State.Chasing;
                }
                break;
        }
    }

    private void FindFamiliarTarget()
    {
        if (target == null)
        {
            // Unity 6 standard - Replaces FindObjectOfType
            FamiliarController familiar = Object.FindFirstObjectByType<FamiliarController>();
            if (familiar != null) target = familiar.transform;
        }

        if (target != null && currentState != State.Attacking)
        {
            float dist = Vector3.Distance(transform.position, target.position);
            
            if (dist <= attackRadius)
            {
                currentState = State.Attacking;
                ExecuteAttack();
            }
            else if (dist <= aggroRadius)
            {
                currentState = State.Chasing;
            }
            else
            {
                currentState = State.Wandering;
            }
        }
    }

    private void HandleWandering()
    {
        wanderTimer -= Time.deltaTime;
        if (wanderTimer <= 0 || Vector3.Distance(transform.position, wanderTarget) < 0.5f)
        {
            PickNewWanderTarget();
        }

        MoveTowards(wanderTarget, walkSpeed);
    }

    private void HandleChasing()
    {
        if (target == null) return;
        MoveTowards(target.position, runSpeed);
    }

    private void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 direction = (destination - transform.position).normalized;
        direction.y = 0; // Keep rotation strictly horizontal

        if (direction != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            transform.position += direction * speed * Time.deltaTime;
            
            // Pass the current speed to the Animator so it knows whether to Walk or Run!
            anim.SetFloat("MoveSpeed", speed);
        }
        else
        {
            anim.SetFloat("MoveSpeed", 0f);
        }
    }

    private void PickNewWanderTarget()
    {
        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
            // The exact 5 arguments required by the new Meta MRUK update
            bool foundSpot = MRUK.Instance.GetCurrentRoom().GenerateRandomPositionOnSurface(
                MRUK.SurfaceType.FACING_UP,
                0.1f,
                new LabelFilter(MRUKAnchor.SceneLabels.FLOOR),
                out Vector3 pos,
                out Vector3 norm
            );

            if (foundSpot)
            {
                wanderTarget = pos;
                wanderTimer = 5f; 
                return;
            }
        }

        // Fallback if MRUK is not loaded or fails
        wanderTarget = transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        wanderTimer = 3f;
    }

    private void ExecuteAttack()
    {
        // Stop moving
        anim.SetFloat("MoveSpeed", 0f);
        anim.SetTrigger("Attack");
        lastAttackTime = Time.time;

        StartCoroutine(DamageSphereRoutine());
    }

    private IEnumerator DamageSphereRoutine()
    {
        // Wait 0.5s for the skeleton's physical swing to reach the player
        yield return new WaitForSeconds(0.5f); 

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.0f, 1.0f);
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue; // Don't hit ourselves

            // 1. Check for a shield block FIRST!
            ShieldBlocker shield = hit.GetComponent<ShieldBlocker>();
            if (shield != null)
            {
                shield.TakeDamage(attackDamage);
                yield break; // Stop checking after a block
            }

            // 2. If no shield, check for the body
            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                yield break; // Stop checking after hitting the body
            }
        }
    }

    public void TakeDamage(float amount)
    {
        if (currentState == State.Dead) return;

        currentHealth -= amount;
        anim.SetTrigger("Hit");

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        currentState = State.Dead;
        anim.SetFloat("MoveSpeed", 0f);
        anim.SetTrigger("Die");
        
        // Disable physics so the dead body doesn't block the player
        rb.isKinematic = true;
        GetComponent<CapsuleCollider>().enabled = false;
        
        Destroy(gameObject, 3f); 
    }
}