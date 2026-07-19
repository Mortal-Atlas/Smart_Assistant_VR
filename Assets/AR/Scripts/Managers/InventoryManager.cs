using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RPGState
{
    public int level = 1;
    public int currentExp = 0;
    
    // Currency / Materials
    public int apples = 5;
    public int sushi = 2;
    public int monsterDust = 0;
    
    // Equipment tracking
    public string equippedWeapon = "Wooden Sword";
    public string equippedArmor = "Tattered Cloak";
    public List<string> unlockedGear = new List<string>();
}

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }
    public static event System.Action<string> OnItemPickedUp;

    [Header("Save Data")]
    public RPGState saveState = new RPGState();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void AddExp(int amount)
    {
        saveState.currentExp += amount;
        OnItemPickedUp?.Invoke($"<color=yellow>+{amount} EXP</color>");
        
        // Simple level up logic
        int expNeeded = saveState.level * 100;
        if (saveState.currentExp >= expNeeded)
        {
            saveState.level++;
            saveState.currentExp -= expNeeded;
            OnItemPickedUp?.Invoke($"<color=cyan>LEVEL UP! You are now Lv {saveState.level}!</color>");
        }
    }

    public void AddLoot(string itemName, int amount)
    {
        if (itemName == "Monster Dust") saveState.monsterDust += amount;
        if (itemName == "Apple") saveState.apples += amount;

        OnItemPickedUp?.Invoke($"Found {amount}x {itemName}");
    }
}