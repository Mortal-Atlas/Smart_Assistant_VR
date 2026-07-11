using UnityEngine;

public class DigitalTwinTracker : MonoBehaviour
{
    [Header("Tracking Architecture")]
    [Tooltip("The actual GameObject/Transform that represents the tracked fiducial marker.")]
    public Transform trackedFiducial; 
    
    [Tooltip("The 3D model of your phone (e.g., your Virtual_S24_Ultra).")]
    public GameObject virtualPhone;

    [Header("Holographic Offset")]
    [Tooltip("X = Left/Right, Y = Up/Down, Z = Depth. 0.0127 meters is exactly 0.5 inches.")]
    // NOTE: If the virtual phone spawns BEHIND your physical phone, change Z to -0.0127f
    public Vector3 localOffset = new Vector3(0f, 0f, 0.0127f); 

    void Update()
    {
        // Only update if the headset can actually see the marker
        if (trackedFiducial != null && trackedFiducial.gameObject.activeInHierarchy)
        {
            if (!virtualPhone.activeSelf) 
                virtualPhone.SetActive(true);

            // 1. Lock the rotation to perfectly match the physical phone
            virtualPhone.transform.rotation = trackedFiducial.rotation;

            // 2. Apply the 0.5-inch offset projecting outward from the screen
            virtualPhone.transform.position = trackedFiducial.TransformPoint(localOffset);
        }
        else
        {
            // Hide the virtual phone if you put the physical phone in your pocket
            if (virtualPhone.activeSelf) 
                virtualPhone.SetActive(false);
        }
    }
}