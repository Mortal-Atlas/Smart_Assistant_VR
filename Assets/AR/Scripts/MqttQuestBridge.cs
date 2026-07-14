using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;

[System.Serializable]
public class RikaCommand
{
    public string command;
}

[System.Serializable]
public class PhonePoseData
{
    public float px, py, pz;
    public float qx, qy, qz, qw;
}

public class MqttQuestBridge : M2MqttUnityClient
{
    [Header("Rika Integration")]
    [Tooltip("Drag the GameObject with the RikaAgent script here")]
    public RikaAgent rikaAgent; 

    [Header("AR Tracking")]
    [Tooltip("Drag the Manager object holding the ImageTrackerHandler here")]
    public ImageTrackerHandler imageTrackerHandler;

    // We don't need to override Start with Task.Run anymore.
    // The base M2MqttUnityClient actually handles the threaded socket connection safely!
    protected override void Start()
    {
        // Let the base class handle its own startup sequence normally.
        base.Start();
    }

    protected override void OnConnected()
    {
        base.OnConnected();
        
        // Subscribe to the topic as soon as the connection is successful, adding the new pose topic
        client.Subscribe(new string[] { "rika/commands", "rika/phone/pose" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
        
        // Broadcast that the VR headset is online and RETAIN the message (the 'true' flag at the end)
        // This solves your race condition! The broker holds this message for the phone when it joins.
        client.Publish("vr/status", System.Text.Encoding.UTF8.GetBytes("online"), MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE, true);
        
        Debug.Log("Connected to the Rika Message Bus! Waiting for commands and phone pose data...");
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
        
        // We only log non-pose messages so we don't spam the console 60 times a second
        if (topic != "rika/phone/pose")
        {
            Debug.Log("Raw message received on " + topic + ": " + msg);
        }
        
        if (topic == "rika/phone/pose")
        {
            try
            {
                PhonePoseData pose = JsonUtility.FromJson<PhonePoseData>(msg);
                if (imageTrackerHandler != null)
                {
                    Vector3 pos = new Vector3(pose.px, pose.py, pose.pz);
                    Quaternion rot = new Quaternion(pose.qx, pose.qy, pose.qz, pose.qw);

                    // Push to main thread since we are modifying transforms in the tracker handler
                    UnityMainThreadDispatcher.Instance().Enqueue(() => 
                    {
                        imageTrackerHandler.UpdateFallbackSensorData(pos, rot);
                    });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError("Failed to parse pose data: " + e.Message);
            }
        }
        else if (topic == "rika/commands")
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
                        else 
                        {
                            Debug.LogError("Hey! You forgot to assign the RikaAgent in the Unity Inspector!");
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
    }
}