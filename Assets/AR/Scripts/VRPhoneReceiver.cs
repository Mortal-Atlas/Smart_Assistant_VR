using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Collections.Generic;

[System.Serializable]
public class TouchPayload
{
    public float tx, ty; // Touch X, Y (0 to 1)
    public bool isTouching;
}

// --- CLOUD SAVE DATA STRUCTURE ---
[System.Serializable]
public class TomodatchiState
{
    public int hunger = 50;
    public int apples = 5;
    public int sushi = 2;
    public int candy = 10;
}
// ---------------------------------

public class VRPhoneReceiver : M2MqttUnityClient
{
    [Header("UI Interaction")]
    [Tooltip("A small sphere or UI Image that moves on the phone screen")]
    public Transform cursorVisual; 
    
    [Tooltip("Physical dimensions of the S24 Ultra screen in meters (Width, Height)")]
    public Vector2 screenPhysicalSize = new Vector2(0.162f, 0.079f); // FLIPPED FOR LANDSCAPE

    private Dictionary<string, string> voiceCommands;

    [Header("Inventory & Pet Status (Cloud Saved)")]
    public TomodatchiState petState = new TomodatchiState();

    // Internal state tracking
    private bool wasTouchingLastFrame = false;
    private bool wasTriggerPulled = false;
    private string currentAppMode = "Standby";

    protected override void Start()
    {
        base.Start(); // CRITICAL: M2Mqtt needs this!

        // Map spoken words to the App State strings used by AppStateManager
        voiceCommands = new Dictionary<string, string>()
        {
            { "standby", "Standby" },
            { "spotify", "Spotify" },
            { "music", "Spotify" },
            { "combat", "Combat" },
            { "fight", "Combat" },
            { "scan", "Scanner" },
            { "camera", "Scanner" },
            { "chat", "AIChat" },
            { "text", "AIChat" },
            { "pet", "Tomodatchi" },
            { "feed", "Tomodatchi" }
        };
    }

    protected override void Update()
    {
        base.Update(); // Required for M2Mqtt to process incoming messages on the main thread

        // Only listen for the trigger pull if we are actually in the Scanner app.
        // I don't want you accidentally taking photos of your floor while feeding the pet.
        if (currentAppMode == "Scanner")
        {
            // OVRInput Triggers are axes (0.0 to 1.0). Checking the float is much more reliable than GetDown.
            float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            bool isTriggerPulled = triggerValue > 0.8f;

            if (isTriggerPulled && !wasTriggerPulled)
            {
                TriggerScannerPhoto();
            }

            wasTriggerPulled = isTriggerPulled;
        }
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        client.Subscribe(new string[] { 
            "rika/phone/touch",
            "rika/phone/command",
            "rika/pet/state", 
            "rika/voice/input", // Listen for HAOS sending us the transcribed text
            "rika/app/switch"   // Listen to sync the current app mode
        }, new byte[] { 
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE
        });
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string payloadString = System.Text.Encoding.UTF8.GetString(message);

        if (topic == "rika/phone/touch")
        {
            // Moving the cursor requires the main thread!
            UnityMainThreadDispatcher.Instance().Enqueue(() => ProcessTouchData(payloadString));
        }
        else if (topic == "rika/phone/command")
        {
            ProcessHAOSCommand(payloadString);
        }
        else if (topic == "rika/pet/state")
        {
            petState = JsonUtility.FromJson<TomodatchiState>(payloadString);
            Debug.Log($"[VRPhoneReceiver] Cloud Save Loaded: Hunger {petState.hunger}, Apples {petState.apples}");
        }
        else if (topic == "rika/voice/input")
        {
            ProcessVoiceInput(payloadString);
        }
        else if (topic == "rika/app/switch")
        {
            // Sync our local state tracker whenever the radial menu or a voice command changes the app
            currentAppMode = payloadString;
            Debug.Log($"[VRPhoneReceiver] Internal state synced to: {currentAppMode}");
        }
    }

