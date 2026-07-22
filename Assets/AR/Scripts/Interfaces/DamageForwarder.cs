using UnityEngine;

// This script goes on your Body Hitbox.
// It catches hits and routes them to the main FamiliarController.
public class DamageForwarder : MonoBehaviour, IDamageable
{
    [Tooltip("Drag the parent object holding the FamiliarController here")]
    public FamiliarController mainController;

    public void TakeDamage(float amount)
    {
        if (mainController != null)
        {
            mainController.TakeDamage(amount);
        }
    }
}