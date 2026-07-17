using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.Networking;

[System.Serializable]
public class SpotifyState
{
    public string track_name;
    public string artist_name;
    public string album_art_url;
    public bool is_playing;
}

public class SpotifyController : MonoBehaviour
{
    [Header("Networking")]
    [Tooltip("Reference to the main MQTT Bridge")]
    public MqttQuestBridge mqttBridge;

    [Header("UI Elements")]
    public TextMeshProUGUI trackNameText;
    public TextMeshProUGUI artistNameText;
    
    [Tooltip("Use a RawImage for web textures, it's easier than converting to Sprites")]
    public RawImage albumArtImage; 
    
    [Tooltip("Optional: Objects to swap out based on playback state")]
    public GameObject playButtonIcon;
    public GameObject pauseButtonIcon;

    private string currentArtUrl = "";

    // Called from MqttQuestBridge when "rika/spotify/state" receives a message
    public void UpdateState(string jsonState)
    {
        try
        {
            SpotifyState state = JsonUtility.FromJson<SpotifyState>(jsonState);
            
            if (trackNameText != null) trackNameText.text = state.track_name;
            if (artistNameText != null) artistNameText.text = state.artist_name;

            // Toggle Play/Pause icons so the UI reflects reality
            if (playButtonIcon != null) playButtonIcon.SetActive(!state.is_playing);
            if (pauseButtonIcon != null) pauseButtonIcon.SetActive(state.is_playing);

            // Only fetch new album art if the song (and URL) actually changed
            if (albumArtImage != null && state.album_art_url != currentArtUrl && !string.IsNullOrEmpty(state.album_art_url))
            {
                currentArtUrl = state.album_art_url;
                StartCoroutine(DownloadAlbumArt(currentArtUrl));
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to parse Spotify State JSON: " + e.Message);
        }
    }

    private IEnumerator DownloadAlbumArt(string url)
    {
        using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(url))
        {
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.Success)
            {
                // Extract the downloaded image as a Texture2D and apply it to the RawImage
                Texture2D texture = DownloadHandlerTexture.GetContent(uwr);
                albumArtImage.texture = texture;
            }
            else
            {
                Debug.LogError($"Failed to download album art from {url}: {uwr.error}");
            }
        }
    }

    // --- UI Button Hooks (Link these in the Unity Inspector OnClick events) ---

    public void OnPlayPausePressed()
    {
        // HAOS spotifyplus usually accepts 'play_pause' to toggle the state
        mqttBridge.PublishSpotifyCommand("play_pause");
    }

    public void OnNextPressed()
    {
        mqttBridge.PublishSpotifyCommand("next");
    }

    public void OnPreviousPressed()
    {
        mqttBridge.PublishSpotifyCommand("previous");
    }

    // --- Extended Commands ---

    public void OnSkipForwardPressed()
    {
        // Send a skip forward command (e.g., +15 seconds)
        mqttBridge.PublishSpotifyCommand("skip_forward");
    }

    public void OnRewindPressed()
    {
        // Send a rewind command (e.g., -15 seconds)
        mqttBridge.PublishSpotifyCommand("rewind");
    }

    public void OnLikePressed()
    {
        // Send command to save track to user's Spotify library
        mqttBridge.PublishSpotifyCommand("like");
    }
}