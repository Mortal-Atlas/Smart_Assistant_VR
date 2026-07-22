using UnityEngine;
using UnityEngine.Events;

public class SpotifyPanelController : MonoBehaviour
{
    [Header("Meta ISDK Button References")]
    [Tooltip("Drag your 'ButtonVisual' object from the Meta Poke Button here")]
    public MeshRenderer buttonVisualRenderer;
    
    [Tooltip("The texture (image) to show when music is PAUSED (Triangle)")]
    public Texture playIconTexture;
    
    [Tooltip("The texture (image) to show when music is PLAYING (Two vertical lines)")]
    public Texture pauseIconTexture;

    [Header("Progress Bar")]
    [Tooltip("Drag the Image acting as your progress bar (Set its Image Type to 'Filled')")]
    public UnityEngine.UI.Image progressBarFill;
    
    [Tooltip("(Optional) Text to show current time, e.g., '1:24'")]
    public TMPro.TMP_Text currentTimeText;
    
    [Tooltip("(Optional) Text to show total time, e.g., '3:45'")]
    public TMPro.TMP_Text totalTimeText;

    [Header("MQTT Events")]
    [Tooltip("Fires when the user hits PLAY. Hook this up to your MQTT Bridge!")]
    public UnityEvent OnSendPlayCommand;
    
    [Tooltip("Fires when the user hits PAUSE. Hook this up to your MQTT Bridge!")]
    public UnityEvent OnSendPauseCommand;

    private bool isPlaying = false;

    // Link this method to your Meta Poke Interactable's Unity Event!
    public void TogglePlayPause()
    {
        isPlaying = !isPlaying;

        if (isPlaying)
        {
            // Update the 3D material to show the Pause icon
            if (buttonVisualRenderer != null && pauseIconTexture != null)
            {
                buttonVisualRenderer.material.mainTexture = pauseIconTexture;
            }
            
            Debug.Log("[Spotify] Sending PLAY command...");
            OnSendPlayCommand?.Invoke();
        }
        else
        {
            // Update the 3D material to show the Play icon
            if (buttonVisualRenderer != null && playIconTexture != null)
            {
                buttonVisualRenderer.material.mainTexture = playIconTexture;
            }
            
            Debug.Log("[Spotify] Sending PAUSE command...");
            OnSendPauseCommand?.Invoke();
        }
    }

    public void ForceSyncUIState(bool isCurrentlyPlaying)
    {
        isPlaying = isCurrentlyPlaying;
        if (buttonVisualRenderer != null)
        {
            buttonVisualRenderer.material.mainTexture = isPlaying ? pauseIconTexture : playIconTexture;
        }
    }

    public void UpdateSongProgress(float currentSeconds, float totalSeconds)
    {
        if (totalSeconds > 0)
        {
            if (progressBarFill != null)
            {
                progressBarFill.fillAmount = Mathf.Clamp01(currentSeconds / totalSeconds);
            }

            if (currentTimeText != null)
            {
                currentTimeText.text = FormatTime(currentSeconds);
            }
            if (totalTimeText != null)
            {
                totalTimeText.text = FormatTime(totalSeconds);
            }
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int minutes = Mathf.FloorToInt(timeInSeconds / 60F);
        int seconds = Mathf.FloorToInt(timeInSeconds - minutes * 60);
        return string.Format("{0:0}:{1:00}", minutes, seconds);
    }
}