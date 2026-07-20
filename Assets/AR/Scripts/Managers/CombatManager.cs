using UnityEngine;
using TMPro;

public class CombatManager : MonoBehaviour
{
    public static CombatManager Instance { get; private set; }

    [Header("Core References")]
    [Tooltip("Reference to the active familiar in the scene")]
    public FamiliarController familiarController;

    [Header("Global Combat UI / Debug Dashboard")]
    [Tooltip("Optional text fields to display exact numbers on a master canvas")]
    public TMP_Text healthDebugText;
    public TMP_Text manaDebugText;
    public TMP_Text staminaDebugText;

    private void Awake()
    {
        // Set up the Singleton pattern so the MQTT Bridge can easily find this manager
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    private void Update()
    {
        // Ensure we have a familiar to track
        if (familiarController == null) return;

        // Safely read the properties using the public getters we established
        float currentHealth = familiarController.currentHealth;
        float currentMana = familiarController.currentMana;
        float currentStamina = familiarController.currentStamina;

        // If you have a master dashboard or debug UI, update the exact numbers here
        if (healthDebugText != null) 
        {
            healthDebugText.text = $"HP: {Mathf.RoundToInt(currentHealth)} / {familiarController.maxHealth}";
        }
        
        if (manaDebugText != null) 
        {
            manaDebugText.text = $"MP: {Mathf.RoundToInt(currentMana)} / {familiarController.maxMana}";
        }
        
        if (staminaDebugText != null) 
        {
            staminaDebugText.text = $"SP: {Mathf.RoundToInt(currentStamina)} / {familiarController.maxStamina}";
        }
    }

    // --- COMMAND ROUTING ---
    // The MqttQuestBridge.cs should call this method when it receives a combat command from the phone's touch zones

    public void ProcessCombatCommand(string commandString)
    {
        if (familiarController == null)
        {
            Debug.LogWarning("[CombatManager] Received combat command, but no Familiar is linked!");
            return;
        }

        // Route the specific MQTT string payload to the correct Familiar action
        switch (commandString.ToUpper())
        {
            case "HEAVY_SLASH":
                familiarController.ExecuteHeavySlash();
                break;
                
            case "FIREBALL":
                familiarController.ExecuteFireball();
                break;
                
            case "DODGE":
                // Example of a future implementation
                // familiarController.ExecuteDodge();
                break;
                
            default:
                Debug.Log($"[CombatManager] Unrecognized combat command received: {commandString}");
                break;
        }
    }
}