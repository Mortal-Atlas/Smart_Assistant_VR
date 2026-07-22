using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    [Header("Input Bindings (Right Controller)")]
    public InputActionReference thumbstickClick;
    public InputActionReference thumbstickAxis;

    [Header("App Canvases")]
    public GameObject spellCanvas;
    public GameObject spotifyCanvas;
    public GameObject scannerCanvas;
    public GameObject combatCanvas;

    [Header("Margo Summoning")]
    [Tooltip("The 3D model of Margo to materialize in your room")]
    public GameObject margoModel;
    [Tooltip("Fires when the thumbstick is clicked and held for 0.5s")]
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
        if (margoModel != null) margoModel.SetActive(false); // Hide Margo on boot
    }

    private void Update()
    {
        if (isThumbstickPressed)
        {
            Vector2 axisValue = thumbstickAxis.action.ReadValue<Vector2>();

            // 1. RADIAL MENU HOVER
            if (axisValue.magnitude > 0.6f)
            {
                DetermineRadialApp(axisValue);
                appSwitchedDuringPress = true; 
            }
            // 2. MARGO SUMMON TIMER (If holding still)
            else if (!appSwitchedDuringPress && !margoSummonedThisPress)
            {
                pressTimer += Time.deltaTime;
                
                // If held for 0.5 seconds with no direction push, summon her!
                if (pressTimer >= 0.5f)
                {
                    SummonMargo();
                    margoSummonedThisPress = true;
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

        if (Mathf.Abs(axis.y) > Mathf.Abs(axis.x))
        {
            if (axis.y > 0) SwitchApp(spellCanvas);     // UP = Spells
            else SwitchApp(combatCanvas);               // DOWN = Combat HUD
        }
        else
        {
            if (axis.x > 0) SwitchApp(scannerCanvas);   // RIGHT = Scanner
            else SwitchApp(spotifyCanvas);              // LEFT = Spotify
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
        if (spellCanvas != null) spellCanvas.SetActive(false);
        if (spotifyCanvas != null) spotifyCanvas.SetActive(false);
        if (scannerCanvas != null) scannerCanvas.SetActive(false);
        if (combatCanvas != null) combatCanvas.SetActive(false);
    }
}