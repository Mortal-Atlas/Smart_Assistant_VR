using UnityEngine;
using System.Collections;
using UnityEngine.InputSystem; 

[RequireComponent(typeof(LineRenderer))]
public class FishingController : MonoBehaviour
{
    public enum FishingState 
    { 
        Idle, 
        Casting, 
        LureInWater, 
        Nibbling, 
        Hooked, 
        LineSnapped 
    }

    [Header("Setup References")]
    [Tooltip("Drag the 'Line point' empty GameObject here.")]
    public Transform linePoint; 
    [Tooltip("The layer of your reflective floor/water.")]
    public LayerMask waterFloorLayer;
    [Tooltip("Drag your downloaded Splash VFX Prefab here.")]
    public GameObject splashPrefab;

    [Header("Line Visuals")]
    [Range(5, 50)]
    public int lineResolution = 20; // How smooth the curve is
    public float lineDroopAmount = 1.5f; // How much gravity sags the line
    public float castDistance = 10f; // Max distance the line can cast
    
    [Header("Tension Settings")]
    [Range(0, 100)]
    public float currentTension = 0f;
    public float maxDurability = 100f;
    
    // Tension Colors
    private Color safeColor = Color.white;
    private Color lightTensionColor = Color.green;
    private Color warningColor = Color.yellow;
    private Color dangerColor = Color.red;
    private Color snapColor = Color.black;

    private LineRenderer lineRenderer;
    private FishingState currentState = FishingState.Idle;
    private Vector3 targetLurePosition; // Where the hook lands
    private bool isFlashingRedBlack = false;

    void Start()
    {
        // Grab the LineRenderer attached to this object
        lineRenderer = GetComponent<LineRenderer>();
        
        // Basic LineRenderer aesthetics
        lineRenderer.startWidth = 0.02f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.positionCount = lineResolution;
        lineRenderer.enabled = false; // Hide line initially
    }

    void Update()
    {
        // For testing purposes on PC before strapping on the headset
        if (Keyboard.current != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame && currentState == FishingState.Idle)
            {
                CastLine();
            }

            // Press 'R' to reel in / simulate tension
            if (Keyboard.current.rKey.isPressed && currentState == FishingState.Hooked)
            {
                currentTension += Time.deltaTime * 30f; // Rapidly increase tension
            }
            else if (currentState == FishingState.Hooked)
            {
                currentTension -= Time.deltaTime * 20f; // Slack regenerates durability
                currentTension = Mathf.Max(0, currentTension);
            }
        }

        // Always draw the line if it's out
        if (currentState != FishingState.Idle && currentState != FishingState.LineSnapped)
        {
            DrawDroopingLine();
        }

