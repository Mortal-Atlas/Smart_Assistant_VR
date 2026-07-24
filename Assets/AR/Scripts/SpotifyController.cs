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
    [SerializeField] private TMP_Text timeText;

    [Header("MQTT Bridge Reference")]
    [SerializeField] private MqttQuestBridge mqttBridge;
    [SerializeField] private string controlTopic = "rika/spotify/control";

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

            if (titleText != null) titleText.text = string.IsNullOrEmpty(data.title) ? "Not Playing" : data.title;
            if (artistText != null) artistText.text = string.IsNullOrEmpty(data.artist) ? "Unknown Artist" : data.artist;

            currentProgressMs = data.progress_ms;
            totalDurationMs = Mathf.Max(1, data.duration_ms);
            isPlaying = data.is_playing;
            UpdateProgressUI();

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

    // --- POKE BUTTON CALLBACK METHODS ---

    public void OnPlayPauseClicked()
    {
        SendControlCommand("play_pause");
    }

    public void OnNextClicked()
    {
        SendControlCommand("next");
    }

    public void OnPreviousClicked()
    {
        SendControlCommand("previous");
    }

    private void SendControlCommand(string command)
    {
        if (mqttBridge != null)
        {
            // Publishes command back to the Pi / python backend
            mqttBridge.PublishToTopic(controlTopic, command);
        }
        else
        {
            Debug.LogWarning($"[SpotifyController] MQTT Bridge reference missing! Command '{command}' not sent.");
        }
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
            TimeSpan current = TimeSpan.FromMilliseconds(currentProgressMs);
            TimeSpan total = TimeSpan.FromMilliseconds(totalDurationMs);
            timeText.text = $"{current:m\\:ss} / {total:m\\:ss}";
        }
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
                    albumCoverImage.color = Color.white;
                }
            }
        }
    }
}