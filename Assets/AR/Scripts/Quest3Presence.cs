using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using System.Text;

public class Quest3Presence : MonoBehaviour
{
    private MqttClient client;
    public string brokerIpAddress = "100.X.X.X"; // Your Pi's Tailscale IP

    void Start()
    {
        client = new MqttClient(brokerIpAddress);
        client.Connect("Quest3_VR_Headset"); // A unique name for the headset
        
        // The Magic Line: Tell the network the app is running
        client.Publish("home/quest3/status", Encoding.UTF8.GetBytes("headset_on"));
        Debug.Log("Broadcasted: I am online and ready!");
    }

    void OnApplicationQuit() // When you close the VR app
    {
        // Tell the network the app is closing
        client.Publish("home/quest3/status", Encoding.UTF8.GetBytes("headset_off"));
        client.Disconnect();
    }
}