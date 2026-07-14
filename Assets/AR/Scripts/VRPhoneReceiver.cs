using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using TMPro; // Required for TextMeshPro UI
using System.Collections; // Required for Coroutines

// --- WE NEEDED TO ADD THIS HERE FOR THE VR PROJECT! ---
[System.Serializable]
public class TouchPayload
{
    public float tx, ty; // Touch X, Y (0 to 1)
    public bool isTouching;
}
// ------------------------------------------------------

public enum PhoneAppMode
{
    Standby,
    PhoneOnly, // When you just want the physical phone, VR UI stays hidden
    Spotify,
    Combat,
    Scanner,
    AIChat     // Our new texting interface!
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

    [Header("AI Chat Settings")]
    [Tooltip("The text box that shows the chat history log")]
    public TextMeshProUGUI chatHistoryText;
    [Tooltip("The text box where Rika's current message types out")]
    public TextMeshProUGUI currentMessageText;
    [Tooltip("How fast the letters appear (in seconds)")]
    public float typingSpeed = 0.03f;

    // State tracking
    private bool wasTouchingLastFrame = false;
    private string fullChatHistory = "";
    private Coroutine typingCoroutine;

    protected override void OnConnected()
    {
        client.Subscribe(new string[] { 
            "rika/phone/touch",
            "rika/phone/command" 
        }, new byte[] { 
            MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE,
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

            if (hit.gameObject.name == "Btn_FuriousSlash") ExecuteCombatMove("Furious Slash");
            if (hit.gameObject.name == "Btn_PlayPause") ToggleSpotify();
            if (hit.gameObject.name == "Btn_TestChat") TriggerTestChat(); // Test button for the typewriter!
        }
    }

    // --- APP ROUTING & COMMANDS ---

    public void SwitchAppMode(PhoneAppMode newMode)
    {
        currentMode = newMode;
        
        if (spotifyPanel != null) spotifyPanel.SetActive(false);
        if (combatPanel != null) combatPanel.SetActive(false);
        if (scannerPanel != null) scannerPanel.SetActive(false);
        if (aiChatPanel != null) aiChatPanel.SetActive(false);
        if (cursorVisual != null) cursorVisual.gameObject.SetActive(true);

        if (newMode == PhoneAppMode.PhoneOnly)
        {
            if (cursorVisual != null) cursorVisual.gameObject.SetActive(false);
            return; // Everything stays off
        }

        if (newMode == PhoneAppMode.Spotify && spotifyPanel != null) spotifyPanel.SetActive(true);
        if (newMode == PhoneAppMode.Combat && combatPanel != null) combatPanel.SetActive(true);
        if (newMode == PhoneAppMode.Scanner && scannerPanel != null) scannerPanel.SetActive(true);
        if (newMode == PhoneAppMode.AIChat && aiChatPanel != null) aiChatPanel.SetActive(true);

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
            // Gemini sent a message back! Switch to chat mode and start typing.
            string reply = command.Replace("AI_SAYS:", "");
            if (currentMode != PhoneAppMode.AIChat) SwitchAppMode(PhoneAppMode.AIChat);
            ReceiveAIMessage(reply);
        }
    }

    // --- AI CHAT LOGIC ---

    public void ReceiveAIMessage(string incomingMessage)
    {
        // If a message was already typing, force it to finish and push it to history immediately
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
            // Push the last completed message into the history log before starting the new one
            fullChatHistory += "\n<color=#ff00ff>Rika:</color> " + currentMessageText.text + "\n";
        }

        if (chatHistoryText != null) chatHistoryText.text = fullChatHistory;
        
        // Start the typewriter effect for the new message
        typingCoroutine = StartCoroutine(TypewriterEffect(incomingMessage));
    }

    private IEnumerator TypewriterEffect(string message)
    {
        if (currentMessageText == null) yield break;

        currentMessageText.text = "";
        
        // Type out the message one character at a time
        foreach (char c in message)
        {
            currentMessageText.text += c;
            
            // Add a tiny haptic tick for every letter printed, like a digital keyboard!
            OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);
            
            yield return new WaitForSeconds(typingSpeed);
            
            // Turn off the haptic tick immediately
            OVRInput.SetControllerVibration(0, 0, OVRInput.Controller.RTouch);
        }

        typingCoroutine = null;
    }

    private void TriggerTestChat()
    {
        SwitchAppMode(PhoneAppMode.AIChat);
        ReceiveAIMessage("Ugh, fine. I live in LA, I'm over emails, but I guess I can text you. What do you want?");
    }

    // --- SPECIFIC APP ACTIONS ---

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