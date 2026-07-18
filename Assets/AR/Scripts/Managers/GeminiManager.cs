using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class GeminiManager : MonoBehaviour
{
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

        // Tell the Pi to grab a frame from the phone using our clean Singleton helper!
        MqttQuestBridge.Instance.PublishMessage(MargoTopics.VisionCapture, "SNAP");
    }

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