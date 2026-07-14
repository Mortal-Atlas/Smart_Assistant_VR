using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using TMPro; // Required for TextMeshPro UI
using System.Collections; // Required for Coroutines
using System.Collections.Generic; // Required for Dictionaries

[System.Serializable]
public class TouchPayload
{
    public float tx, ty; // Touch X, Y (0 to 1)
    public bool isTouching;
}

// --- NEW CLOUD SAVE DATA STRUCTURE ---
[System.Serializable]
public class TomodatchiState
{
    public int hunger = 50;
    public int apples = 5;
    public int sushi = 2;
    public int candy = 10;
}
// -------------------------------------

public enum PhoneAppMode
{
    Standby,
    PhoneOnly, // When you just want the physical phone, VR UI stays hidden
    Spotify,
    Combat,
    Scanner,
    AIChat,    // Our texting interface
    Tomodatchi // The virtual pet app
}

public class VRPhoneReceiver : M2MqttUnityClient
{
    [Header("UI Interaction")]
    [Tooltip("A small sphere or UI Image that moves on the phone screen")]
    public Transform cursorVisual; 
    
    [Tooltip("Physical dimensions of the S24 Ultra screen in meters (Width, Height)")]
    public Vector2 screenPhysicalSize = new Vector2(0.162f, 0.079f); // FLIPPED FOR LANDSCAPE

    [Header("App States")]
    public PhoneAppMode currentMode = PhoneAppMode.Standby;

    [Header("Panels (Drag your Canvas UI Panels here)")]
    public GameObject spotifyPanel;
    public GameObject combatPanel;
    public GameObject scannerPanel;
    public GameObject aiChatPanel;
    public GameObject tomodatchiPanel; 

    [Header("Voice Commands")]
    [Tooltip("UI Image of a microphone to show when Rika is listening")]
    public GameObject micIndicator;
    private bool isListening = false;

    private System.Collections.Generic.Dictionary<string, PhoneAppMode> voiceCommands;

    [Header("Inventory & Pet Status (Cloud Saved)")]
    public TomodatchiState petState = new TomodatchiState();

    [Header("AI Chat Settings")]
    public TextMeshProUGUI chatHistoryText;
    public TextMeshProUGUI currentMessageText;
    public float typingSpeed = 0.03f;

    // Internal state tracking
    private bool wasTouchingLastFrame = false;
    private string fullChatHistory = "";
    private Coroutine typingCoroutine;

    protected override void Start()
    {
        base.Start(); // CRITICAL: M2Mqtt needs this!

        if (micIndicator != null) micIndicator.SetActive(false);

        // We initialize this for ALL platforms now, not just Windows!
        voiceCommands = new Dictionary<string, PhoneAppMode>()
        {
            { "standby", PhoneAppMode.Standby },
            { "spotify", PhoneAppMode.Spotify },
            { "music", PhoneAppMode.Spotify },
            { "combat", PhoneAppMode.Combat },
            { "fight", PhoneAppMode.Combat },
            { "scan", PhoneAppMode.Scanner },
            { "camera", PhoneAppMode.Scanner },
            { "chat", PhoneAppMode.AIChat },
            { "text", PhoneAppMode.AIChat },
            { "pet", PhoneAppMode.Tomodatchi },
            { "feed", PhoneAppMode.Tomodatchi }
        };
    }

    protected override void Update()
    {
        base.Update(); // CRITICAL: This processes the MQTT message queue!

        // Detect right thumbstick click
        if (OVRInput.GetDown(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch))
        {
            ToggleListening();
        }
    }

