using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine.InputSystem;
using System.Text;
using System.Collections;

[System.Serializable]
public class RikaCommand
{
    public string command;
}

public class MqttQuestBridge : M2MqttUnityClient
{
    [Header("Rika Integration")]
    public RikaAgent rikaAgent; 
    public RikaChatController chatController;
    public AppStateManager appStateManager;
    public SpotifyController spotifyController;

    [Header("AI Integration")]
    public Meta.XR.BuildingBlocks.AIBlocks.TextToSpeechAgent ttsAgent;

    [Header("Controls")]
    public InputActionReference rightThumbstickClick;
    public InputActionReference rightThumbstickAxis;
    public float thumbstickDeadzone = 0.5f;

    private bool wasPressingThumbstick = false;

    // --- CONNECTION FIX: Added Delayed Connect to prevent Socket errors ---
    protected override void Start()
    {
        StartCoroutine(DelayedConnect());
    }

    private IEnumerator DelayedConnect()
    {
        yield return new WaitForSeconds(1.0f);
        base.Start(); 
    }

    private void OnEnable()
    {
        if (rightThumbstickClick != null && rightThumbstickClick.action != null) rightThumbstickClick.action.Enable();
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null) rightThumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        if (rightThumbstickClick != null && rightThumbstickClick.action != null) rightThumbstickClick.action.Disable();
        if (rightThumbstickAxis != null && rightThumbstickAxis.action != null) rightThumbstickAxis.action.Disable();
    }

    protected override void Update()
    {
        base.Update();
        HandleThumbstickInput();
    }

    private void HandleThumbstickInput()
    {
        if (rightThumbstickClick == null || rightThumbstickAxis == null) return;
        bool isPressed = rightThumbstickClick.action.ReadValue<float>() > 0.5f;
        Vector2 currentAxis = rightThumbstickAxis.action.ReadValue<Vector2>();

        if (isPressed && !wasPressingThumbstick) ActivateRika();
        else if (!isPressed && wasPressingThumbstick && currentAxis.magnitude > thumbstickDeadzone) HandleDirectionalRelease(currentAxis);

        wasPressingThumbstick = isPressed;
    }

    private void ActivateRika()
    {
        if (rikaAgent != null) rikaAgent.Materialize();
        if (client != null && client.IsConnected)
            client.Publish("rika/voice/listen", Encoding.UTF8.GetBytes("start"), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
    }

    private void HandleDirectionalRelease(Vector2 axis)
    {
        string appState = "Standby";
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        if (angle > -22.5f && angle <= 22.5f) appState = "Spotify";
        else if (angle > 67.5f && angle <= 112.5f) appState = "Combat";
        else if (angle > 157.5f || angle <= -157.5f) appState = "Scanner";
        else if (angle > -112.5f && angle <= -67.5f) appState = "Tomodatchi";

        if (client != null && client.IsConnected)
            client.Publish("rika/app/switch", Encoding.UTF8.GetBytes(appState), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
    }

    // --- INTEGRATED GEMINI MANAGER METHODS ---
    public void OnTranscriptionReceived(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript)) return;
        PublishToTopic("rika/prompt", transcript);
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        client.Subscribe(new string[] { "rika/commands", "rika/response", "rika/voice/input", "rika/app/switch", "rika/spotify/state" }, 
            new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        client.Publish("vr/status", Encoding.UTF8.GetBytes("online"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = Encoding.UTF8.GetString(message);
        UnityMainThreadDispatcher.Instance().Enqueue(() => 
        {
            if (topic == "rika/response")
            {
                if (chatController != null) chatController.OnMqttMessageReceived("\n\nRika: " + msg);
                if (ttsAgent != null) ttsAgent.SpeakText(msg.Replace("*", ""));
            }
            else if (topic == "rika/app/switch" && appStateManager != null) appStateManager.SwitchApp(msg);
            else if (topic == "rika/spotify/state" && spotifyController != null) spotifyController.UpdateState(msg);
        });
    }

    // --- HELPER METHODS ---
    public void PublishSpotifyCommand(string command)
    {
        PublishToTopic("rika/haos/spotify/toggle", command);
    }

    public void PublishToTopic(string topic, string message)
    {
        if (client != null && client.IsConnected)
        {
            client.Publish(topic, Encoding.UTF8.GetBytes(message), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
        }
    }
}