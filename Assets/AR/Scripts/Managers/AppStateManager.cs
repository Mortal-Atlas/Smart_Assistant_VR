using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    [Header("Input Bindings (Right Controller)")]
    [Tooltip("Bind to XRI RightHand/Thumbstick Press")]
    public InputActionReference thumbstickClick;
    [Tooltip("Bind to XRI RightHand/Primary2DAxis")]
    public InputActionReference thumbstickAxis;

    [Header("App Canvases")]
    public GameObject spellCanvas;
    public GameObject spotifyCanvas;
    public GameObject scannerCanvas;
    public GameObject combatCanvas;

    [Header("Margo Voice Activation")]
    [Tooltip("Fires when the thumbstick is clicked without being pushed in a direction.")]
    public UnityEvent OnMargoActivated;

    // Internal state tracking for the radial menu
    private bool isThumbstickPressed = false;
    private bool appSwitchedDuringPress = false;
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

        if (thumbstickAxis != null)
        {
            thumbstickAxis.action.Enable();
        }
    }

    private void OnDisable()
    {
        if (thumbstickClick != null)
        {
            thumbstickClick.action.started -= OnThumbstickPress;
            thumbstickClick.action.canceled -= OnThumbstickRelease;
            thumbstickClick.action.Disable();
        }

        if (thumbstickAxis != null)
        {
            thumbstickAxis.action.Disable();
        }
    }

    private void Start()
    {
        // Hide all apps on boot
        HideAllApps();
    }

    private void Update()
    {
        // If the user is holding down the thumbstick, read the axis for radial selection
        if (isThumbstickPressed && !appSwitchedDuringPress)
        {
            Vector2 axisValue = thumbstickAxis.action.ReadValue<Vector2>();

            // If pushed far enough in a direction (deadzone of 0.6)
            if (axisValue.magnitude > 0.6f)
            {
                DetermineRadialApp(axisValue);
                appSwitchedDuringPress = true; // Lock in the choice until released
            }
        }
    }

    private void OnThumbstickPress(InputAction.CallbackContext context)
    {
        isThumbstickPressed = true;
        appSwitchedDuringPress = false;
    }

    private void OnThumbstickRelease(InputAction.CallbackContext context)
    {
        isThumbstickPressed = false;

        // If we just clicked and released without pushing a direction, Talk to Margo!
        if (!appSwitchedDuringPress)
        {
            Debug.Log("[AppStateManager] Margo Voice Activated!");
            OnMargoActivated?.Invoke();
            // You can link your PhoneElevenLabsTTS or SttToggle to this UnityEvent in the Inspector!
        }
    }

    private void DetermineRadialApp(Vector2 axis)
    {
        HideAllApps();

        // Calculate angle or use simple absolute values to determine quadrant
        if (Mathf.Abs(axis.y) > Mathf.Abs(axis.x))
        {
            // Vertical movement
            if (axis.y > 0) SwitchApp(spellCanvas);     // UP = Spells
            else SwitchApp(combatCanvas);               // DOWN = Combat HUD
        }
        else
        {
            // Horizontal movement
            if (axis.x > 0) SwitchApp(scannerCanvas);   // RIGHT = Scanner
            else SwitchApp(spotifyCanvas);              // LEFT = Spotify
        }
    }

    private void SwitchApp(GameObject appCanvas)
    {
        if (appCanvas == null) return;
        
        currentActiveApp = appCanvas;
        currentActiveApp.SetActive(true);
        Debug.Log($"[AppStateManager] Switched to App: {appCanvas.name}");
    }

    private void HideAllApps()
    {
        if (spellCanvas != null) spellCanvas.SetActive(false);
        if (spotifyCanvas != null) spotifyCanvas.SetActive(false);
        if (scannerCanvas != null) scannerCanvas.SetActive(false);
        if (combatCanvas != null) combatCanvas.SetActive(false);
    }
}