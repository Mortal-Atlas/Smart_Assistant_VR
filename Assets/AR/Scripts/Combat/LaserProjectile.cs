using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class LaserProjectile : MonoBehaviour
{
    [Header("Laser Settings")]
    public float speed = 20f;
    public float damage = 15f;
    public float lifeTime = 3f; // Destroys itself after 3 seconds

    void Start()
    {
        // 1. Ensure the collider is a Trigger (so it doesn't physically push things)
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        // 2. Ensure the Rigidbody ignores gravity (lasers don't fall!)
        Rigidbody rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.isKinematic = true; // We will move it manually with code

        // 3. Start the self-destruct timer
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // Ignore our own player and familiar!
        if (other.CompareTag("Player") || other.transform.root.GetComponentInChildren<FamiliarController>() != null)
        {
            return;
        }

        // Look for the damage interface on the object we hit
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(damage);
            
            // Destroy the laser immediately after it hits an enemy
            Destroy(gameObject);
        }
    }
}