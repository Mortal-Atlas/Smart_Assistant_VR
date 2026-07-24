using UnityEngine;
using M2MqttUnity;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using TMPro; 
using System.Collections.Generic;

[System.Serializable]
public struct WeatherSpriteMap 
{ 
    public string condition; 
    public Sprite sprite; 
}

public class WeatherWidgetReceiver : MonoBehaviour
{
    [Header("MQTT Connection")]
    public M2MqttUnityClient mqttManager;
    public string topic = "vr/environment/update";

    [Header("UI Display")]
    public TextMeshPro tempText;
    public TextMeshPro conditionText;

    [Header("Layout Settings")]
    public float spacing = 0.5f;
    public float zOffset = -0.05f; // Adjust this to push sprites out from the UI board!
    public float spriteScale = 0.1f; // Shrink those giant images down!
    public Transform[] forecastAnchors = new Transform[5];

    [Header("Visual Mapping")]
    public List<WeatherSpriteMap> weatherVisuals;

    // This runs the microsecond the game starts. 
    void Awake()
    {
        Debug.Log("Rika says: AWAKE! The Weather Widget GameObject actually exists!");
    }

    // Standard Start function
    void Start()
    {
        Debug.Log("Rika says: START! Running the setup...");
        ArrangeAnchors();

        if (mqttManager == null)
        {
            Debug.LogError("Rika says: You forgot to assign the MQTT Manager in the inspector!");
            return;
        }

        // Boot up the waiting routine
        StartCoroutine(WaitForMQTT());
    }

    System.Collections.IEnumerator WaitForMQTT()
    {
        Debug.Log("Rika says: Waiting for MQTT connection...");
        while (mqttManager.client == null || !mqttManager.client.IsConnected)
        {
            yield return null;
        }

        Debug.Log("Rika says: MQTT Connected! Subscribing to " + topic);
        mqttManager.client.MqttMsgPublishReceived += OnMessageReceived;
        mqttManager.client.Subscribe(new string[] { topic }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
    }

    [ContextMenu("Arrange Anchors")]
    public void ArrangeAnchors()
    {
        for (int i = 0; i < forecastAnchors.Length; i++)
        {
            if (forecastAnchors[i] != null)
            {
                // We inject your new Z-Offset right here
                forecastAnchors[i].localPosition = new Vector3(i * spacing, 0, zOffset);
            }
        }
    }

    void OnMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        // CRITICAL FIX: Since the MQTT Client shares events, this script hears EVERY topic.
        // If the topic doesn't match our specific weather topic, ignore it immediately!
        if (e.Topic != topic) return;

        string json = Encoding.UTF8.GetString(e.Message);
        
        Debug.Log("Rika says: I got a message! Here is the JSON: " + json);
        
        try 
        {
            WeatherData data = JsonUtility.FromJson<WeatherData>(json);
            UnityMainThreadDispatcher.Instance().Enqueue(() => RefreshWidget(data));
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Rika says: The JSON from Home Assistant is broken! Error: " + ex.Message);
        }
    }

    void RefreshWidget(WeatherData data)
    {
        Debug.Log($"Rika says: Updating UI with Temp: {data.temp}, Condition: {data.condition}");

        if (tempText != null) tempText.text = data.temp.ToString("F1") + "°";
        if (conditionText != null) conditionText.text = data.condition;

        if (forecastAnchors.Length > 0 && forecastAnchors[0] != null)
        {
            SpriteRenderer sr = forecastAnchors[0].GetComponentInChildren<SpriteRenderer>();
            if (sr == null)
            {
                sr = forecastAnchors[0].gameObject.AddComponent<SpriteRenderer>();
            }

            // Apply your custom shrink scale here!
            sr.transform.localScale = new Vector3(spriteScale, spriteScale, spriteScale);

            bool foundSprite = false;
            foreach (var map in weatherVisuals)
            {
                if (map.condition == data.condition)
                {
                    sr.sprite = map.sprite;
                    foundSprite = true;
                    Debug.Log("Rika says: Found the matching sprite for " + data.condition);
                    break;
                }
            }

            if (!foundSprite)
            {
                Debug.LogWarning("Rika says: I couldn't find a sprite in your list for the condition: " + data.condition);
            }
        }
    }
}

[System.Serializable]
public class WeatherData 
{
    public float temp;
    public string condition;
}