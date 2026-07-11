using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;

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

    // NOTICE: I deleted the two 'brokerAddress' variables here! 

    protected override void OnConnected()
    {
        base.OnConnected();
        
        // Subscribe to the topic as soon as the connection is successful
        client.Subscribe(new string[] { "rika/commands" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        Debug.Log("Connected to the Rika Message Bus! Waiting for commands...");
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
                    if (rikaAgent != null) 
                    {
                        Debug.Log("Materializing Rika!");
                        rikaAgent.Materialize();
                    } 
                    else 
                    {
                        Debug.LogError("Hey! You forgot to assign the RikaAgent in the Unity Inspector!");
                    }
                }
                else if (data != null && data.command == "poof") 
                {
                    if (rikaAgent != null) 
                    {
                        Debug.Log("Poofing Rika away!");
                        rikaAgent.PoofAway();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse the JSON command. Did you format it right? Error: " + e.Message);
            }
        }
    }
}