using UnityEngine;
using UnityEngine.UI;

public class ZTargetReticle : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your FamiliarAI script GameObject here")]
    public FamiliarAI familiarAI;
    
    [Tooltip("The RectTransform of your Ocarina of Time targeting graphic/image")]
    public RectTransform reticleVisual;

    [Header("Animation Settings")]
    [Tooltip("Time in seconds for the reticle to pop in from small to full size (Ocarina style)")]
    public float popInDuration = 0.25f;
    [Tooltip("Rotation speed in degrees per second (Negative for clockwise rotation)")]
    public float rotationSpeed = 180f;
    [Tooltip("Vertical offset above the enemy's center position")]
    public float heightOffset = 1.8f;

    private float timer = 0f;
    private Transform cachedTarget;
    private CanvasGroup canvasGroup;

    void Start()
    {
        if (familiarAI == null)
        {
            familiarAI = Object.FindFirstObjectByType<FamiliarAI>();
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // Hide reticle on start
        SetReticleActive(false);
    }

    void Update()
    {
        if (familiarAI == null) return;

        // Get the active lock-on target from FamiliarAI
        Transform target = familiarAI.CurrentTarget;

        if (target != null)
        {
            // If we just locked onto a new target, trigger the 0.25s pop-in animation
            if (cachedTarget != target)
            {
                cachedTarget = target;
                timer = 0f;
                SetReticleActive(true);
            }

            // 1. Follow target position with height offset
            transform.position = target.position + Vector3.up * heightOffset;

            // 2. Billboard: Make the reticle always face the VR Camera
            if (Camera.main != null)
            {
                transform.rotation = Camera.main.transform.rotation;
            }

            // 3. Pop-in scale animation (0.25s from 20% scale to 100% scale)
            if (timer < popInDuration)
            {
                timer += Time.deltaTime;
                float progress = Mathf.Clamp01(timer / popInDuration);
                
                // SmoothStep creates that classic Zelda snappy pop-in feel
                float scale = Mathf.SmoothStep(0.2f, 1.0f, progress);
                if (reticleVisual != null)
                {
                    reticleVisual.localScale = Vector3.one * scale;
                }
                canvasGroup.alpha = progress;
            }
            else
            {
                // 4. Continuous clockwise rotation once fully expanded
                if (reticleVisual != null)
                {
                    reticleVisual.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
                }
            }
        }
        else
        {
            // No target locked, hide reticle
            if (cachedTarget != null)
            {
                cachedTarget = null;
                SetReticleActive(false);
            }
        }
    }

    private void SetReticleActive(bool active)
    {
        if (reticleVisual != null)
        {
            reticleVisual.gameObject.SetActive(active);
        }
        if (canvasGroup != null)
        {
            canvasGroup.alpha = active ? 1f : 0f;
        }
    }
}