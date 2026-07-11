using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using UnityEngine.InputSystem;
public class ArtemisButton : MonoBehaviour
{
    [Tooltip("Drag your MQTT_manager object here")]
    public M2MqttUnityClient mqttManager;
    
    [Tooltip("The MQTT topic Home Assistant will listen to")]
    public string topic = "vr/artemis/toggle";

    // This is the function the VR button will trigger
    public void ToggleSwitch()
    {
        if (mqttManager != null && mqttManager.client != null)
        {
            // Send the word "TOGGLE" to the broker
            mqttManager.client.Publish(topic, Encoding.UTF8.GetBytes("TOGGLE"), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
            Debug.Log("VR Button Pressed: Sent MQTT command to Artemis.");
        }
        else
        {
            Debug.LogWarning("MQTT Manager is not assigned or not connected!");
        }
    }

   void Update()
    {
       if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
    {
        ToggleSwitch();
    }
    }
    void OnApplicationPause(bool pauseStatus)
{
    if (!pauseStatus) // If pauseStatus is false, the app has just RESUMED
    {
        Debug.Log("Headset resumed - Checking MQTT connection...");
        
        // If connected, disconnect cleanly first to avoid zombie sockets
        if (mqttManager.client != null && mqttManager.client.IsConnected)
        {
            mqttManager.client.Disconnect();
        }

        // Re-initiate connection
        mqttManager.Connect(); 
    }
}
// Inside your reconnection method (triggered by OnApplicationPause)
public void ReconnectMQTT()
{
    // Ensure the client is disconnected first
    if (mqttManager.client != null && mqttManager.client.IsConnected)
    {
        mqttManager.client.Disconnect();
    }

    // Force connect with cleanSession = false
    // You may need to provide the full list of arguments if it's not exposing it
    // The cleanSession parameter is typically the 9th argument in M2Mqtt
    mqttManager.client.Connect(
        "UnityQuestClient", // ClientID
        "unity_user",    // Username
        "12345",    // Password
        false,              // Will Retain
        0,                  // Will QoS
        false,              // Will Retain
        null,               // Will Topic
        null,               // Will Message
        false,              // <--- THIS IS THE CLEAN SESSION FLAG
        60                  // Keep Alive
    );
    
    
    Debug.Log("MQTT Reconnected with Persistent Session.");
}
}