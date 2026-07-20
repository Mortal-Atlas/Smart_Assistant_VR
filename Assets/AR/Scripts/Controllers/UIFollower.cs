using UnityEngine;

public class UIFollower : MonoBehaviour
{
    [Header("Tracking")]
    [Tooltip("Drag your Main Camera (VR Headset) here. If left blank, it will auto-find it.")]
    public Transform playerCamera;

    [Header("Positioning")]
    [Tooltip("How fast the UI catches up to you. Higher = faster (less floaty).")]
    public float followSpeed = 10f;
    
    [Tooltip("Distance in front of your face (meters).")]
    public float forwardDistance = 0.5f;
    
    [Tooltip("Height offset from your eyes (negative is lower).")]
    public float heightOffset = -0.3f;
    
    [Tooltip("Lateral offset. Negative moves it to your left hand, positive to your right.")]
    public float lateralOffset = -0.3f;

    [Header("Rotation")]
    [Tooltip("Your manual tilt preference in degrees (X axis). 0 is flat, 45 is tilted back like a book.")]
    public float manualTiltX = 45f;

    private void Start()
    {
        // Auto-grab the headset if you forgot to drag it in
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    private void LateUpdate()
    {
        if (playerCamera == null) return;

        // 1. Flatten the camera's forward and right vectors so the UI doesn't tilt when you look up/down
        Vector3 flatForward = playerCamera.forward;
        flatForward.y = 0;
        flatForward.Normalize();

        Vector3 flatRight = playerCamera.right;
        flatRight.y = 0;
        flatRight.Normalize();

        // 2. Calculate exactly where the UI *should* be
        Vector3 targetPos = playerCamera.position 
                            + (flatForward * forwardDistance) 
                            + (flatRight * lateralOffset) 
                            + (Vector3.up * heightOffset);

        // 3. Smoothly move (Lerp) the UI towards that target position
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followSpeed);

        // 4. Calculate the rotation so it always faces the player
        // We calculate direction FROM the camera TO the UI so the text isn't backwards
        Vector3 lookDirection = transform.position - playerCamera.position;
        lookDirection.y = 0; // Keep the core billboard flat on the Y axis

        if (lookDirection != Vector3.zero)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDirection);
            
            // Extract the Euler angles, force your preferred manual tilt, and pack it back up
            Vector3 euler = targetRot.eulerAngles;
            euler.x = manualTiltX; 
            targetRot = Quaternion.Euler(euler);

            // 5. Smoothly rotate (Slerp) the UI
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * followSpeed);
        }
    }
}