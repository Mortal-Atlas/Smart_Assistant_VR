using UnityEngine;

[System.Serializable]
public class TomodatchiState
{
    public int hunger = 50;
    public int apples = 5;
    public int sushi = 2;
    public int candy = 10;
}

public class TomodatchiManager : MonoBehaviour
{
    public static TomodatchiManager Instance { get; private set; }

    [Header("Pet Status (Cloud Saved)")]
    public TomodatchiState petState = new TomodatchiState();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        // Listen to the bridge for cloud save data upon boot
        MqttQuestBridge.OnPetStateReceived += LoadCloudState;
    }

    private void OnDisable()
    {
        MqttQuestBridge.OnPetStateReceived -= LoadCloudState;
    }

    private void LoadCloudState(string jsonState)
    {
        try
        {
            petState = JsonUtility.FromJson<TomodatchiState>(jsonState);
            Debug.Log($"[Tomodatchi] Cloud Save Loaded: Hunger {petState.hunger}, Apples {petState.apples}");
            // TODO: Update your Pet UI Text elements here!
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Tomodatchi State JSON: " + e.Message);
        }
    }

    public void FeedTomodatchi(string foodName, int nutritionValue)
    {
        bool hasFood = false;
        int remainingAmount = 0;

        if (foodName == "Apples" && petState.apples > 0) { petState.apples--; hasFood = true; remainingAmount = petState.apples; }
        else if (foodName == "Sushi" && petState.sushi > 0) { petState.sushi--; hasFood = true; remainingAmount = petState.sushi; }
        else if (foodName == "Candy" && petState.candy > 0) { petState.candy--; hasFood = true; remainingAmount = petState.candy; }

        if (hasFood)
        {
            petState.hunger = Mathf.Max(0, petState.hunger - nutritionValue); 
            
            Debug.Log($"[Tomodatchi] 🍎 Fed {foodName}! Inventory left: {remainingAmount}. Hunger is now {petState.hunger}");
            
            // Rumble the physical phone!
            OVRInput.SetControllerVibration(0.5f, 0.2f, OVRInput.Controller.RTouch);
            
            SyncPetStateToHAOS();
        }
        else
        {
            Debug.LogWarning($"[Tomodatchi] ❌ Tried to feed {foodName}, but inventory is empty!");
            OVRInput.SetControllerVibration(0.1f, 0.5f, OVRInput.Controller.RTouch);
        }
    }

    private void SyncPetStateToHAOS()
    {
        string saveJson = JsonUtility.ToJson(petState);
        // The 'true' at the end is the RETAIN flag! This tells the MQTT broker on the Pi 
        // to hold onto this message forever, acting as a free database.
        MqttQuestBridge.Instance.PublishMessage(MargoTopics.PetState, saveJson, true);
    }
}