        // Handle tension logic if we are actively hooked
        if (currentState == FishingState.Hooked)
        {
            HandleTensionVisuals();
        }
    }

    /// <summary>
    /// Call this method from your Hand Tracking "Pinch" or "Throw" gesture.
    /// </summary>
    public void CastLine()
    {
        // Shoot a raycast diagonally down from the line point
        Vector3 castDirection = (linePoint.forward + Vector3.down * 0.5f).normalized;
        
        if (Physics.Raycast(linePoint.position, castDirection, out RaycastHit hit, castDistance, waterFloorLayer))
        {
            targetLurePosition = hit.point;
            currentState = FishingState.LureInWater;
            lineRenderer.enabled = true;
            
            // Look for nearby fish shadows within a 5 meter radius
            Collider[] hitColliders = Physics.OverlapSphere(targetLurePosition, 5f);
            bool fishFound = false;
            
            foreach (var hitCollider in hitColliders)
            {
                FishShadow fish = hitCollider.GetComponent<FishShadow>();
                if (fish != null)
                {
                    // Create a temporary empty object at the lure position for the fish to track
                    GameObject tempLure = new GameObject("TempLureTarget");
                    tempLure.transform.position = targetLurePosition;
                    
                    fish.Investigate(tempLure.transform);
                    fishFound = true;
                    
                    // Start the realistic bite sequence
                    StartCoroutine(BiteSequence(tempLure));
                    break;
                }
            }

            if (!fishFound)
            {
                Debug.Log("No fish nearby! Re-cast.");
                // Fallback for testing if no fish are in the scene
                Invoke(nameof(SimulateFishBite), 2f);
            }
        }
        else
        {
            Debug.Log("Cast failed: Didn't hit the floor layer.");
        }
    }

    /// <summary>
    /// Draws a Quadratic Bezier curve between the pole tip and the water.
    /// </summary>
    private void DrawDroopingLine()
    {
        if (lineRenderer == null || linePoint == null) return;

        // P0: Start at the pole tip
        Vector3 p0 = linePoint.position; 
        
        // P2: End at the water hit point
        Vector3 p2 = targetLurePosition; 
        
        // P1: Control point for the droop (middle point, pushed downward)
        Vector3 midPoint = Vector3.Lerp(p0, p2, 0.5f);
        // We push it down by 'lineDroopAmount' on the Y axis to simulate gravity
        Vector3 p1 = new Vector3(midPoint.x, Mathf.Min(p0.y, p2.y) - lineDroopAmount, midPoint.z);

        // Plot the points along the curve
        for (int i = 0; i < lineResolution; i++)
        {
            float t = i / (float)(lineResolution - 1);
            
            // Bezier formula
            Vector3 position = Mathf.Pow(1 - t, 2) * p0 + 
                               2 * (1 - t) * t * p1 + 
                               Mathf.Pow(t, 2) * p2;
            
            lineRenderer.SetPosition(i, position);
        }
    }

    private IEnumerator BiteSequence(GameObject tempLure)
    {
        currentState = FishingState.Nibbling;
        
        // Wait 2 seconds for the fish to swim over to the lure
        yield return new WaitForSeconds(2f);
        
        Debug.Log("Nibbling...");
        // You can trigger small controller haptics or line twitches here in the future
        
        // Wait for the final bite
        yield return new WaitForSeconds(1.5f);

        // Cleanup the temporary target and hook the fish
        Destroy(tempLure);
        SimulateFishBite();
    }

    private void SimulateFishBite()
    {
        currentState = FishingState.Hooked;
        Debug.Log("Fish Hooked! Tension game starting.");
        
        // Play Splash VFX from your downloaded pack
        if (splashPrefab != null)
        {
            Instantiate(splashPrefab, targetLurePosition, Quaternion.identity);
        }
    }

    private void HandleTensionVisuals()
    {
        Color currentLineColor = safeColor;

        // Map the 0-100 float to our specific colors
        if (currentTension < 20f) currentLineColor = safeColor;
        else if (currentTension < 50f) currentLineColor = lightTensionColor;
        else if (currentTension < 75f) currentLineColor = warningColor;
        else if (currentTension < 90f) currentLineColor = dangerColor;
        else if (currentTension < 100f) 
        {
            // Flash between Red and Black rapidly if in the 90-99 range
            currentLineColor = Mathf.PingPong(Time.time * 10f, 1f) > 0.5f ? dangerColor : snapColor;
        }
        else
        {
            // Snap the line!
            SnapLine();
            return;
        }

        // Apply the color to the line
        lineRenderer.startColor = currentLineColor;
        lineRenderer.endColor = currentLineColor;
    }

    private void SnapLine()
    {
        currentState = FishingState.LineSnapped;
        lineRenderer.enabled = false;
        currentTension = 0f;
        Debug.Log("Line Snapped! The fish got away.");
        
        // Play snap SFX here
        
        // Reset to idle after a moment
        Invoke(nameof(ResetToIdle), 2f);
    }

    private void ResetToIdle()
    {
        currentState = FishingState.Idle;
    }
}