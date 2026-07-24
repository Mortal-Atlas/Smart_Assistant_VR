using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using TMPro;

public class SpotifyController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text artistText;
    [SerializeField] private RawImage albumCoverImage;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private TMP_Text timeText; // Optional: e.g. "1:24 / 3:45"

    [Header("MQTT Bridge Reference")]
    [Tooltip("Drag your MqttQuestBridge GameObject here")]
    [SerializeField] private MqttQuestBridge mqttBridge;

    [Header("Playback State")]
    private float currentProgressMs = 0f;
    private float totalDurationMs = 1f;
    private bool isPlaying = false;
    private string currentAlbumUrl = "";

    [System.Serializable]
    public class SpotifyTrackData
    {
        public string title;
        public string artist;
        public string album_art_url;
        public int progress_ms;
        public int duration_ms;
        public bool is_playing;
    }

    private void Update()
    {
        // Smooth local progression if playing
        if (isPlaying && totalDurationMs > 0)
        {
            currentProgressMs += Time.deltaTime * 1000f;
            currentProgressMs = Mathf.Clamp(currentProgressMs, 0f, totalDurationMs);
            
            UpdateProgressUI();
        }
    }

    public void UpdateState(string jsonPayload)
    {
        try
        {
            SpotifyTrackData data = JsonUtility.FromJson<SpotifyTrackData>(jsonPayload);
            if (data == null) return;

            // 1. Update Title & Artist
            if (titleText != null) titleText.text = string.IsNullOrEmpty(data.title) ? "Not Playing" : data.title;
            if (artistText != null) artistText.text = string.IsNullOrEmpty(data.artist) ? "Unknown Artist" : data.artist;

            // 2. Update Progress Values
            currentProgressMs = data.progress_ms;
            totalDurationMs = Mathf.Max(1, data.duration_ms);
            isPlaying = data.is_playing;
            UpdateProgressUI();

            // 3. Fetch Album Art if changed
            if (!string.IsNullOrEmpty(data.album_art_url) && data.album_art_url != currentAlbumUrl)
            {
                currentAlbumUrl = data.album_art_url;
                StartCoroutine(DownloadAlbumArt(currentAlbumUrl));
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SpotifyController] Error parsing JSON: {ex.Message}");
        }
    }

    
    // Wire this to your Play/Pause Poke Button
    public void OnPlayPauseClicked()
    {
        if (mqttBridge != null) mqttBridge.PublishSpotifyCommand("play_pause");
    }

    // Wire this to your Next Poke Button
    public void OnNextClicked()
    {
        if (mqttBridge != null) mqttBridge.PublishSpotifyCommand("next");
    }

    // Wire this to your Previous/Rewind Poke Button
    public void OnPreviousClicked()
    {
        if (mqttBridge != null) mqttBridge.PublishSpotifyCommand("previous");
    }

    private void UpdateProgressUI()
    {
        float ratio = Mathf.Clamp01(currentProgressMs / totalDurationMs);

        if (progressSlider != null)
        {
            progressSlider.value = ratio;
        }

        if (timeText != null)
        {
            string currentFormatted = FormatTime(currentProgressMs);
            string totalFormatted = FormatTime(totalDurationMs);
            timeText.text = $"{currentFormatted} / {totalFormatted}";
        }
    }

    private string FormatTime(float timeMs)
    {
        TimeSpan t = TimeSpan.FromMilliseconds(timeMs);
        return string.Format("{0:D1}:{1:D2}", t.Minutes, t.Seconds);
    }

    private IEnumerator DownloadAlbumArt(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture = DownloadHandlerTexture.GetContent(request);
                if (albumCoverImage != null)
                {
                    albumCoverImage.texture = texture;
                    albumCoverImage.color = Color.white; // Ensure visibility
                }
            }
            else
            {
                Debug.LogWarning($"[SpotifyController] Failed to download album art: {request.error}");
            }
        }
    }
}