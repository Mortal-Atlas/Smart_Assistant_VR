using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using UnityEngine;

public class SpotifyController : M2MqttUnityClient
{
    public AudioSource musicSource;

    protected override void SubscribeTopics()
    {
        client.Subscribe(new string[] { "home/media/spotify/command" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
    }

    protected override void DecodeMessage(string topic, byte[] message)
    {
        string msg = System.Text.Encoding.UTF8.GetString(message);
        if (topic == "home/media/spotify/command")
        {
            if (msg == "play")
            {
                // Trigger your AudioSource or play logic here
                musicSource.Play();
            }
        }
    }
}