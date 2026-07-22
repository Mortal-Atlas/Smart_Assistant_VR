using UnityEngine;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Core References")]
    public FamiliarController familiarController;

    [Header("Global Combat UI / Debug Dashboard")]
    public TMP_Text healthDebugText;
    public TMP_Text manaDebugText;
    public TMP_Text staminaDebugText;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void Update()
    {
        if (familiarController == null) return;

        float currentHealth = familiarController.currentHealth;
        float currentMana = familiarController.currentMana;
        float currentStamina = familiarController.currentStamina;

        if (healthDebugText != null) healthDebugText.text = $"HP: {Mathf.RoundToInt(currentHealth)} / {familiarController.maxHealth}";
        if (manaDebugText != null) manaDebugText.text = $"MP: {Mathf.RoundToInt(currentMana)} / {familiarController.maxMana}";
        if (staminaDebugText != null) staminaDebugText.text = $"SP: {Mathf.RoundToInt(currentStamina)} / {familiarController.maxStamina}";
    }

    public void ProcessCombatCommand(string commandString)
    {
        if (familiarController == null) return;

        // Updated to use the new combo and spin attacks!
        switch (commandString.ToUpper())
        {
            case "MELEE":
            case "HEAVY_SLASH":
                familiarController.ExecuteMeleeAttack();
                break;
                
            case "SPIN_ATTACK":
                familiarController.ExecuteSpinAttack();
                break;
        }
    }
}