    private void ToggleListening()
    {
        isListening = !isListening;
        if (micIndicator != null) micIndicator.SetActive(isListening);

        if (isListening)
        {
            Debug.Log("🎤 Telling HAOS/Pi to start listening...");
            client.Publish("rika/voice/listen", System.Text.Encoding.UTF8.GetBytes("start"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
        else
        {
            Debug.Log("🔇 Telling HAOS/Pi to stop listening.");
            client.Publish("rika/voice/listen", System.Text.Encoding.UTF8.GetBytes("stop"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        client.Subscribe(new string[] { 
            "rika/phone/touch",
            "rika/phone/command",
            "rika/pet/state", 
            "rika/voice/input" // Listen for HAOS sending us the transcribed text!
        }, new byte[] { 
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
            MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE,
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE 
        });
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string payloadString = System.Text.Encoding.UTF8.GetString(message);

        if (topic == "rika/phone/touch")
        {
            ProcessTouchData(payloadString);
        }
        else if (topic == "rika/phone/command")
        {
            ProcessHAOSCommand(payloadString);
        }
        else if (topic == "rika/pet/state")
        {
            petState = JsonUtility.FromJson<TomodatchiState>(payloadString);
            Debug.Log($"💾 Rika Cloud Save Loaded: Hunger {petState.hunger}, Apples {petState.apples}");
        }
        else if (topic == "rika/voice/input")
        {
            ProcessVoiceInput(payloadString);
        }
    }

    private void ProcessVoiceInput(string text)
    {
        Debug.Log("🗣️ Rika heard via MQTT: " + text);
        string lowerText = text.ToLower().Trim();

        lowerText = lowerText.Replace(".", "").Replace("!", "").Replace("?", "");

        if (voiceCommands.ContainsKey(lowerText))
        {
            SwitchAppMode(voiceCommands[lowerText]);
        }
        else
        {
            if (currentMode != PhoneAppMode.AIChat) SwitchAppMode(PhoneAppMode.AIChat);
            
            fullChatHistory += "\n<color=#00ffff>You:</color> " + text + "\n";
            if (chatHistoryText != null) chatHistoryText.text = fullChatHistory;
            if (currentMessageText != null) currentMessageText.text = "...";

            // Fire it off to Gemini!
            client.Publish("rika/chat/input", System.Text.Encoding.UTF8.GetBytes(text), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }

        OVRInput.SetControllerVibration(0.5f, 0.3f, OVRInput.Controller.RTouch);
        
        // Auto-close the mic once we receive a result
        if (isListening) ToggleListening(); 
    }

    private void ProcessTouchData(string json)
    {
        TouchPayload data = JsonUtility.FromJson<TouchPayload>(json);

        if (cursorVisual == null || currentMode == PhoneAppMode.PhoneOnly) return;

        bool isTapDown = data.isTouching && !wasTouchingLastFrame;
        bool isTapRelease = !data.isTouching && wasTouchingLastFrame;

        if (data.isTouching)
        {
            float mappedX = (data.tx - 0.5f) * screenPhysicalSize.x;
            float mappedY = -(data.ty - 0.5f) * screenPhysicalSize.y;
            cursorVisual.localPosition = new Vector3(mappedX, mappedY, 0.002f); 

            OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);
        }

        if (isTapRelease)
        {
            OVRInput.SetControllerVibration(1.0f, 0.5f, OVRInput.Controller.RTouch);
            AttemptPhysicalClick();
        }

        wasTouchingLastFrame = data.isTouching;
    }

    private void AttemptPhysicalClick()
    {
        Collider[] hitColliders = Physics.OverlapSphere(cursorVisual.position, 0.01f);
        foreach (var hit in hitColliders)
        {
            Debug.Log("🦇 Rika clicked: " + hit.gameObject.name);

            // General Apps
            if (hit.gameObject.name == "Btn_FuriousSlash") ExecuteCombatMove("Furious Slash");
            if (hit.gameObject.name == "Btn_PlayPause") ToggleSpotify();
            if (hit.gameObject.name == "Btn_TestChat") TriggerTestChat();
            
            // App Navigation
            if (hit.gameObject.name == "Btn_AppTomodatchi") SwitchAppMode(PhoneAppMode.Tomodatchi);
            if (hit.gameObject.name == "Btn_AppCombat") SwitchAppMode(PhoneAppMode.Combat);
            if (hit.gameObject.name == "Btn_AppScanner") SwitchAppMode(PhoneAppMode.Scanner);
            if (hit.gameObject.name == "Btn_AppSpotify") SwitchAppMode(PhoneAppMode.Spotify);

            // Tomodatchi Interactions
            if (hit.gameObject.name == "Btn_FeedApple") FeedTomodatchi("Apples", 10);
            if (hit.gameObject.name == "Btn_FeedSushi") FeedTomodatchi("Sushi", 25);
        }
    }

    public void SwitchAppMode(PhoneAppMode newMode)
    {
        currentMode = newMode;
        
        if (spotifyPanel != null) spotifyPanel.SetActive(false);
        if (combatPanel != null) combatPanel.SetActive(false);
        if (scannerPanel != null) scannerPanel.SetActive(false);
        if (aiChatPanel != null) aiChatPanel.SetActive(false);
        if (tomodatchiPanel != null) tomodatchiPanel.SetActive(false);
        
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(true);

        if (newMode == PhoneAppMode.PhoneOnly)
        {
            if (cursorVisual != null) cursorVisual.gameObject.SetActive(false);
            return; 
        }

        if (newMode == PhoneAppMode.Spotify && spotifyPanel != null) spotifyPanel.SetActive(true);
        if (newMode == PhoneAppMode.Combat && combatPanel != null) combatPanel.SetActive(true);
        if (newMode == PhoneAppMode.Scanner && scannerPanel != null) scannerPanel.SetActive(true);
        if (newMode == PhoneAppMode.AIChat && aiChatPanel != null) aiChatPanel.SetActive(true);
        if (newMode == PhoneAppMode.Tomodatchi && tomodatchiPanel != null) tomodatchiPanel.SetActive(true);

        client.Publish("rika/phone/status", System.Text.Encoding.UTF8.GetBytes($"Mode changed to {newMode}"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    private void ProcessHAOSCommand(string command)
    {
        Debug.Log("🦇 Received HAOS Command: " + command);

        if (command.StartsWith("SCAN_RESULT:"))
        {
            string geminiFact = command.Replace("SCAN_RESULT:", "");
            // TODO: Update Scanner UI
        }
        else if (command.StartsWith("AI_SAYS:"))
        {
            string reply = command.Replace("AI_SAYS:", "");
            if (currentMode != PhoneAppMode.AIChat) SwitchAppMode(PhoneAppMode.AIChat);
            ReceiveAIMessage(reply);
        }
    }

    public void ReceiveAIMessage(string incomingMessage)
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            if (!string.IsNullOrEmpty(currentMessageText.text))
            {
                fullChatHistory += "\n<color=#ff00ff>Rika:</color> " + currentMessageText.text + "\n";
            }
        }
        else if (!string.IsNullOrEmpty(currentMessageText.text))
        {
            fullChatHistory += "\n<color=#ff00ff>Rika:</color> " + currentMessageText.text + "\n";
        }

        if (chatHistoryText != null) chatHistoryText.text = fullChatHistory;
        
        typingCoroutine = StartCoroutine(TypewriterEffect(incomingMessage));
    }

    private IEnumerator TypewriterEffect(string message)
    {
        if (currentMessageText == null) yield break;

        currentMessageText.text = "";
        
        foreach (char c in message)
        {
            currentMessageText.text += c;
            
            OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);
            yield return new WaitForSeconds(typingSpeed);
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        }

        typingCoroutine = null;
    }

    private void FeedTomodatchi(string foodName, int nutritionValue)
    {
        if (currentMode != PhoneAppMode.Tomodatchi) return;

        bool hasFood = false;
        int remainingAmount = 0;

        // Check our new cloud-syncable state
        if (foodName == "Apples" && petState.apples > 0) { petState.apples--; hasFood = true; remainingAmount = petState.apples; }
        else if (foodName == "Sushi" && petState.sushi > 0) { petState.sushi--; hasFood = true; remainingAmount = petState.sushi; }
        else if (foodName == "Candy" && petState.candy > 0) { petState.candy--; hasFood = true; remainingAmount = petState.candy; }

        if (hasFood)
        {
            petState.hunger = Mathf.Max(0, petState.hunger - nutritionValue); 
            
            Debug.Log($"🍎 Fed {foodName}! Inventory left: {remainingAmount}. Hunger is now {petState.hunger}");
            OVRInput.SetControllerVibration(0.5f, 0.2f, OVRInput.Controller.RTouch);
            
            // SAVE TO HAOS IMMEDIATELY!
            SyncPetStateToHAOS();
        }
        else
        {
            Debug.LogWarning($"❌ Tried to feed {foodName}, but inventory is empty!");
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

    private void TriggerTestChat()
    {
        SwitchAppMode(PhoneAppMode.AIChat);
        ReceiveAIMessage("Ugh, fine. I live in LA, I'm over emails, but I guess I can text you. What do you want?");
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
        if (currentMode != PhoneAppMode.Scanner) return;
        client.Publish("rika/phone/camera", System.Text.Encoding.UTF8.GetBytes("SNAP"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }
}