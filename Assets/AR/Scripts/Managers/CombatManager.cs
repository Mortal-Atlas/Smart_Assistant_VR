using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class CombatManager : MonoBehaviour
{
    [Header("RPG Stats")]
    public float bossMaxHealth = 1000f;
    private float currentBossHealth;
    
    [Tooltip("Fills from 0.0 to 1.0 based on attacks")]
    private float ultimateMeter = 0f;
    
    [Header("UI References")]
    public Image bossHealthBarFill;
    public Image ultimateMeterFill;
    public TextMeshProUGUI combatLogText;
    
    [Tooltip("The glowing UI overlay that appears when Ultimate is ready")]
    public GameObject ultimateReadyOverlay;

    private void Awake()
    {
        currentBossHealth = bossMaxHealth;
        UpdateUI();
    }

    private void OnEnable()
    {
        // Listen to the global event bus for any combat commands
        MqttQuestBridge.OnCombatInputReceived += HandleCombatInput;
    }

    private void OnDisable()
    {
        MqttQuestBridge.OnCombatInputReceived -= HandleCombatInput;
    }

    private void HandleCombatInput(string moveName)
    {
        // Ignore inputs if the Combat panel isn't actively open
        if (!gameObject.activeInHierarchy) return;

        float damage = 0f;
        float ultCharge = 0f;

        // Simple State Machine for moves
        switch (moveName)
        {
            case "Light Attack":
                damage = Random.Range(20f, 35f);
                ultCharge = 0.05f;
                LogMessage($"Light strike! Dealt {damage:F0} DMG.");
                TriggerHaptic(0.2f, 0.1f);
                break;

            case "Heavy Slash":
                damage = Random.Range(60f, 90f);
                ultCharge = 0.15f;
                LogMessage($"Heavy Slash! Dealt {damage:F0} DMG.");
                TriggerHaptic(0.5f, 0.3f);
                break;

            case "Furious Slash": // The Ultimate
                if (ultimateMeter >= 1.0f)
                {
                    damage = Random.Range(300f, 450f);
                    ultimateMeter = 0f; // Reset meter
                    LogMessage($"<color=orange>FURIOUS SLASH!</color> Dealt {damage:F0} MASSIVE DMG!");
                    TriggerHaptic(1.0f, 0.8f);
                }
                else
                {
                    LogMessage("Ultimate is not ready yet!");
                    TriggerHaptic(0.1f, 0.1f);
                    return; // Abort the attack
                }
                break;
                
            default:
                Debug.LogWarning($"[Combat] Unknown move received: {moveName}");
                break;
        }

        if (damage > 0)
        {
            ApplyDamage(damage, ultCharge);
        }
    }

    private void ApplyDamage(float dmg, float ultGain)
    {
        currentBossHealth = Mathf.Max(0, currentBossHealth - dmg);
        
        // Cap the ultimate meter at 1.0
        ultimateMeter = Mathf.Clamp01(ultimateMeter + ultGain);

        UpdateUI();

        if (currentBossHealth <= 0)
        {
            LogMessage("<color=green>VICTORY! Boss Defeated!</color>");
            // Optional: Send a command to Margo to celebrate
            MqttQuestBridge.Instance.PublishMessage(MargoTopics.VoiceListen, "I just defeated the boss, say something cool.");
        }
    }

    private void UpdateUI()
    {
        if (bossHealthBarFill != null)
            bossHealthBarFill.fillAmount = currentBossHealth / bossMaxHealth;

        if (ultimateMeterFill != null)
            ultimateMeterFill.fillAmount = ultimateMeter;

        if (ultimateReadyOverlay != null)
            ultimateReadyOverlay.SetActive(ultimateMeter >= 1.0f);
    }

    private void LogMessage(string msg)
    {
        if (combatLogText != null)
        {
            combatLogText.text = msg;
        }
        Debug.Log($"[Combat] {msg}");
    }

    private void TriggerHaptic(float frequency, float amplitude)
    {
        // Rumble the physical controller that the phone is mounted to
        OVRInput.SetControllerVibration(frequency, amplitude, OVRInput.Controller.RTouch);
    }
}