    private void ProcessVoiceInput(string text)
    {
        Debug.Log("🗣️ Voice Transcript Received: " + text);
        string lowerText = text.ToLower().Trim().Replace(".", "").Replace("!", "").Replace("?", "");

        if (voiceCommands.ContainsKey(lowerText))
        {
            // Publish globally to switch apps (MqttQuestBridge will catch this and update the UI)
            SwitchAppModeGlobal(voiceCommands[lowerText]);
        }
        else
        {
            // Switch to Chat if it's just a normal conversation prompt
            SwitchAppModeGlobal("AIChat");

            // Fire it off to the Python backend! (rika_brain.py listens on 'rika/prompt')
            client.Publish("rika/prompt", System.Text.Encoding.UTF8.GetBytes(text), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
    }

    private void ProcessTouchData(string json)
    {
        TouchPayload data = JsonUtility.FromJson<TouchPayload>(json);

        if (cursorVisual == null) return;

        bool isTapRelease = !data.isTouching && wasTouchingLastFrame;

        if (data.isTouching)
        {
            // Show cursor and map coordinates
            if (!cursorVisual.gameObject.activeSelf) cursorVisual.gameObject.SetActive(true);
            
            float mappedX = (data.tx - 0.5f) * screenPhysicalSize.x;
            float mappedY = -(data.ty - 0.5f) * screenPhysicalSize.y;
            cursorVisual.localPosition = new Vector3(mappedX, mappedY, 0.002f); 

            // Light vibration for dragging
            OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);
        }
        else
        {
            // Hide cursor when not touching
            if (cursorVisual.gameObject.activeSelf) cursorVisual.gameObject.SetActive(false);
        }

        if (isTapRelease)
        {
            // Hard vibration for physical click
            OVRInput.SetControllerVibration(1.0f, 0.5f, OVRInput.Controller.RTouch);
            AttemptPhysicalClick();
        }

        wasTouchingLastFrame = data.isTouching;
    }

    private void AttemptPhysicalClick()
    {
        // Cast a tiny sphere at the cursor's location to detect UI BoxColliders
        Collider[] hitColliders = Physics.OverlapSphere(cursorVisual.position, 0.01f);
        foreach (var hit in hitColliders)
        {
            Debug.Log("[VRPhoneReceiver] Physical Click Detected on: " + hit.gameObject.name);

            // General Apps
            if (hit.gameObject.name == "Btn_FuriousSlash") ExecuteCombatMove("Furious Slash");
            if (hit.gameObject.name == "Btn_PlayPause") ToggleSpotify();
            
            // App Navigation (Sends global MQTT command instead of handling it locally)
            if (hit.gameObject.name == "Btn_AppTomodatchi") SwitchAppModeGlobal("Tomodatchi");
            if (hit.gameObject.name == "Btn_AppCombat") SwitchAppModeGlobal("Combat");
            if (hit.gameObject.name == "Btn_AppScanner") SwitchAppModeGlobal("Scanner");
            if (hit.gameObject.name == "Btn_AppSpotify") SwitchAppModeGlobal("Spotify");
            if (hit.gameObject.name == "Btn_TestChat") SwitchAppModeGlobal("AIChat");

            // Tomodatchi Interactions
            if (hit.gameObject.name == "Btn_FeedApple") FeedTomodatchi("Apples", 10);
            if (hit.gameObject.name == "Btn_FeedSushi") FeedTomodatchi("Sushi", 25);
        }
    }

    private void SwitchAppModeGlobal(string appName)
    {
        currentAppMode = appName; // Set it locally instantly so we don't have to wait for the bounce-back
        
        if (client != null && client.IsConnected)
        {
            client.Publish("rika/app/switch", System.Text.Encoding.UTF8.GetBytes(appName), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }
    }

    private void ProcessHAOSCommand(string command)
    {
        Debug.Log("🦇 Received HAOS Command: " + command);

        if (command.StartsWith("SCAN_RESULT:"))
        {
            string geminiFact = command.Replace("SCAN_RESULT:", "");
            // TODO: Update Scanner UI with my glorious analysis of your photo
        }
    }

    private void FeedTomodatchi(string foodName, int nutritionValue)
    {
        bool hasFood = false;
        int remainingAmount = 0;

        // Check our cloud-syncable state
        if (foodName == "Apples" && petState.apples > 0) { petState.apples--; hasFood = true; remainingAmount = petState.apples; }
        else if (foodName == "Sushi" && petState.sushi > 0) { petState.sushi--; hasFood = true; remainingAmount = petState.sushi; }
        else if (foodName == "Candy" && petState.candy > 0) { petState.candy--; hasFood = true; remainingAmount = petState.candy; }

        if (hasFood)
        {
            petState.hunger = Mathf.Max(0, petState.hunger - nutritionValue); 
            
            Debug.Log($"[VRPhoneReceiver] 🍎 Fed {foodName}! Inventory left: {remainingAmount}. Hunger is now {petState.hunger}");
            OVRInput.SetControllerVibration(0.5f, 0.2f, OVRInput.Controller.RTouch);
            
            // SAVE TO HAOS IMMEDIATELY!
            SyncPetStateToHAOS();
        }
        else
        {
            Debug.LogWarning($"[VRPhoneReceiver] ❌ Tried to feed {foodName}, but inventory is empty!");
            OVRInput.SetControllerVibration(0.1f, 0.5f, OVRInput.Controller.RTouch);
        }
    }

    private void SyncPetStateToHAOS()
    {
        string saveJson = JsonUtility.ToJson(petState);
        // The 'true' at the end is the RETAIN flag! This tells the MQTT broker on your Pi 
        // to hold onto this message forever, acting as a free database.
        client.Publish("rika/pet/state", System.Text.Encoding.UTF8.GetBytes(saveJson), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
    }

    private void ExecuteCombatMove(string moveName)
    {
        client.Publish("rika/game/combat", System.Text.Encoding.UTF8.GetBytes(moveName), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    private void ToggleSpotify()
    {
        client.Publish("rika/haos/spotify/toggle", System.Text.Encoding.UTF8.GetBytes("toggle"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    public void TriggerScannerPhoto()
    {
        Debug.Log("📸 SNAP! Telling the phone to take a picture.");
        OVRInput.SetControllerVibration(1.0f, 0.8f, OVRInput.Controller.RTouch); // Big rumble for shutter click
        client.Publish("rika/phone/camera", System.Text.Encoding.UTF8.GetBytes("SNAP"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }
}