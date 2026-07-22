using UnityEngine;

// Goes on the Shield Hitbox. Catches damage and converts it to Stamina drain!
public class ShieldBlocker : MonoBehaviour, IDamageable
{
    [Tooltip("Drag the parent object holding the FamiliarController here")]
    public FamiliarController mainController;

    public void TakeDamage(float amount)
    {
        if (mainController != null)
        {
            // Route this hit specifically to the Block function, not the Health function
            mainController.BlockAttack(amount);
        }
    }
}