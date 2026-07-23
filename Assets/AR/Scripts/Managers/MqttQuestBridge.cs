using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.InputSystem;
using UnityEngine.Events; // Required for UnityEvent


[System.Serializable]
public class RikaCommand
{
    public string command;
}

public class MqttQuestBridge : M2MqttUnityClient
{
    public static MqttQuestBridge Instance { get; private set; }
    public static event System.Action<string> OnSpotifyStateUpdated;
    public static event System.Action<string> OnTouchDataReceived;
    public static event System.Action<string> OnVoiceInputReceived;
    public static event System.Action<string> OnPetStateReceived;
    public static event System.Action<string> OnAppSwitched;
    public static event System.Action<Vector2> OnFishingCastReceived;
    public static event System.Action<string> OnCombatInputReceived;
    public static event System.Action<string> OnPetActionReceived;

    [Header("Margo Integration")]
    [Tooltip("Drag the GameObject with the RikaAgent script here")]
    public GameObject rikaAgent; // Using GameObject to avoid missing script errors
    
    [Tooltip("Drag the GameObject with the ChatController script here")]
    public GameObject chatController;

    [Tooltip("Drag the GameObject with the AppStateManager script here")]
    public GameObject appStateManager;

    [Tooltip("Drag the GameObject with the GeminiManager script here")]
    public GameObject geminiManager;

    [Header("TTS Output")]
    [Tooltip("Drag your Meta TTS Building Block here and select Speak(string) to make Margo talk.")]
    public UnityEvent<string> OnAIResponseReceived; 

    [Header("Controls")]
    [Tooltip("Input Action for Right Thumbstick Click (Press)")]
    public InputActionReference rightThumbstickClick;
    [Tooltip("Input Action for Right Thumbstick Axis (Vector2)")]
    public InputActionReference rightThumbstickAxis;
    
    [Tooltip("How far the thumbstick must be pushed upon release to trigger an app switch (0.0 to 1.0)")]
    public float thumbstickDeadzone = 0.5f;

    private bool wasPressingThumbstick = false;
    private bool wasHeadsetOnFace = true;

    protected override void Awake()
    {
        if (Instance != null && Instance != this) 
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        base.Awake();
    }

