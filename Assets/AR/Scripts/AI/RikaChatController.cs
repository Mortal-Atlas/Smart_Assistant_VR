using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Events; // NEW: Required to create Inspector events!

public class RikaChatController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Drag your TextMeshPro text element here.")]
    public TextMeshProUGUI chatTextDisplay;

    [Header("Typewriter Settings")]
    [Tooltip("Time in seconds between each character.")]
    public float typingSpeed = 0.03f;
    
    [Header("Audio Output")]
    [Tooltip("Drag your Meta Text-to-Speech (TTS) block here to make Margo talk.")]
    public UnityEvent<string> OnTextReadyForTTS; // NEW: The bridge to your TTS

    private Coroutine typingCoroutine;

    /// <summary>
    /// Call this method from your MqttQuestBridge when a message arrives on "rika/response".
    /// </summary>
    /// <param name="message">The text received from the Python backend.</param>
    public void OnMqttMessageReceived(string message)
    {
        // MQTT callbacks happen on a background thread.
        // We MUST use the main thread dispatcher to update the UI safely.
        UnityMainThreadDispatcher.Instance().Enqueue(() =>
        {
            // 1. SEND THE TEXT TO THE TTS VOICE INSTANTLY
            OnTextReadyForTTS?.Invoke(message);

            // 2. STOP OLD TYPING COROUTINE IF SHE IS ALREADY TALKING
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            
            // 3. START TYPING THE NEW MESSAGE ON THE UI
            typingCoroutine = StartCoroutine(TypewriterEffect(message));
        });
    }

    /// <summary>
    /// Types out the message one character at a time.
    /// </summary>
    private IEnumerator TypewriterEffect(string textToType)
    {
        chatTextDisplay.text = ""; // Clear old text

        foreach (char letter in textToType)
        {
            chatTextDisplay.text += letter;
            
            // Wait for the specified typing speed before showing the next letter
            yield return new WaitForSeconds(typingSpeed);
        }
        
        // Typing finished
        typingCoroutine = null;
    }
}   