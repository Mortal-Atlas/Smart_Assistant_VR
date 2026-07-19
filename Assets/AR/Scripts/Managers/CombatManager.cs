using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;

[System.Serializable]
public class CombatLogEntry
{
    public string message;
    public float timeRemaining;
}

public class CombatManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Familiar GameObject here")]
    public FamiliarController playerFamiliar;

    [Header("HUD UI Elements")]
    public Image healthBarFill;
    public Image manaBarFill;
    public Image staminaBarFill;
    
    [Tooltip("The TextMeshPro element where the fading text log appears")]
    public TextMeshProUGUI fadingCombatLogText;

    [Header("Log Settings")]
    public float messageDisplayTime = 4.0f;
    private List<CombatLogEntry> activeLogs = new List<CombatLogEntry>();

    private void OnEnable()
    {
        InventoryManager.OnItemPickedUp += AddLogMessage;
    }

    private void OnDisable()
    {
        InventoryManager.OnItemPickedUp -= AddLogMessage;
    }

    private void Update()
    {
        if (playerFamiliar != null)
        {
            // Sync UI bars to familiar stats
            if (healthBarFill != null) healthBarFill.fillAmount = playerFamiliar.currentHealth / playerFamiliar.maxHealth;
            if (manaBarFill != null) manaBarFill.fillAmount = playerFamiliar.currentMana / playerFamiliar.maxMana;
            if (staminaBarFill != null) staminaBarFill.fillAmount = playerFamiliar.currentStamina / playerFamiliar.maxStamina;
        }

        UpdateFadingLog();
    }

    public void AddLogMessage(string msg)
    {
        activeLogs.Add(new CombatLogEntry { message = msg, timeRemaining = messageDisplayTime });
    }

    private void UpdateFadingLog()
    {
        if (fadingCombatLogText == null) return;

        string builtLog = "";
        
        // Loop backwards so we can safely remove expired items
        for (int i = activeLogs.Count - 1; i >= 0; i--)
        {
            activeLogs[i].timeRemaining -= Time.deltaTime;

            if (activeLogs[i].timeRemaining <= 0)
            {
                activeLogs.RemoveAt(i);
            }
            else
            {
                // Calculate alpha (opacity) based on remaining time. 
                // Starts fading out during the last 1.5 seconds.
                float alpha = Mathf.Clamp01(activeLogs[i].timeRemaining / 1.5f);
                int hexAlpha = (int)(alpha * 255);
                
                // Unity Rich Text Alpha tag: <alpha=#FF>
                builtLog = $"<alpha=#{hexAlpha:X2}>{activeLogs[i].message}\n" + builtLog;
            }
        }

        fadingCombatLogText.text = builtLog;
    }
}