    private void OnEnable()
    {
        if (rightThumbstickClick != null && rightThumbstickClick.action != null)
            rightThumbstickClick.action.Enable();
            
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null)
            rightThumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        if (rightThumbstickClick != null && rightThumbstickClick.action != null)
            rightThumbstickClick.action.Disable();
            
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null)
            rightThumbstickAxis.action.Disable();
    }

    protected override void Start()
    {
        base.Start();
    }

    protected override void Update()
    {
        base.Update(); 
        HandleThumbstickInput();
        MonitorHeadsetProximity();
    }

    private void MonitorHeadsetProximity()
    {
        bool isHeadsetOnFace = OVRPlugin.userPresent;

        if (isHeadsetOnFace && !wasHeadsetOnFace)
        {
            Debug.Log("[VR Status] Headset mounted. Telling phone to enter Trackpad Mode.");
            PublishMessage(MargoTopics.VRStatus, "online", false); 
        }
        else if (!isHeadsetOnFace && wasHeadsetOnFace)
        {
            Debug.Log("[VR Status] Headset removed. Telling phone to show Buttons.");
            PublishMessage(MargoTopics.VRStatus, "offline", false); 
        }

        wasHeadsetOnFace = isHeadsetOnFace;
    }

    private void HandleThumbstickInput()
    {
        if (rightThumbstickClick == null || rightThumbstickAxis == null) return;
        if (rightThumbstickClick.action == null || rightThumbstickAxis.action == null) return;

        bool isPressed = rightThumbstickClick.action.ReadValue<float>() > 0.5f;
        Vector2 currentAxis = rightThumbstickAxis.action.ReadValue<Vector2>();

        if (isPressed && !wasPressingThumbstick)
        {
            ActivateRika();
        }
        else if (!isPressed && wasPressingThumbstick)
        {
            if (currentAxis.magnitude > thumbstickDeadzone)
            {
                HandleDirectionalRelease(currentAxis);
            }
        }

        wasPressingThumbstick = isPressed;
    }

    private void ActivateRika()
    {
        Debug.Log("Thumbstick Pressed: Activating Margo!");
        if (rikaAgent != null)
        {
            rikaAgent.SendMessage("Materialize", SendMessageOptions.DontRequireReceiver);
        }
        PublishMessage(MargoTopics.VoiceListen, "start");
    }

    private void HandleDirectionalRelease(Vector2 axis)
    {
        string appState = "Standby";
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;

        if (angle > -22.5f && angle <= 22.5f) appState = "Spotify"; 
        else if (angle > 22.5f && angle <= 67.5f) appState = "App_ForwardRight"; 
        else if (angle > 67.5f && angle <= 112.5f) appState = "Combat"; 
        else if (angle > 112.5f && angle <= 157.5f) appState = "App_ForwardLeft"; 
        else if (angle > 157.5f || angle <= -157.5f) appState = "Scanner"; 
        else if (angle > -157.5f && angle <= -112.5f) appState = "App_BackLeft"; 
        else if (angle > -112.5f && angle <= -67.5f) appState = "Tomodatchi"; 
        else if (angle > -67.5f && angle <= -22.5f) appState = "App_BackRight"; 

        Debug.Log($"Thumbstick Released at angle {angle:F1}: Changing App State to {appState}");
        PublishMessage(MargoTopics.AppSwitch, appState, true);
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        client.Subscribe(new string[] { MargoTopics.Commands, MargoTopics.AIResponse, MargoTopics.VoiceInput, MargoTopics.AppSwitch, MargoTopics.VisionResult, MargoTopics.SpotifyState, MargoTopics.PhoneTouch, MargoTopics.PetState, MargoTopics.FishingCast, MargoTopics.CombatInput, MargoTopics.PetAction, "vr/status/ping" }, 
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        
        PublishMessage(MargoTopics.VRStatus, "online", false);
        Debug.Log("Connected to the Margo Message Bus! Waiting for commands...");
    }

    protected override void OnApplicationQuit()
    {
        PublishMessage(MargoTopics.VRStatus, "offline", false);
        base.OnApplicationQuit();
    }

    public void PublishMessage(string topic, string message, bool retain = false)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, retain);
        }
        else
        {
            Debug.LogWarning($"[MQTT] Cannot publish to {topic}. Broker is disconnected.");
        }
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string payload = System.Text.Encoding.UTF8.GetString(message);

        // --- CORE AI TEXT ---
        if (topic == MargoTopics.AIResponse)
        {
            Debug.Log($"[MQTT] Received AI Response: {payload}");

            // 1. Send it to the UI panel
            if (chatController != null)
            {
                chatController.SendMessage("OnMqttMessageReceived", payload, SendMessageOptions.DontRequireReceiver);
            }

            // 2. Invoke the UnityEvent for the Meta TTS Block on the Main Thread
            UnityMainThreadDispatcher.Instance().Enqueue(() =>
            {
                OnAIResponseReceived?.Invoke(payload);
            });
        }
        
        // --- MODULE ROUTING ---
        else if (topic == MargoTopics.SpotifyState)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnSpotifyStateUpdated?.Invoke(payload); });
        }
        else if (topic == MargoTopics.PhoneTouch)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnTouchDataReceived?.Invoke(payload); });
        }
        else if (topic == MargoTopics.VoiceInput)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnVoiceInputReceived?.Invoke(payload); });
        }
        else if (topic == MargoTopics.PetState)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnPetStateReceived?.Invoke(payload); });
        }
        else if (topic == MargoTopics.AppSwitch)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnAppSwitched?.Invoke(payload); });
        }
        else if (topic == MargoTopics.CombatInput)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnCombatInputReceived?.Invoke(payload); });
        }
        else if (topic == MargoTopics.PetAction)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => { OnPetActionReceived?.Invoke(payload); });
        }
    }
}