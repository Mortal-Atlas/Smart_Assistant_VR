using UnityEngine;
using Meta.XR.BuildingBlocks.AIBlocks;
using System.Collections;
using UnityEngine.Android; 

public class RikaWakeWord : MonoBehaviour
{
    [Header("Connections")]
    public GeminiManager geminiManager;
    [Tooltip("Drag the [BuildingBlock] Speech To Text object here")]
    public SpeechToTextAgent sttAgent;

    [Header("Settings")]
    public string wakeWord = "rika";
    public float gracePeriod = 4.0f;

    private bool isListening = false;
    private string commandPayload = "";
    private bool isAwake = false;
    private Coroutine silenceTimer;
    private float statusTimer = 0f;

    private void Start()
    {
        // Request Microphone permission only if on Android Standalone
        #if UNITY_ANDROID && !UNITY_EDITOR
        if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
        {
            Permission.RequestUserPermission(Permission.Microphone);
        }
        #endif
        
        Debug.Log("RikaWakeWord Initialized. Ready for Poke interaction.");
    }

    private void Update()
    {
        // HEARTBEAT TRACER: Check the agent's status every 3 seconds while active
        if (isListening)
        {
            statusTimer += Time.deltaTime;
            if (statusTimer > 3.0f)
            {
                statusTimer = 0f;
                if (sttAgent != null)
                {
                    Debug.Log($"<color=orange>[HEARTBEAT]</color> STT Agent Enabled: {sttAgent.enabled}");
                }
            }
        }
    }

    public void ToggleListening()
    {
        isListening = !isListening;

        if (isListening)
        {
            // Security Check
            #if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Debug.LogWarning("Microphone permission not granted yet!");
                isListening = false;
                return;
            }
            #endif

            // Enable the STT agent to grab the hardware
            sttAgent.enabled = true;
            sttAgent.StartListening();
            Debug.Log("Microphone ON - Listening via Meta SDK...");
        }
        else
        {
            sttAgent.enabled = false;
            Debug.Log("Microphone OFF");
        }
    }

    public void ProcessIncomingSpeech(string transcript)
    {
        if (!isListening || string.IsNullOrWhiteSpace(transcript)) return;

        // Keep the mic alive for 24/7 listening
        sttAgent.StartListening();

        Debug.Log($"<color=cyan>[RAW STT INPUT]</color> {transcript}");

        string lowerTranscript = transcript.ToLower();
        string safeWakeWord = wakeWord.Trim().ToLower();

        if (!isAwake)
        {
            if (lowerTranscript.Contains(safeWakeWord))
            {
                isAwake = true;
                Debug.Log("<color=green>[WAKE WORD]</color> Rika is awake!");
                
                int startIndex = lowerTranscript.IndexOf(safeWakeWord) + safeWakeWord.Length;
                commandPayload = (startIndex < transcript.Length) ? transcript.Substring(startIndex).Trim() : "";
                ResetSilenceTimer();
            }
        }
        else
        {
            commandPayload += " " + transcript.Trim();
            ResetSilenceTimer();
        }
    }

    private void ResetSilenceTimer()
    {
        if (silenceTimer != null) StopCoroutine(silenceTimer);
        silenceTimer = StartCoroutine(SilenceCountdown());
    }

    private IEnumerator SilenceCountdown()
    {
        yield return new WaitForSeconds(gracePeriod);

        if (!string.IsNullOrWhiteSpace(commandPayload))
        {
            Debug.Log($"<color=yellow>[SENDING]</color> Sending payload to Gemini: {commandPayload}");
            if (geminiManager != null)
            {
                geminiManager.SendMessageToRika(commandPayload);
            }
        }

        isAwake = false;
        commandPayload = "";
    }
}