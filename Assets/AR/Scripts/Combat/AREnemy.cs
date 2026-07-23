using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine.UI; 
using TMPro;          

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(CapsuleCollider))]
public class AREnemy : MonoBehaviour, IDamageable
{
    [Header("Enemy Stats")]
    public string enemyName = "Skeleton";
    public int level = 1;
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Enemy UI")]
    public Image healthBarFill;  
    public TMP_Text nameText;    
    public TMP_Text levelText;   

    [Header("Movement & Combat")]
    public float walkSpeed = 1.0f;
    public float runSpeed = 2.5f;
    public float aggroRadius = 4.0f;
    public float attackRadius = 1.5f;
    public float attackDamage = 15f;
    public float attackCooldown = 2.5f;
    
    [Header("Combat Feel")]
    [Tooltip("How hard the enemy is pushed back when hit.")]
    public float knockbackForce = 5f;
    [Tooltip("How long the AI pauses to allow the physics to push it.")]
    public float knockbackDuration = 0.2f;

    private Transform target;
    private Animator anim;
    private Rigidbody rb;

    private enum State { Wandering, Chasing, Attacking, Dead }
    private State currentState;

    private Vector3 wanderTarget;
    private float wanderTimer;
    private float lastAttackTime;
    private float knockbackTimer = 0f;

    void Start()
    {
        currentHealth = maxHealth;
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        
        // Setup the UI above their head
        if (nameText != null) 
        {
            nameText.text = enemyName;
        }
        if (levelText != null)
        {
            levelText.text = $"Lv.{level}";
        }
        
        UpdateHealthUI();

        // PHYSICS UPDATE: Enable Gravity and Solid Collisions!
        rb.isKinematic = false; 
        rb.useGravity = true;
        
        // Prevent the skeleton from tipping over and falling on its face
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        
        currentState = State.Wandering;
        PickNewWanderTarget();
    }

    void Update()
    {
        if (currentState == State.Dead) return;

        // Pause the AI logic briefly if we are getting knocked back
        if (knockbackTimer > 0)
        {
            knockbackTimer -= Time.deltaTime;
            return; 
        }

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
            
            Vector3 moveVelocity = direction * speed;
            moveVelocity.y = rb.linearVelocity.y; 
            rb.linearVelocity = moveVelocity;
            
            anim.SetFloat("MoveSpeed", speed);
        }
        else
        {
            rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
            anim.SetFloat("MoveSpeed", 0f);
        }
    }

    private void PickNewWanderTarget()
    {
        if (MRUK.Instance != null && MRUK.Instance.GetCurrentRoom() != null)
        {
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

        wanderTarget = transform.position + new Vector3(Random.Range(-2f, 2f), 0, Random.Range(-2f, 2f));
        wanderTimer = 3f;
    }

    private void ExecuteAttack()
    {
        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        anim.SetFloat("MoveSpeed", 0f);
        anim.SetTrigger("Attack");
        lastAttackTime = Time.time;

        StartCoroutine(DamageSphereRoutine());
    }

    private IEnumerator DamageSphereRoutine()
    {
        yield return new WaitForSeconds(0.5f); 

        Collider[] hits = Physics.OverlapSphere(transform.position + transform.forward * 1.0f, 1.0f);
        foreach (var hit in hits)
        {
            if (hit.transform.root == transform.root) continue; 

            ShieldBlocker shield = hit.GetComponent<ShieldBlocker>();
            if (shield != null)
            {
                shield.TakeDamage(attackDamage);
                yield break; 
            }

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                yield break; 
            }
        }
    }

    // Automatically triggers knockback away from the target/attacker when damage is received!
    public void TakeDamage(float amount)
    {
        if (currentState == State.Dead) return;

        currentHealth -= amount;
        UpdateHealthUI(); 
        
        anim.SetTrigger("Hit");

        // Automatically determine knockback source from the current target or face direction
        Vector3 hitSource = target != null ? target.position : transform.position - transform.forward;
        ApplyKnockback(hitSource);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public void ApplyKnockback(Vector3 hitSourcePosition)
    {
        if (currentState == State.Dead) return;

        knockbackTimer = knockbackDuration; 

        Vector3 knockbackDirection = (transform.position - hitSourcePosition).normalized;
        knockbackDirection.y = 0; 

        rb.linearVelocity = new Vector3(0, rb.linearVelocity.y, 0);
        rb.AddForce(knockbackDirection * knockbackForce, ForceMode.Impulse);
    }

    private void Die()
    {
        currentState = State.Dead;
        rb.linearVelocity = Vector3.zero;
        anim.SetFloat("MoveSpeed", 0f);
        anim.SetTrigger("Die");
        
        rb.isKinematic = true;
        GetComponent<CapsuleCollider>().enabled = false;
        
        Destroy(gameObject, 3f); 
    }

    private void UpdateHealthUI()
    {
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Clamp01(currentHealth / maxHealth);
        }
    }
}