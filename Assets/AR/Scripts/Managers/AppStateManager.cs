using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class AppStateManager : MonoBehaviour
{
    public static AppStateManager Instance { get; private set; }

    [Header("Input Bindings")]
    [Tooltip("Pushing the RIGHT thumbstick to open the radial dial")]
    public InputActionReference thumbstickAxis;
    
    [Tooltip("Pressing the LEFT Menu (Options) button to summon Margo")]
    public InputActionReference summonMargoAction; 

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
    [Tooltip("The Canvas holding your Radial Dial background or App Selector Menu")]
    public GameObject appSwapMenuCanvas;
    public UnityEvent OnMargoActivated;
    
    private GameObject currentActiveApp;
    private bool isDialActive = false;
    private bool isMenuOpen = false; // NEW: Tracks whether the system menu is on or off

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;
    }

    private void OnEnable()
    {
        if (thumbstickAxis != null) thumbstickAxis.action.Enable();
        
        if (summonMargoAction != null)
        {
            summonMargoAction.action.Enable();
            // Instantly summon Margo the moment the button is pressed
            summonMargoAction.action.performed += SummonMargo;
        }
    }

    private void OnDisable()
    {
        if (thumbstickAxis != null) thumbstickAxis.action.Disable();
        
        if (summonMargoAction != null)
        {
            summonMargoAction.action.Disable();
            summonMargoAction.action.performed -= SummonMargo;
        }
    }

    private void Start()
    {
        HideAllApps();
        ClearHighlights();
        if (margoModel != null) margoModel.SetActive(false); 
        if (appSwapMenuCanvas != null) appSwapMenuCanvas.SetActive(false);
    }

    private void Update()
    {
        if (thumbstickAxis == null) return;

        Vector2 axis = thumbstickAxis.action.ReadValue<Vector2>();

        // 1. If pushing the stick far enough, activate Dial Mode
        if (axis.magnitude > 0.5f)
        {
            isDialActive = true;
            DetermineRadialApp(axis);
        }
        // 2. If letting go of the stick (returning to center), lock it in!
        else if (isDialActive && axis.magnitude < 0.2f)
        {
            isDialActive = false;
            ClearHighlights();
            // Notice we DO NOT hide the app here! It stays securely open.
        }
    }

    private void SummonMargo(InputAction.CallbackContext context)
    {
        isMenuOpen = !isMenuOpen; // Toggle the state
        Debug.Log($"[AppStateManager] Options Button Pressed! System Menu is now {(isMenuOpen ? "OPEN" : "CLOSED")}.");
        
        // 1. Toggle Margo
        if (margoModel != null && Camera.main != null)
        {
            margoModel.SetActive(isMenuOpen);
            
            if (isMenuOpen)
            {
                Transform cam = Camera.main.transform;
                Vector3 spawnPos = cam.position + (cam.forward * 1.5f);
                spawnPos.y = cam.position.y - 0.4f; 
                
                margoModel.transform.position = spawnPos;

                Vector3 lookPos = cam.position;
                lookPos.y = margoModel.transform.position.y; 
                margoModel.transform.LookAt(lookPos);
            }
        }

        // 2. Toggle the App Swap Menu
        if (appSwapMenuCanvas != null)
        {
            appSwapMenuCanvas.SetActive(isMenuOpen);
            
            // Note: If your App Swap Menu is a World Space canvas, you might want to position 
            // it right next to Margo here. If it's attached to your wrist or camera, 
            // simply turning it on is enough!
        }

        if (isMenuOpen)
        {
            OnMargoActivated?.Invoke();
        }
    }

    private void DetermineRadialApp(Vector2 axis)
    {
        // Calculate angle from 0 to 360 degrees
        float angle = Mathf.Atan2(axis.y, axis.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        // Divide into 8 slices
        int slice = Mathf.RoundToInt(angle / 45f) % 8;

        GameObject targetApp = null;
        GameObject targetHighlight = null;

        switch (slice)
        {
            case 0: targetHighlight = highlightRight; targetApp = appRight; break;
            case 1: targetHighlight = highlightRightForward; targetApp = appRightForward; break;
            case 2: targetHighlight = highlightForward; targetApp = appForward; break;
            case 3: targetHighlight = highlightLeftForward; targetApp = appLeftForward; break;
            case 4: targetHighlight = highlightLeft; targetApp = appLeft; break;
            case 5: targetHighlight = highlightLeftBack; targetApp = appLeftBack; break;
            case 6: targetHighlight = highlightBack; targetApp = appBack; break;
            case 7: targetHighlight = highlightRightBack; targetApp = appRightBack; break;
        }

        // Show the UI highlight so the player knows what they are hovering over
        ClearHighlights();
        if (targetHighlight != null) targetHighlight.SetActive(true);

        // Preview the app instantly if one is slotted there
        if (targetApp != null && targetApp != currentActiveApp)
        {
            SwitchApp(targetApp);
        }
    }

    private void SwitchApp(GameObject appCanvas)
    {
        if (appCanvas == null) return;
        HideAllApps();
        currentActiveApp = appCanvas;
        currentActiveApp.SetActive(true);
    }

    // NEW: This is the missing method that Node-RED / MQTT uses to open apps via voice!
    public void SwitchApp(string appName)
    {
        appName = appName.ToLower();
        GameObject foundApp = null;

        if (appForward != null && appForward.name.ToLower() == appName) foundApp = appForward;
        else if (appRightForward != null && appRightForward.name.ToLower() == appName) foundApp = appRightForward;
        else if (appRight != null && appRight.name.ToLower() == appName) foundApp = appRight;
        else if (appRightBack != null && appRightBack.name.ToLower() == appName) foundApp = appRightBack;
        else if (appBack != null && appBack.name.ToLower() == appName) foundApp = appBack;
        else if (appLeftBack != null && appLeftBack.name.ToLower() == appName) foundApp = appLeftBack;
        else if (appLeft != null && appLeft.name.ToLower() == appName) foundApp = appLeft;
        else if (appLeftForward != null && appLeftForward.name.ToLower() == appName) foundApp = appLeftForward;

        if (foundApp != null)
        {
            Debug.Log($"[AppStateManager] Opening {foundApp.name} via MQTT command");
            SwitchApp(foundApp);
        }
        else
        {
            Debug.LogWarning($"[AppStateManager] MQTT tried to open app '{appName}', but it wasn't found in the 8 slots!");
        }
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