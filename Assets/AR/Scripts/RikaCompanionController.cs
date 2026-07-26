using UnityEngine;

public class RikaCompanionController : MonoBehaviour
{
    [Header("Tracking Targets")]
    [Tooltip("Drag your Main Camera (VR Headset) here.")]
    public Transform playerHead;

    [Header("Positioning Offsets")]
    [Tooltip("Where she floats when ignoring you (e.g., to your right and slightly behind).")]
    public Vector3 idleOffset = new Vector3(0.8f, -0.2f, -0.2f);
    
    [Tooltip("Where she floats when listening (e.g., right in front of you).")]
    public Vector3 listeningOffset = new Vector3(0f, -0.1f, 1.2f);

    [Header("Movement Settings")]
    public float glideSpeed = 3.0f;
    public float rotationSpeed = 5.0f;

    [Header("Ghostly Float Settings")]
    public float floatAmplitude = 0.05f;
    public float floatFrequency = 1.0f;

    // Internal State
    private bool isListening = false;
    private Vector3 currentVelocity; // Used for SmoothDamp

    void Update()
    {
        if (playerHead == null) return;

        UpdatePositionAndRotation();
    }

    private void UpdatePositionAndRotation()
    {
        // 1. Get the player's forward/right directions, ignoring pitch/roll
        Vector3 flatForward = playerHead.forward;
        flatForward.y = 0;
        flatForward.Normalize();

        Vector3 flatRight = playerHead.right;
        flatRight.y = 0;
        flatRight.Normalize();

        // 2. Determine which offset to use based on her state
        Vector3 targetOffset = isListening ? listeningOffset : idleOffset;

        // 3. Calculate the exact target position in world space
        Vector3 targetPosition = playerHead.position 
                               + (flatRight * targetOffset.x) 
                               + (Vector3.up * targetOffset.y) 
                               + (flatForward * targetOffset.z);

        // 4. Add the elegant ghostly float (Sine wave)
        float hoverY = Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        targetPosition.y += hoverY;

        // 5. Glide smoothly to the target position
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * glideSpeed);

        // 6. Always smoothly rotate to look at the player's face
        Vector3 lookDirection = playerHead.position - transform.position;
        lookDirection.y = 0; // Keep her perfectly upright, no tilting
        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }

    // --- PUBLIC METHODS TO CALL FROM YOUR UI/BUTTONS ---

    public void CallRikaToListen()
    {
        isListening = true;
        // Optional: Trigger a "listening" animation or particle effect here
    }

    public void DismissRika()
    {
        isListening = false;
        // Optional: Trigger a "thinking" or "idle" animation here
    }
}