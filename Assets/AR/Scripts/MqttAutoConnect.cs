using UnityEngine;
using M2MqttUnity;

public class MqttAutoConnect : M2MqttUnityClient
{
    protected override void Start()
    {
        base.Start(); // This is required for M2MqttUnityClient to initialize
        
        // Auto-connect on launch
        Connect(); 
        Debug.Log("MQTT: Auto-connect initiated on startup.");
    }

    // Optional: Override this to handle the connection success
    protected override void OnConnected()
    {
        base.OnConnected();
        Debug.Log("MQTT: Successfully connected automatically!");
    }
}