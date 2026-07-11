using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages; // Required for QoS Levels
using System.Text;

public class Quest3Presence : MonoBehaviour
{
    private MqttClient client;

    [Header("Broker Configuration")]
    public string brokerIpAddress = "192.168.X.X"; // Your Pi's Local IP
    public int brokerPort = 1883;
    public string mqttUsername = "pi";
    public string mqttPassword = "yourpassword";

    void Start()
    {
        client = new MqttClient(brokerIpAddress, brokerPort, false, null, null, MqttSslProtocols.None);
        
        string clientId = "Quest3_VR_Headset_" + System.Guid.NewGuid().ToString();
        
        // 1. Define the Last Will and Testament
        string willTopic = "home/quest3/status";
        string willMessage = "headset_off";

        try 
        {
            // 2. Connect USING THE LWT OVERLOAD
            client.Connect(
                clientId, 
                mqttUsername, 
                mqttPassword, 
                false,                              // willRetain
                MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, // willQosLevel
                true,                               // willFlag (Turns ON the Last Will)
                willTopic, 
                willMessage, 
                true,                               // cleanSession
                5                                   // keepAlivePeriod (Seconds before Pi declares headset dead)
            );
            
            if (client.IsConnected)
            {
                client.Publish("home/quest3/status", Encoding.UTF8.GetBytes("headset_on"));
                Debug.Log("🔥 VR Online: Broadcasted 'headset_on' to Pi.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("VR Failed to connect to broker: " + e.Message);
        }
    }

    void OnApplicationQuit()
    {
        if (client != null && client.IsConnected)
        {
            // We still try to send it politely if it's a clean quit
            client.Publish("home/quest3/status", Encoding.UTF8.GetBytes("headset_off"));
            client.Disconnect();
        }
    }
}