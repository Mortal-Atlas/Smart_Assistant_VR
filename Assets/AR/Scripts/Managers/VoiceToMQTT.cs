using UnityEngine;

public class VoiceToMQTT : MonoBehaviour
{
    [Header("MQTT Settings")]
    [Tooltip("The exact MQTT topic that margo_brain.py is listening to for user messages")]
    public string margoChatTopic = "rika/prompt"; // Change this if your python script uses a different topic!

    // IMPORTANT: This method MUST be called dynamically by the Meta STT Block
    public void SendVoiceCommandToMargo(string transcribedText)
    {
        // Don't send empty messages if the mic picked up background noise
        if (string.IsNullOrWhiteSpace(transcribedText)) return;

        Debug.Log($"[VoiceToMQTT] Forwarding STT to Brain: {transcribedText}");
        
        if (MqttQuestBridge.Instance != null)
        {
            // Send the transcribed text to your Python brain!
            MqttQuestBridge.Instance.PublishMessage(margoChatTopic, transcribedText);
        }
        else
        {
            Debug.LogError("[VoiceToMQTT] MqttQuestBridge is missing from the scene!");
        }
    }
}