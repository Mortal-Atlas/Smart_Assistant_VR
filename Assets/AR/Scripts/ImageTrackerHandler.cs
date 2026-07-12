using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

[RequireComponent(typeof(ARTrackedImageManager))]
public class ImageTrackerHandler : MonoBehaviour
{
    private ARTrackedImageManager imageManager;
    
    [Tooltip("Drag the Virtual_S24_Ultra from the SCENE HIERARCHY here!")]
    public GameObject virtualPhone; 

    void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
        
        // Hide the phone as soon as the app starts
        if (virtualPhone != null) virtualPhone.SetActive(false);
    }

    // UPDATED FOR AR FOUNDATION 6.0
    // UPDATED FOR AR FOUNDATION 6.0 UnityEvent SYNTAX
    void OnEnable() => imageManager.trackablesChanged.AddListener(OnTrackablesChanged);
    void OnDisable() => imageManager.trackablesChanged.RemoveListener(OnTrackablesChanged);

    void OnTrackablesChanged(ARTrackablesChangedEventArgs<ARTrackedImage> eventArgs)
    {
        foreach (var newImage in eventArgs.added)
            UpdateTracking(newImage);

        foreach (var updatedImage in eventArgs.updated)
            UpdateTracking(updatedImage);

        foreach (var removedImage in eventArgs.removed)
            if (virtualPhone != null) virtualPhone.SetActive(false);
    }

    void UpdateTracking(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            virtualPhone.SetActive(true);
            // Moves the whole digital twin (including its child canvas) to the marker
            virtualPhone.transform.position = trackedImage.transform.position;
            virtualPhone.transform.rotation = trackedImage.transform.rotation;
        }
        else
        {
            virtualPhone.SetActive(false);
        }
    }
}