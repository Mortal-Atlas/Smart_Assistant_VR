using UnityEngine;

public class FaceCamera : MonoBehaviour
{
    private Transform mainCamera;

    void Start()
    {
        // Grabs the main camera (the VR Headset)
        if (Camera.main != null)
        {
            mainCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Forces the UI to perfectly face the headset without flipping backwards
            transform.LookAt(transform.position + mainCamera.forward);
        }
    }
}