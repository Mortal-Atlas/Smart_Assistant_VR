using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Collider))]
public class WeaponHitbox : MonoBehaviour
{
    [Header("Damage Settings")]
    public float weaponDamage = 25f;
    
    private Collider hitCollider;
    private bool isActive = false;
    
    // Keeps track of who we hit so we don't hit the same skeleton 60 times in one swing
    private List<Collider> alreadyHitObjects = new List<Collider>();

    void Awake()
    {
        hitCollider = GetComponent<Collider>();
        hitCollider.isTrigger = true; 
        hitCollider.enabled = false; // Off by default so we don't do damage just standing still
    }

    // Called by the FamiliarController when the animation starts
    public void ActivateHitbox()
    {
        isActive = true;
        hitCollider.enabled = true;
        alreadyHitObjects.Clear(); // Reset the hit list for the new swing
    }

    // Called by the FamiliarController when the animation ends
    public void DeactivateHitbox()
    {
        isActive = false;
        hitCollider.enabled = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        // Don't hit ourselves!
        if (other.transform.root == transform.root) return;

        // Don't hit the same enemy twice in a single swing
        if (alreadyHitObjects.Contains(other)) return;

        // Look for the interface on the object we hit (or its parents)
        IDamageable target = other.GetComponentInParent<IDamageable>();
        if (target != null)
        {
            target.TakeDamage(weaponDamage);
            alreadyHitObjects.Add(other);
        }
    }
}