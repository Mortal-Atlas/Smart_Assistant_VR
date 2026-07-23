using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class VRElevenLabsTTS : MonoBehaviour
{
    [Header("ElevenLabs Configuration")]
    [Tooltip("Leave this blank in the inspector. It will load from Resources/elevenlabs_key.txt")]
    [SerializeField] private string apiKey = "";
    [SerializeField] private string voiceId = "YOUR_VOICE_ID";
    [SerializeField] private string modelId = "eleven_flash_v2_5";
    
    private AudioSource audioSource;
    private const string apiUrl = "https://api.elevenlabs.io/v1/text-to-speech";

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        LoadAPIKey();
    }

    private void LoadAPIKey()
    {
        // Loads the key from a text file located at Assets/Resources/elevenlabs_key.txt
        TextAsset keyFile = Resources.Load<TextAsset>("elevenlabs_key");
        if (keyFile != null)
        {
            apiKey = keyFile.text.Trim();
        }
        else
        {
            Debug.LogError("[VRElevenLabsTTS] API Key file not found! Please create Assets/Resources/elevenlabs_key.txt");
        }
    }

    public void Speak(string textToSpeak)
    {
        if (string.IsNullOrWhiteSpace(textToSpeak)) return;
        
        Debug.Log($"[VRElevenLabsTTS] Preparing to speak: {textToSpeak}");
        StartCoroutine(SendTTSRequest(textToSpeak));
    }

    private IEnumerator SendTTSRequest(string text)
    {
        // Set optimize_streaming_latency=2 or 3 for faster responses
        string requestUrl = apiUrl + voiceId + "?optimize_streaming_latency=2";

        // Manually format the JSON to avoid needing extra libraries
        // Note: Formatting is specific to ElevenLabs API requirements
        string jsonData = $@"
        {{
            ""text"": ""{text}"",
            ""model_id"": ""{modelId}"",
            ""voice_settings"": {{
                ""stability"": 0.5,
                ""similarity_boost"": 0.7
            }}
        }}";

        byte[] postData = Encoding.UTF8.GetBytes(jsonData);

        // Setup the UnityWebRequest to receive an audio clip
        using (UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(requestUrl, AudioType.MPEG))
        {
            request.method = "POST";
            
            // Set the payload and headers
            request.uploadHandler = new UploadHandlerRaw(postData);
            request.uploadHandler.contentType = "application/json";
            request.SetRequestHeader("xi-api-key", apiKey);
            request.SetRequestHeader("Accept", "audio/mpeg");

            // Send the request and wait for a response
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.ConnectionError || request.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError($"[VRElevenLabsTTS] API Error: {request.error}");
                Debug.LogError($"[VRElevenLabsTTS] Response: {request.downloadHandler.text}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log("[VRElevenLabsTTS] Playing audio successfully!");
                }
            }
        }
    }
}