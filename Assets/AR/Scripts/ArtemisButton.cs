using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;

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
       if(Input.GetKeyDown(KeyCode.Space)) // For testing in the Unity Editor
       {
           ToggleSwitch();
       }
    }
}