using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

public class ImageTrackerHandler : MonoBehaviour
{
    private ARTrackedImageManager imageManager;
    public GameObject virtualPhone; // Drag your 3D prefab here

    void Awake()
    {
        imageManager = GetComponent<ARTrackedImageManager>();
    }

    void OnEnable() => imageManager.trackedImagesChanged += OnChanged;
    void OnDisable() => imageManager.trackedImagesChanged -= OnChanged;

    void OnChanged(ARTrackedImagesChangedEventArgs eventArgs)
    {
        foreach (var trackedImage in eventArgs.added) UpdatePhone(trackedImage);
        foreach (var trackedImage in eventArgs.updated) UpdatePhone(trackedImage);
    }

    void UpdatePhone(ARTrackedImage trackedImage)
    {
        if (trackedImage.trackingState == TrackingState.Tracking)
        {
            virtualPhone.SetActive(true);
            virtualPhone.transform.position = trackedImage.transform.position;
            virtualPhone.transform.rotation = trackedImage.transform.rotation;
        }
        else
        {
            virtualPhone.SetActive(false);
        }
    }
}