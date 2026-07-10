using UnityEngine;
using System.Collections;
using Meta.XR.BuildingBlocks.AIBlocks;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

public class GeminiManager : MonoBehaviour
{
    [Header("MQTT Settings")]
    [Tooltip("IP of your Raspberry Pi 5")]
    public string brokerIPAddress = "192.168.1.127";
    public int brokerPort = 1883;
    public string mqttUsername = "rika";
    public string mqttPassword = "12345";

    [Header("Connections")]
    public TextToSpeechAgent ttsAgent;

    private MqttClient client;
    private readonly string topicPublish = "rika/prompt";
    private readonly string topicSubscribe = "rika/response";
    
    private string pendingResponse = null;

    void Start()
    {
        // Start the connection process without freezing the main thread
        StartCoroutine(ConnectToPiRoutine());
    }

    private IEnumerator ConnectToPiRoutine()
    {
        Debug.Log("[Rika Network] Starting background connection...");
        
        // Give Unity a moment to breathe after startup
        yield return new WaitForSeconds(1.0f);

        client = new MqttClient(brokerIPAddress);
        client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        
        string clientId = "Quest_Rika_Client_" + System.Guid.NewGuid().ToString();
        
        try
        {
            client.Connect(clientId, mqttUsername, mqttPassword);
            client.Subscribe(new string[] { topicSubscribe }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
            Debug.Log("[Rika Network] Connected to the Pi 5 broker!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[Rika Network] Connection Failed: {e.Message}");
        }
    }

    // --- WIRE THIS UP IN THE INSPECTOR ---
    // Drag your GameObject with GeminiManager into the STT Agent's "OnTranscription" event
    // and select GeminiManager -> OnTranscriptionReceived(string)
    public void OnTranscriptionReceived(string transcript)
    {
        // ADD THIS LINE
        Debug.Log($"<color=green>!!! RIKA MANAGER RECEIVED EVENT: {transcript} !!!</color>");

        if (string.IsNullOrWhiteSpace(transcript)) return;
        
        if (client != null && client.IsConnected)
        {
            Debug.Log($"[Rika Network] Sending transcript: {transcript}");
            client.Publish(topicPublish, Encoding.UTF8.GetBytes(transcript), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, false);
        }
        else
        {
            Debug.LogError("[Rika Network] MQTT is not connected. Can't send message.");
        }
    }

    void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string responseText = Encoding.UTF8.GetString(e.Message);
        pendingResponse = responseText.Replace("*", "");
    }

    void Update()
    {
        if (!string.IsNullOrEmpty(pendingResponse))
        {
            if (ttsAgent != null) ttsAgent.SpeakText(pendingResponse);
            pendingResponse = null;
        }
    }

    void OnApplicationQuit()
    {
        if (client != null && client.IsConnected) client.Disconnect();
    }
}