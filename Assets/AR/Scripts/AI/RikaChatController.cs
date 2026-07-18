using System.Collections;
using UnityEngine;
using TMPro;

public class RikaChatController : MonoBehaviour
{
    [Header("UI Elements")]
    [Tooltip("Drag your TextMeshPro text element here.")]
    public TextMeshProUGUI chatTextDisplay;

    [Header("Typewriter Settings")]
    [Tooltip("Time in seconds between each character.")]
    public float typingSpeed = 0.03f;

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
            // If Rika is already typing something, stop the old coroutine
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            
            // Start typing the new message
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