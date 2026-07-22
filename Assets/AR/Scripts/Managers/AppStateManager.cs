using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    [Header("Input Bindings (Right Controller)")]
    public InputActionReference thumbstickClick;
    public InputActionReference thumbstickAxis;

    [Header("App Canvases (8-Way Directional)")]
    public GameObject appForward;        // UP
    public GameObject appRightForward;   // UP-RIGHT
    public GameObject appRight;          // RIGHT
    public GameObject appRightBack;      // DOWN-RIGHT
    public GameObject appBack;           // DOWN
    public GameObject appLeftBack;       // DOWN-LEFT
    public GameObject appLeft;           // LEFT
    public GameObject appLeftForward;    // UP-LEFT

    [Header("Radial Highlights (8-Way Directional)")]
    public GameObject highlightForward;
    public GameObject highlightRightForward;
    public GameObject highlightRight;
    public GameObject highlightRightBack;
    public GameObject highlightBack;
    public GameObject highlightLeftBack;
    public GameObject highlightLeft;
    public GameObject highlightLeftForward;

    [Header("Margo Summoning")]
    [Tooltip("The 3D model of Margo to materialize in your room")]
    public GameObject margoModel;
    [Tooltip("Fires when the thumbstick is clicked and held for 0.3s")]
    public UnityEvent OnMargoActivated;
    
    private float pressTimer = 0f;
    private bool isThumbstickPressed = false;
    private bool appSwitchedDuringPress = false;
    private bool margoSummonedThisPress = false;
    private GameObject currentActiveApp;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        if (thumbstickClick != null)
        {
            thumbstickClick.action.started += OnThumbstickPress;
            thumbstickClick.action.canceled += OnThumbstickRelease;
            thumbstickClick.action.Enable();
        }

        if (thumbstickAxis != null) thumbstickAxis.action.Enable();
    }

    private void OnDisable()
    {
        if (thumbstickClick != null)
        {
            thumbstickClick.action.started -= OnThumbstickPress;
            thumbstickClick.action.canceled -= OnThumbstickRelease;
            thumbstickClick.action.Disable();
        }

        if (thumbstickAxis != null) thumbstickAxis.action.Disable();
    }

    private void Start()
    {
        HideAllApps();
        ClearHighlights();
        if (margoModel != null) margoModel.SetActive(false); // Hide Margo on boot
    }

    private void Update()
    {
        if (isThumbstickPressed)
        {
            Vector2 axisValue = thumbstickAxis.action.ReadValue<Vector2>();

            // 1. RADIAL MENU HOVER
            if (axisValue.magnitude > 0.5f)
            {
                DetermineRadialApp(axisValue);
                appSwitchedDuringPress = true; 
            }
            else
            {
                // Clear highlights if resting in the middle
                ClearHighlights();

                // 2. MARGO SUMMON TIMER (If holding still)
                if (!appSwitchedDuringPress && !margoSummonedThisPress)
                {
                    pressTimer += Time.deltaTime;
                    
                    // Reduced to 0.3s so it feels much more responsive!
                    if (pressTimer >= 0.3f)
                    {
                        SummonMargo();
                        margoSummonedThisPress = true;
                    }
                }
            }
        }
    }

    private void OnThumbstickPress(InputAction.CallbackContext context)
    {
        isThumbstickPressed = true;
        appSwitchedDuringPress = false;
        margoSummonedThisPress = false;
        pressTimer = 0f;
    }

    private void OnThumbstickRelease(InputAction.CallbackContext context)
    {
        isThumbstickPressed = false;
        ClearHighlights(); // Turn off the hover visuals when you let go
    }

    private void SummonMargo()
    {
        Debug.Log("[AppStateManager] Margo Materialized!");
        
        if (margoModel != null && Camera.main != null)
        {
            // Turn her on
            margoModel.SetActive(true);
            
            // Calculate a spot 1.5 meters exactly in front of the VR headset
            Transform cam = Camera.main.transform;
            Vector3 spawnPos = cam.position + (cam.forward * 1.5f);
            spawnPos.y = cam.position.y - 0.4f; // Drop her slightly so she isn't floating at eye level
            
            margoModel.transform.position = spawnPos;

            // Make her look at you
            Vector3 lookPos = cam.position;
            lookPos.y = margoModel.transform.position.y; // Keep her standing straight
            margoModel.transform.LookAt(lookPos);
        }

        // Trigger the AI voice listener
        OnMargoActivated?.Invoke();
    }

    private void DetermineRadialApp(Vector2 axis)
    {
        HideAllApps();
        ClearHighlights();

        // 1. Calculate angle from -180 to 180 degrees
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        
        // 2. Normalize angle to 0-360 for easier division
        if (angle < 0) angle += 360f;

        // 3. Divide 360 degrees into 8 slices of 45 degrees each.
        // Adding 22.5 to the offset ensures the slices are perfectly centered on the joystick axes.
        int slice = Mathf.RoundToInt(angle / 45f) % 8;

        switch (slice)
        {
            case 0: // RIGHT
                if (highlightRight != null) highlightRight.SetActive(true);
                SwitchApp(appRight);
                break;
            case 1: // UP-RIGHT (Right Forward)
                if (highlightRightForward != null) highlightRightForward.SetActive(true);
                SwitchApp(appRightForward);
                break;
            case 2: // UP (Forward)
                if (highlightForward != null) highlightForward.SetActive(true);
                SwitchApp(appForward);
                break;
            case 3: // UP-LEFT (Left Forward)
                if (highlightLeftForward != null) highlightLeftForward.SetActive(true);
                SwitchApp(appLeftForward);
                break;
            case 4: // LEFT
                if (highlightLeft != null) highlightLeft.SetActive(true);
                SwitchApp(appLeft);
                break;
            case 5: // DOWN-LEFT (Left Back)
                if (highlightLeftBack != null) highlightLeftBack.SetActive(true);
                SwitchApp(appLeftBack);
                break;
            case 6: // DOWN (Back)
                if (highlightBack != null) highlightBack.SetActive(true);
                SwitchApp(appBack);
                break;
            case 7: // DOWN-RIGHT (Right Back)
                if (highlightRightBack != null) highlightRightBack.SetActive(true);
                SwitchApp(appRightBack);
                break;
        }
    }

    private void SwitchApp(GameObject appCanvas)
    {
        if (appCanvas == null) return;
        currentActiveApp = appCanvas;
        currentActiveApp.SetActive(true);
    }

    private void HideAllApps()
    {
        if (appForward != null) appForward.SetActive(false);
        if (appRightForward != null) appRightForward.SetActive(false);
        if (appRight != null) appRight.SetActive(false);
        if (appRightBack != null) appRightBack.SetActive(false);
        if (appBack != null) appBack.SetActive(false);
        if (appLeftBack != null) appLeftBack.SetActive(false);
        if (appLeft != null) appLeft.SetActive(false);
        if (appLeftForward != null) appLeftForward.SetActive(false);
    }

    private void ClearHighlights()
    {
        if (highlightForward != null) highlightForward.SetActive(false);
        if (highlightRightForward != null) highlightRightForward.SetActive(false);
        if (highlightRight != null) highlightRight.SetActive(false);
        if (highlightRightBack != null) highlightRightBack.SetActive(false);
        if (highlightBack != null) highlightBack.SetActive(false);
        if (highlightLeftBack != null) highlightLeftBack.SetActive(false);
        if (highlightLeft != null) highlightLeft.SetActive(false);
        if (highlightLeftForward != null) highlightLeftForward.SetActive(false);
    }
}