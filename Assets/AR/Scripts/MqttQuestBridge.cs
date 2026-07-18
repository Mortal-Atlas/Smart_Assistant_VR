using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.InputSystem;

[System.Serializable]
public class RikaCommand
{
    public string command;
}

public class MqttQuestBridge : M2MqttUnityClient
{
    // --- SINGLETON & EVENTS ---
    public static MqttQuestBridge Instance { get; private set; }
    public static event System.Action<string> OnSpotifyStateUpdated;
    public static event System.Action<string> OnTouchDataReceived;
    public static event System.Action<string> OnVoiceInputReceived;
    public static event System.Action<string> OnPetStateReceived;
    public static event System.Action<string> OnAppSwitched;

    [Header("Margo Integration")]
    [Tooltip("Drag the GameObject with the RikaAgent script here")]
    public RikaAgent rikaAgent; 
    
    [Tooltip("Drag the GameObject with the RikaChatController script here")]
    public RikaChatController chatController;

    [Tooltip("Drag the GameObject with the AppStateManager script here")]
    public AppStateManager appStateManager;

    [Tooltip("Drag the GameObject with the GeminiManager script here")]
    public GeminiManager geminiManager;

    [Header("Controls")]
    [Tooltip("Input Action for Right Thumbstick Click (Press)")]
    public InputActionReference rightThumbstickClick;
    [Tooltip("Input Action for Right Thumbstick Axis (Vector2)")]
    public InputActionReference rightThumbstickAxis;
    
    [Tooltip("How far the thumbstick must be pushed upon release to trigger an app switch (0.0 to 1.0)")]
    public float thumbstickDeadzone = 0.5f;

    // State tracking for the press-and-hold radial menu gesture
    private bool wasPressingThumbstick = false;

