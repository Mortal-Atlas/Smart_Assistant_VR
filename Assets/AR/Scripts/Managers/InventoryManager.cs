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
    public int candy = 10;
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
    
    [Header("References")]
    public FamiliarController playerFamiliar;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        MqttQuestBridge.OnPetActionReceived += ExecuteQuickAction;
    }

    private void OnDisable()
    {
        MqttQuestBridge.OnPetActionReceived -= ExecuteQuickAction;
    }

    private void ExecuteQuickAction(string action)
    {
        // Try to automatically find the familiar in the scene if it isn't assigned
        if (playerFamiliar == null) playerFamiliar = FindFirstObjectByType<FamiliarController>();
        if (playerFamiliar == null) return;

        if (action == "quick_feed")
        {
            bool ateSomething = false;
            
            // Loop eating food until full, prioritizing the weakest food first
            while (playerFamiliar.currentHealth < playerFamiliar.maxHealth && (saveState.apples > 0 || saveState.sushi > 0))
            {
                if (saveState.apples > 0) { saveState.apples--; playerFamiliar.Heal(10f); ateSomething = true; }
                else if (saveState.sushi > 0) { saveState.sushi--; playerFamiliar.Heal(25f); ateSomething = true; }
            }

            if (ateSomething) OnItemPickedUp?.Invoke("<color=green>Familiar fully fed!</color>");
            else OnItemPickedUp?.Invoke("<color=red>No food left!</color>");
        }
        else if (action == "restore_energy")
        {
            if (saveState.candy > 0)
            {
                saveState.candy--;
                playerFamiliar.RestoreStamina(50f);
                OnItemPickedUp?.Invoke("<color=yellow>Ate Candy! Energy restored.</color>");
            }
            else OnItemPickedUp?.Invoke("<color=red>Out of Candy!</color>");
        }
        else if (action == "quick_equip")
        {
            // Simple hardcoded tier list (higher index = better stats)
            List<string> weaponTiers = new List<string> { "Wooden Sword", "Iron Sword", "Steel Longsword", "Crystal Blade" };
            List<string> armorTiers = new List<string> { "Tattered Cloak", "Leather Armor", "Iron Chestplate", "Dragon Scale" };

            string bestWeapon = saveState.equippedWeapon;
            string bestArmor = saveState.equippedArmor;
            
            int highestWeaponTier = weaponTiers.IndexOf(bestWeapon);
            int highestArmorTier = armorTiers.IndexOf(bestArmor);

            foreach (string gear in saveState.unlockedGear)
            {
                int wTier = weaponTiers.IndexOf(gear);
                if (wTier > highestWeaponTier) { highestWeaponTier = wTier; bestWeapon = gear; }

                int aTier = armorTiers.IndexOf(gear);
                if (aTier > highestArmorTier) { highestArmorTier = aTier; bestArmor = gear; }
            }

            if (bestWeapon != saveState.equippedWeapon || bestArmor != saveState.equippedArmor)
            {
                saveState.equippedWeapon = bestWeapon;
                saveState.equippedArmor = bestArmor;
                OnItemPickedUp?.Invoke($"<color=cyan>Equipped Best: {bestWeapon} & {bestArmor}</color>");
            }
            else
            {
                OnItemPickedUp?.Invoke("Already wearing the best gear.");
            }
        }
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

    public void AddLogMessage(string msg)
    {
        // Pass-through to trigger the fading UI combat log
        OnItemPickedUp?.Invoke(msg);
    }
}