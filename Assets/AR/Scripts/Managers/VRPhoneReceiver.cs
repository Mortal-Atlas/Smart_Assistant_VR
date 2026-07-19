using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class TouchPayload
{
    public float tx, ty; // Touch X, Y (0 to 1)
    public bool isTouching;
}

public class VRPhoneReceiver : MonoBehaviour
{
    [Header("UI Interaction")]
    [Tooltip("A small sphere or UI Image that moves on the phone screen")]
    public Transform cursorVisual; 
    
    [Tooltip("Physical dimensions of the S24 Ultra screen in meters (Width, Height)")]
    public Vector2 screenPhysicalSize = new Vector2(0.162f, 0.079f); 

    private Dictionary<string, string> voiceCommands;

    // Internal state tracking
    private bool wasTouchingLastFrame = false;
    private bool wasTriggerPulled = false;
    private string currentAppMode = "Standby";

    private void Awake()
    {
        // Map spoken words to the App State strings used by AppStateManager
        voiceCommands = new Dictionary<string, string>()
        {
            { "standby", "Standby" }, { "spotify", "Spotify" }, { "music", "Spotify" },
            { "combat", "Combat" }, { "fight", "Combat" }, { "scan", "Scanner" },
            { "camera", "Scanner" }, { "chat", "AIChat" }, { "text", "AIChat" },
            { "pet", "Tomodatchi" }, { "feed", "Tomodatchi" }
        };
    }

    private void OnEnable()
    {
        MqttQuestBridge.OnTouchDataReceived += ProcessTouchData;
        MqttQuestBridge.OnVoiceInputReceived += ProcessVoiceInput;
        MqttQuestBridge.OnAppSwitched += SyncAppMode;
    }

    private void OnDisable()
    {
        MqttQuestBridge.OnTouchDataReceived -= ProcessTouchData;
        MqttQuestBridge.OnVoiceInputReceived -= ProcessVoiceInput;
        MqttQuestBridge.OnAppSwitched -= SyncAppMode;
    }

    private void Update()
    {
        // Only listen for the trigger pull if we are actually in the Scanner app.
        if (currentAppMode == "Scanner")
        {
            float triggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
            bool isTriggerPulled = triggerValue > 0.8f;

            if (isTriggerPulled && !wasTriggerPulled)
            {
                TriggerScannerPhoto();
            }

            wasTriggerPulled = isTriggerPulled;
        }
    }

    private void SyncAppMode(string appName)
    {
        currentAppMode = appName;
    }

    private void ProcessVoiceInput(string text)
    {
        string lowerText = text.ToLower().Trim().Replace(".", "").Replace("!", "").Replace("?", "");

        if (voiceCommands.ContainsKey(lowerText))
        {
            SwitchAppModeGlobal(voiceCommands[lowerText]);
        }
        else
        {
            SwitchAppModeGlobal("AIChat");
            // Rika's brain handles the response via the Bridge!
        }
    }

    private void ProcessTouchData(string json)
    {
        TouchPayload data = JsonUtility.FromJson<TouchPayload>(json);
        if (cursorVisual == null) return;

        bool isTapRelease = !data.isTouching && wasTouchingLastFrame;

        if (data.isTouching)
        {
            if (!cursorVisual.gameObject.activeSelf) cursorVisual.gameObject.SetActive(true);
            
            float mappedX = (data.tx - 0.5f) * screenPhysicalSize.x;
            float mappedY = -(data.ty - 0.5f) * screenPhysicalSize.y;
            cursorVisual.localPosition = new Vector3(mappedX, mappedY, 0.002f); 

            OVRInput.SetControllerVibration(0.1f, 0.1f, OVRInput.Controller.RTouch);
        }
        else
        {
            if (cursorVisual.gameObject.activeSelf) cursorVisual.gameObject.SetActive(false);
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
            Debug.Log("[VRPhoneReceiver] Physical Click Detected on: " + hit.gameObject.name);

            // --- Combat App ---
            if (hit.gameObject.name == "Btn_LightAttack") 
                MqttQuestBridge.Instance.PublishMessage(MargoTopics.CombatInput, "Light Attack");
            if (hit.gameObject.name == "Btn_HeavySlash") 
                MqttQuestBridge.Instance.PublishMessage(MargoTopics.CombatInput, "Heavy Slash");
            if (hit.gameObject.name == "Btn_FuriousSlash") 
                MqttQuestBridge.Instance.PublishMessage(MargoTopics.CombatInput, "Furious Slash");
            
            // --- Spotify ---
            if (hit.gameObject.name == "Btn_PlayPause") 
                MqttQuestBridge.Instance.PublishMessage(MargoTopics.SpotifyToggle, "toggle");
            
            // --- App Navigation ---
            if (hit.gameObject.name == "Btn_AppTomodatchi") SwitchAppModeGlobal("Tomodatchi");
            if (hit.gameObject.name == "Btn_AppCombat") SwitchAppModeGlobal("Combat");
            if (hit.gameObject.name == "Btn_AppScanner") SwitchAppModeGlobal("Scanner");
            if (hit.gameObject.name == "Btn_AppSpotify") SwitchAppModeGlobal("Spotify");
            if (hit.gameObject.name == "Btn_TestChat") SwitchAppModeGlobal("AIChat");

            // --- Tomodatchi Interactions ---
            if (hit.gameObject.name == "Btn_FeedApple") TomodatchiManager.Instance.FeedTomodatchi("Apples", 10);
            if (hit.gameObject.name == "Btn_FeedSushi") TomodatchiManager.Instance.FeedTomodatchi("Sushi", 25);
        }
    }

    private void SwitchAppModeGlobal(string appName)
    {
        currentAppMode = appName; 
        MqttQuestBridge.Instance.PublishMessage(MargoTopics.AppSwitch, appName, true);
    }

    public void TriggerScannerPhoto()
    {
        Debug.Log("📸 SNAP! Telling the Pi to grab a frame.");
        OVRInput.SetControllerVibration(1.0f, 0.8f, OVRInput.Controller.RTouch); 
        MqttQuestBridge.Instance.PublishMessage(MargoTopics.VisionCapture, "SNAP");
    }
}