    protected override void Awake()
    {
        // Enforce Singleton pattern so any script can access the network effortlessly
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
        // Enable input actions safely
        if (rightThumbstickClick != null && rightThumbstickClick.action != null)
            rightThumbstickClick.action.Enable();
            
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null)
            rightThumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        // Disable input actions safely to avoid memory leaks
        if (rightThumbstickClick != null && rightThumbstickClick.action != null)
            rightThumbstickClick.action.Disable();
            
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null)
            rightThumbstickAxis.action.Disable();
    }

    protected override void Start()
    {
        // Let the base class handle its own startup sequence normally.
        base.Start();
    }

    protected override void Update()
    {
        base.Update(); // Required for M2MqttUnityClient network processing on main thread
        HandleThumbstickInput();
    }

    private void HandleThumbstickInput()
    {
        if (rightThumbstickClick == null || rightThumbstickAxis == null) return;
        if (rightThumbstickClick.action == null || rightThumbstickAxis.action == null) return;

        // Read the current state of the thumbstick click and directional axis
        bool isPressed = rightThumbstickClick.action.ReadValue<float>() > 0.5f;
        Vector2 currentAxis = rightThumbstickAxis.action.ReadValue<Vector2>();

        // 1. Just pressed down: Always prompt Margo immediately
        if (isPressed && !wasPressingThumbstick)
        {
            ActivateRika();
        }
        // 2. Just released: Read the coordinate at the exact time of release
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
            rikaAgent.Materialize();
        }
        
        // Use our new clean helper!
        PublishMessage(MargoTopics.VoiceListen, "start");
    }

    private void HandleDirectionalRelease(Vector2 axis)
    {
        string appState = "Standby";

        // Calculate the angle of the thumbstick (-180 to 180 degrees)
        // 0 is Right, 90 is Forward(Up), 180/-180 is Left, -90 is Back(Down)
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;

        // 8-Way Directional mapping
        if (angle > -22.5f && angle <= 22.5f) 
            appState = "Spotify"; // Right
        else if (angle > 22.5f && angle <= 67.5f) 
            appState = "App_ForwardRight"; // Forward-Right (Placeholder)
        else if (angle > 67.5f && angle <= 112.5f) 
            appState = "Combat"; // Forward
        else if (angle > 112.5f && angle <= 157.5f) 
            appState = "App_ForwardLeft"; // Forward-Left (Placeholder)
        else if (angle > 157.5f || angle <= -157.5f) 
            appState = "Scanner"; // Left
        else if (angle > -157.5f && angle <= -112.5f) 
            appState = "App_BackLeft"; // Back-Left (Placeholder)
        else if (angle > -112.5f && angle <= -67.5f) 
            appState = "Tomodatchi"; // Back
        else if (angle > -67.5f && angle <= -22.5f) 
            appState = "App_BackRight"; // Back-Right (Placeholder)

        Debug.Log($"Thumbstick Released at angle {angle:F1}: Changing App State to {appState}");
        
        // Publish the state change to the MQTT broker
        PublishMessage(MargoTopics.AppSwitch, appState, true);
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        // Subscribed using centralized MargoTopics instead of hardcoded strings
        client.Subscribe(new string[] { MargoTopics.Commands, MargoTopics.AIResponse, MargoTopics.VoiceInput, MargoTopics.AppSwitch, MargoTopics.VisionResult, MargoTopics.SpotifyState, MargoTopics.PhoneTouch, MargoTopics.PetState }, 
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        
        // Broadcast that the VR headset is online and RETAIN the message
        PublishMessage(MargoTopics.VRStatus, "online", true);
        
        Debug.Log("Connected to the Margo Message Bus! Waiting for commands...");
    }

    protected override void OnApplicationQuit()
    {
        // Be nice and tell the phone we're offline when closing the VR app
        PublishMessage(MargoTopics.VRStatus, "offline", true);
        base.OnApplicationQuit();
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log("Raw message received on " + topic + ": " + msg);
        
        if (topic == MargoTopics.Commands)
        {
            try 
            {
                RikaCommand data = JsonUtility.FromJson<RikaCommand>(msg);
                
                if (data != null && data.command == "materialize") 
                {
                    // MUST push to main thread to modify Unity GameObjects and Transforms!
                    UnityMainThreadDispatcher.Instance().Enqueue(() => 
                    {
                        if (rikaAgent != null) 
                        {
                            Debug.Log("Materializing Rika!");
                            rikaAgent.Materialize();
                        } 
                    });
                }
                else if (data != null && data.command == "poof") 
                {
                    // MUST push to main thread!
                    UnityMainThreadDispatcher.Instance().Enqueue(() => 
                    {
                        if (rikaAgent != null) 
                        {
                            Debug.Log("Poofing Rika away!");
                            rikaAgent.PoofAway();
                        }
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse the JSON command. Error: " + e.Message);
            }
        } 
        else if (topic == MargoTopics.VoiceInput)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (chatController != null) chatController.OnMqttMessageReceived("\n\nUser: " + msg);
                OnVoiceInputReceived?.Invoke(msg);
            });
        }
        else if (topic == MargoTopics.AIResponse)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (chatController != null) chatController.OnMqttMessageReceived("\n\nMargo: " + msg);
            });
        }
        else if (topic == MargoTopics.AppSwitch)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (appStateManager != null) appStateManager.SwitchApp(msg);
                OnAppSwitched?.Invoke(msg);
            });
        }
        else if (topic == MargoTopics.VisionResult)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (geminiManager != null) geminiManager.UpdateScanResult(msg);
            });
        }
        else if (topic == MargoTopics.SpotifyState)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                // We broadcast this to ANY script listening, fully decoupling the UI!
                OnSpotifyStateUpdated?.Invoke(msg);
            });
        }
        else if (topic == MargoTopics.PhoneTouch)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                OnTouchDataReceived?.Invoke(msg);
            });
        }
        else if (topic == MargoTopics.PetState)
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                OnPetStateReceived?.Invoke(msg);
            });
        }
    }

    // --- CLEAN PUBLISH HELPER ---
    public void PublishMessage(string topic, string payload, bool retain = false)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, retain);
        }
        else
        {
            Debug.LogWarning($"[MQTT] Cannot publish to {topic}. Broker is disconnected.");
        }
    }
}