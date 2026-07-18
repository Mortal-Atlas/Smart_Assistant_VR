using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MJPEGStreamDecoder : MonoBehaviour
{
    [Tooltip("The IP Webcam video URL. e.g., http://10.94.93.86:8080/shot.jpg")]
    public string streamUrl = "http://10.94.93.86:8080/shot.jpg";
    
    [Tooltip("The RawImage component on your UI Canvas where the video will display.")]
    public RawImage displayImage;

    [Tooltip("How often to fetch a new frame (in seconds). Lower number = faster stream. 0.016 is ~60fps, 0.033 is ~30fps.")]
    [Range(0.01f, 1f)]
    public float fetchInterval = 0.033f;

    private Texture2D currentFrame;
    private bool isStreaming = false;

    void Awake()
    {
        // Initialize an empty texture to prevent errors before the first frame arrives
        currentFrame = new Texture2D(2, 2);
        // Fill the initial texture with a solid color (e.g., black or white) to prevent transparency
        Color[] pixels = new Color[4];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.white;
        currentFrame.SetPixels(pixels);
        currentFrame.Apply();
        
        if (displayImage != null)
        {
            displayImage.texture = currentFrame;
            // Ensure the RawImage component itself isn't completely transparent
            displayImage.color = Color.white; 
        }
    }

    // Automatically start the stream when the UI panel turns on
    void OnEnable()
    {
        StartStream();
    }

    // Automatically stop the stream when the UI panel turns off
    void OnDisable()
    {
        StopStream();
    }

    public void StartStream()
    {
        if (!isStreaming)
        {
            isStreaming = true;
            StartCoroutine(FetchFrames());
        }
    }

    public void StopStream()
    {
        isStreaming = false;
        StopAllCoroutines();
    }

    private IEnumerator FetchFrames()
    {
        while (isStreaming)
        {
            // We use /shot.jpg instead of /video. This pulls the latest single frame.
            using (UnityWebRequest uwr = UnityWebRequestTexture.GetTexture(streamUrl))
            {
                yield return uwr.SendWebRequest();

                if (uwr.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("MJPEG Fetch Error: " + uwr.error);
                }
                else
                {
                    // Load the downloaded image data into our texture
                    Texture2D downloadedTexture = DownloadHandlerTexture.GetContent(uwr);
                    
                    // We need to destroy the old texture to prevent massive memory leaks
                    if (currentFrame != null)
                    {
                        Destroy(currentFrame);
                    }

                    currentFrame = downloadedTexture;
                    displayImage.texture = currentFrame;
                }
            }
            
            // Wait before pulling the next frame to control the framerate
            yield return new WaitForSeconds(fetchInterval);
        }
    }
}