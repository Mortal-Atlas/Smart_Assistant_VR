using UnityEngine;
using UnityEngine.UI;

public class FamiliarCombatUI : MonoBehaviour
{
    [Header("UI Image References")]
    [Tooltip("Drag the Filled Images here (Health and Mana on Phone Canvas, Stamina on Familiar Canvas)")]
    public Image healthBarFill;
    public Image manaBarFill;
    public Image staminaRadialFill;

    [Header("Animation Settings")]
    [Tooltip("How fast the bars shrink/grow to their new values")]
    public float fillSpeed = 5f;

    // Internal target percentages (0.0 to 1.0)
    private float targetHealthPct = 1f;
    private float targetManaPct = 1f;
    private float targetStaminaPct = 1f;

    void Update()
    {
        // Smoothly interpolate the visual fill amounts towards the target percentage
        if (healthBarFill != null)
        {
            healthBarFill.fillAmount = Mathf.Lerp(healthBarFill.fillAmount, targetHealthPct, Time.deltaTime * fillSpeed);
        }
            
        if (manaBarFill != null)
        {
            manaBarFill.fillAmount = Mathf.Lerp(manaBarFill.fillAmount, targetManaPct, Time.deltaTime * fillSpeed);
        }
            
        if (staminaRadialFill != null)
        {
            staminaRadialFill.fillAmount = Mathf.Lerp(staminaRadialFill.fillAmount, targetStaminaPct, Time.deltaTime * fillSpeed);
        }
    }

    // --- RECEIVER METHODS ---
    // These are called by the FamiliarController to set the target fill amounts.

    public void UpdateHealthBar(float current, float max) 
    { 
        targetHealthPct = Mathf.Clamp01(current / max); 
    }

    public void UpdateManaBar(float current, float max) 
    { 
        targetManaPct = Mathf.Clamp01(current / max); 
    }

    public void UpdateStaminaBar(float current, float max) 
    { 
        targetStaminaPct = Mathf.Clamp01(current / max); 
    }
}