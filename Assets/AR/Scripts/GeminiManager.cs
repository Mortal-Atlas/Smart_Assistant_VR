using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GeminiManager : MonoBehaviour
{
    [Header("Networking")]
    [Tooltip("Reference to the main MQTT Bridge")]
    public MqttQuestBridge mqttBridge;

    [Header("Input")]
    [Tooltip("Input Action for Right Trigger Pull")]
    public InputActionReference rightTriggerAction;

    [Header("UI Elements")]
    public TextMeshProUGUI scannerOutputText;
    public GameObject loadingIndicator;

    private bool isScanning = false;

    private void OnEnable()
    {
        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.Enable();
            rightTriggerAction.action.performed += OnTriggerPulled;
        }
    }

    private void OnDisable()
    {
        if (rightTriggerAction != null && rightTriggerAction.action != null)
        {
            rightTriggerAction.action.performed -= OnTriggerPulled;
            rightTriggerAction.action.Disable();
        }
    }

    private void OnTriggerPulled(InputAction.CallbackContext context)
    {
        // Only allow scanning if the Scanner panel is actually active
        // and we aren't already waiting for a response
        if (!gameObject.activeInHierarchy || isScanning) return;

        ExecuteScan();
    }

    public void ExecuteScan()
    {
        isScanning = true;
        
        if (scannerOutputText != null) 
            scannerOutputText.text = "Margo is analyzing...";
            
        if (loadingIndicator != null) 
            loadingIndicator.SetActive(true);

        // Tell the Pi to grab a frame from the phone and run it through Gemini
        if (mqttBridge != null && mqttBridge.client != null && mqttBridge.client.IsConnected)
        {
            Debug.Log("Scanner Triggered. Requesting vision analysis...");
            // Using a new topic specifically for vision commands
            mqttBridge.client.Publish("margo/vision/capture", System.Text.Encoding.UTF8.GetBytes("SNAP"), uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
        }
        else
        {
            Debug.LogWarning("Scanner failed: MQTT bridge is disconnected.");
            isScanning = false;
            if (loadingIndicator != null) loadingIndicator.SetActive(false);
            if (scannerOutputText != null) scannerOutputText.text = "Connection error. Margo can't see right now.";
        }
    }

    // --- THE MISSING METHOD ---
    // Called by MqttQuestBridge when "margo/vision/result" receives a payload
    public void UpdateScanResult(string resultText)
    {
        isScanning = false;
        
        if (loadingIndicator != null) 
            loadingIndicator.SetActive(false);

        if (scannerOutputText != null)
        {
            scannerOutputText.text = resultText;
        }
        
        Debug.Log("Scan complete: " + resultText);
    }
}