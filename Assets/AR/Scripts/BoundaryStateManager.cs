using UnityEngine;
using System;

public class BoundaryStateManager : MonoBehaviour
{
    public enum BoundaryMode { Travel, Home }
    
    [Tooltip("Defaulting to Travel assumes we don't know where we are yet.")]
    public BoundaryMode currentMode = BoundaryMode.Travel; 

    // Events that other scripts (like your UI or Boundary renderers) can listen to
    public event Action OnHomeModeActivated;
    public event Action OnTravelModeActivated;

    [Header("Boundary Systems")]
    [SerializeField] private GameObject travelBoundarySystem; // The dynamic mesh logic
    [SerializeField] private GameObject homeBoundarySystem;   // Your finished Home Mode logic

    void Start()
    {
        // Force the initial state on boot
        TransitionToMode(currentMode);
    }

    // Call this method whenever a QR code is scanned or MQTT payload is received
    public void TransitionToMode(BoundaryMode newMode)
    {
        if (currentMode == newMode) return; // Prevent redundant triggering

        currentMode = newMode;

        if (currentMode == BoundaryMode.Home)
        {
            // 1. Shut down heavy depth API / mesh polling
            travelBoundarySystem.SetActive(false);
            
            // 2. Turn on the lightweight static bounds
            homeBoundarySystem.SetActive(true);
            
            // 3. Tell the rest of the app we are home
            OnHomeModeActivated?.Invoke();
            
            Debug.Log("Switched to Home Mode: Static boundaries loaded.");
        }
        else
        {
            homeBoundarySystem.SetActive(false);
            travelBoundarySystem.SetActive(true);
            OnTravelModeActivated?.Invoke();
            
            Debug.Log("Switched to Travel Mode: Dynamic spatial mapping active.");
        }
    }
}