using UnityEngine;
using Meta.XR.BuildingBlocks.AIBlocks;

public class SttToggle : MonoBehaviour
{
    [Header("Settings")]
    public SpeechToTextAgent sttAgent;
    
    private bool isListening = false;

    // Call this method from your VR Button's 'OnClick' event
    public void ToggleMic()
    {
        if (sttAgent == null) 
        {
            Debug.LogError("SttToggle: Missing reference to SpeechToTextAgent!");
            return;
        }

        else
        {
            sttAgent.StartListening();
            Debug.Log("[Rika Network] Microphone ON");
        }
    }
}