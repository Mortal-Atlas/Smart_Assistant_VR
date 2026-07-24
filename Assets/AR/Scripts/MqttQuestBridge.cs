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
    [Header("Rika Integration")]
    [Tooltip("Drag the GameObject with the RikaAgent script here")]
    public RikaAgent rikaAgent; 
    
    [Tooltip("Drag the GameObject with the RikaChatController script here")]
    public RikaChatController chatController;

    [Tooltip("Drag the GameObject with the AppStateManager script here")]
    public AppStateManager appStateManager;

    [Tooltip("Drag the GameObject with the SpotifyController script here")]
    public SpotifyController spotifyController;

    [Header("Controls")]
    [Tooltip("Input Action for Right Thumbstick Click (Press)")]
    public InputActionReference rightThumbstickClick;
    [Tooltip("Input Action for Right Thumbstick Axis (Vector2)")]
    public InputActionReference rightThumbstickAxis;
    
    [Tooltip("How far the thumbstick must be pushed upon release to trigger an app switch (0.0 to 1.0)")]
    public float thumbstickDeadzone = 0.5f;

    // State tracking for the press-and-hold radial menu gesture
    private bool wasPressingThumbstick = false;

    private void OnEnable()
    {
        // Ensure input actions are actively listening
        if (rightThumbstickClick != null && rightThumbstickClick.action != null)
            rightThumbstickClick.action.Enable();
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null)
            rightThumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        // Clean up input actions to avoid memory leaks
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

        // 1. Just pressed down: Always prompt Rika immediately
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
        Debug.Log("Thumbstick Pressed: Activating Rika!");
        if (rikaAgent != null)
        {
            rikaAgent.Materialize();
        }
        
        // Optional: Immediately tell HAOS to start listening for voice dictation
        if (client != null && client.IsConnected)
        {
            client.Publish("rika/voice/listen", System.Text.Encoding.UTF8.GetBytes("start"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
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
        
        // Publish the state change to the MQTT broker so the UI Canvas (Phone) updates
        if (client != null && client.IsConnected)
        {
            client.Publish("rika/app/switch", System.Text.Encoding.UTF8.GetBytes(appState), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        // Subscribed to "rika/voice/input" to capture what the user says for the scrollable chat log
        // Added "rika/app/switch" to synchronize UI states
        // Added "rika/spotify/state" to pull HACS SpotifyPlus metadata
        client.Subscribe(new string[] { "rika/commands", "rika/response", "rika/voice/input", "rika/app/switch", "rika/spotify/state" }, 
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        
        // Broadcast that the VR headset is online and RETAIN the message (the 'true' flag at the end)
        client.Publish("vr/status", System.Text.Encoding.UTF8.GetBytes("online"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        
        Debug.Log("Connected to the Rika Message Bus! Waiting for commands and responses...");
    }

    protected override void OnApplicationQuit()
    {
        // Be nice and tell the phone we're offline when closing the VR app
        if (client != null && client.IsConnected)
        {
            client.Publish("vr/status", System.Text.Encoding.UTF8.GetBytes("offline"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        }
        base.OnApplicationQuit();
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        Debug.Log("Raw message received on " + topic + ": " + msg);
        
        if (topic == "rika/commands")
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
                Debug.LogError("Failed to parse the JSON command. Did you format it right? Error: " + e.Message);
            }
        } 
        // --------------------------------------------------------
        // CHAT LOGIC: Capturing both sides of the conversation
        // --------------------------------------------------------
        else if (topic == "rika/voice/input")
        {
            // This is the transcript of what the USER said. Safely dispatch to main thread.
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (chatController != null)
                {
                    // Passing a formatted string so the ChatController can append it to the log
                    chatController.OnMqttMessageReceived("\n\nUser: " + msg);
                }
            });
        }
        else if (topic == "rika/response")
        {
            // This is Rika's AI reply. Safely dispatch to main thread.
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (chatController != null)
                {
                    chatController.OnMqttMessageReceived("\n\nRika: " + msg);
                }
                else
                {
                    Debug.LogWarning("RikaChatController is not assigned in the Inspector on MqttQuestBridge!");
                }
            });
        }
        // --------------------------------------------------------
        // APP STATE LOGIC: Switching the UI panels
        // --------------------------------------------------------
        else if (topic == "rika/app/switch")
        {
            // MUST push to main thread to modify GameObjects
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (appStateManager != null)
                {
                    appStateManager.SwitchApp(msg);
                }
                else
                {
                    Debug.LogWarning("AppStateManager is not assigned in the Inspector on MqttQuestBridge!");
                }
            });
        }
        // --------------------------------------------------------
        // SPOTIFY LOGIC: Syncing Track Metadata
        // --------------------------------------------------------
        else if (topic == "rika/spotify/state")
        {
            UnityMainThreadDispatcher.Instance().Enqueue(() => 
            {
                if (spotifyController != null)
                {
                    spotifyController.UpdateState(msg);
                }
            });
        }
    }

    public void PublishSpotifyCommand(string command)
    {
        if (client != null && client.IsConnected)
        {
            Debug.Log($"Sending Spotify Command: {command}");
            client.Publish("rika/haos/spotify/toggle", System.Text.Encoding.UTF8.GetBytes(command), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
    }

    // A generic publish method so other controllers (like SpotifyController) can easily publish payload data
    public void PublishToTopic(string topic, string message)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, System.Text.Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
            Debug.Log($"[MQTT] Published to {topic}: {message}");
        }
        else
        {
            Debug.LogWarning("[MQTT] Cannot publish, client is not connected.");
        }
    }
}