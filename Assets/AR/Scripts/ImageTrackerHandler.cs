using System.Collections.Generic;
using UnityEngine;
using Meta.XR.MRUtilityKit;

public class ImageTrackerHandler : MonoBehaviour
{
    [Header("Tracking Setup")]
    [Tooltip("The Virtual S24 Ultra Prefab to spawn over the physical phone.")]
    public GameObject virtualPhonePrefab;

    [Tooltip("The exact text embedded in the QR code. If empty, it tracks ANY QR code.")]
    public string targetQRPayload = "rika_s24_tracker";

    [Tooltip("Adjust this to nudge the virtual phone so it perfectly aligns with the real one.")]
    public Vector3 positionOffset = new Vector3(0, 0.05f, 0);

    [Tooltip("How smoothly the hologram follows the QR code. Higher = snappier, Lower = smoother but lags behind slightly.")]
    public float trackingSmoothSpeed = 15f;

    [Header("Sensor Fallback (MQTT)")]
    [Tooltip("If true, the phone stays visible when the QR code is covered, using streamed sensor data.")]
    public bool useSensorFallback = true;

    private GameObject spawnedPhone;

    // Fallback State
    private bool isQRVisible = false;
    private Quaternion latestSensorRotation = Quaternion.identity;
    private Vector3 latestSensorPosition = Vector3.zero;

    // Calibration offsets
    private Quaternion rotationOffset = Quaternion.identity;
    private Vector3 positionDriftOffset = Vector3.zero;

    // SmoothDamp velocity reference
    private Vector3 positionVelocity = Vector3.zero;

    // Trackables cache to prevent garbage collection allocations every frame
    private List<MRUKTrackable> _trackables = new List<MRUKTrackable>();

    void Start()
    {
        // Rika's Sanity Check: Ensure MRUK is actually in the scene
        if (MRUK.Instance == null)
        {
            Debug.LogError("💀 RIKA FATAL ERROR: MRUK Instance is missing! Add the MRUK prefab to your scene!");
            return;
        }

        Debug.Log("🦇 Rika says: Native MRUK Tracking is ALIVE! Scanning for QR Codes...");
    }

    void Update()
    {
        // Safety check to ensure MRUK is ready
        if (MRUK.Instance == null) return;

        // 1. Get all current trackables (QR codes, keyboards, etc.)
        MRUK.Instance.GetTrackables(_trackables);

        // 2. Find the first actively tracked QR code that matches our payload
        MRUKTrackable activeQRCode = null;
        foreach (var trackable in _trackables)
        {
            // We check IsTracked so it instantly registers if your hand covers it
            if (trackable.TrackableType == OVRAnchor.TrackableType.QRCode && trackable.IsTracked)
            {
                // If we set a specific target payload, verify it matches before accepting it
                if (!string.IsNullOrEmpty(targetQRPayload))
                {
                    // Note: Depending on your specific Meta SDK version, MarkerPayloadString might be located 
                    // inside the Anchor directly. But MRUK usually passes it through.
                    if (trackable.MarkerPayloadString != targetQRPayload)
                    {
                        continue; // This is a random QR code, ignore it and keep searching!
                    }
                }

                activeQRCode = trackable;
                break;
            }
        }

        // 3. Handle State Changes based on what we found
        if (activeQRCode != null)
        {
            bool justLocked = false;
            if (!isQRVisible)
            {
                Debug.Log($"🦇 Rika says: QR Code Locked! Payload: {activeQRCode.MarkerPayloadString}");
                if (spawnedPhone == null)
                {
                    // Spawn the virtual phone if it doesn't exist yet
                    spawnedPhone = Instantiate(virtualPhonePrefab);
                }
                
                spawnedPhone.SetActive(true);
                isQRVisible = true;
                justLocked = true; // Flag to snap instantly on the first frame
            }

            // 4. Continuously update Transform from the QR code
            UpdatePhoneFromQR(activeQRCode, justLocked);
        }
        else
        {
            // If the QR code was visible but now we lost it
            if (isQRVisible)
            {
                isQRVisible = false;

                if (useSensorFallback)
                {
                    Debug.Log("🦇 Rika says: QR Code covered! Switching to IMU Sensor Fallback...");
                }
            }
        }
    }
    private void UpdatePhoneFromQR(MRUKTrackable trackable, bool snapInstantly)
    {
        // Calculate absolute room positions from QR code
        Vector3 targetPosition = trackable.transform.position + (trackable.transform.rotation * positionOffset);
        Quaternion targetRotation = trackable.transform.rotation;

        // Apply to the virtual phone
        if (spawnedPhone != null)
        {
            if (snapInstantly)
            {
                // Instantly snap on the very first frame so it doesn't fly across the room
                spawnedPhone.transform.position = targetPosition;
                spawnedPhone.transform.rotation = targetRotation;
            }
            else
            {
                // SmoothDamp acts like a physical spring, hiding the low 5Hz update rate of the QR scanner much better than Lerp
                spawnedPhone.transform.position = Vector3.SmoothDamp(spawnedPhone.transform.position, targetPosition, ref positionVelocity, 0.15f);
                spawnedPhone.transform.rotation = Quaternion.Slerp(spawnedPhone.transform.rotation, targetRotation, Time.deltaTime * trackingSmoothSpeed);
            }
        }

        // Constantly recalibrate the sensor offset while the QR code is visible
        // We use the TARGET position/rotation here to keep the calibration mathematically pure
        rotationOffset = targetRotation * Quaternion.Inverse(latestSensorRotation);
        positionDriftOffset = targetPosition - (rotationOffset * latestSensorPosition);
    }

    // --- CALLED BY YOUR MQTT BRIDGE SCRIPT ---
    /// <summary>
    /// Feed the live phone gyro/AR data into this method over MQTT!
    /// </summary>
    private Vector3 targetPos;
    private Quaternion targetRot;

    public void UpdateFallbackSensorData(Vector3 phoneSpatialPosition, Quaternion phoneTiltRotation)
    {
        latestSensorPosition = phoneSpatialPosition;
        latestSensorRotation = phoneTiltRotation;

        if (!isQRVisible && spawnedPhone != null && useSensorFallback)
        {
            // Calculate where it SHOULD be
            targetRot = rotationOffset * latestSensorRotation;
            targetPos = (rotationOffset * latestSensorPosition) + positionDriftOffset;
            
            // LERP towards it for buttery smoothness (hides the network jitter!)
            spawnedPhone.transform.rotation = Quaternion.Slerp(spawnedPhone.transform.rotation, targetRot, Time.deltaTime * 25f);
            spawnedPhone.transform.position = Vector3.Lerp(spawnedPhone.transform.position, targetPos, Time.deltaTime * 25f);
        }
    }
}