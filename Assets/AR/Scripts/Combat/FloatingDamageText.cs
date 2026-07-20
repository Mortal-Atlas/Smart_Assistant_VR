using UnityEngine;
using TMPro;

public class FloatingDamageText : MonoBehaviour
{
    [Header("Animation Settings")]
    public float moveSpeed = 0.5f;
    public float destroyTime = 1.5f;
    
    private TMP_Text damageText;
    private Color textColor;
    private float timer;

    void Awake()
    {
        damageText = GetComponentInChildren<TMP_Text>();
        
        if (damageText != null)
        {
            // Store the initial color so we can manipulate its alpha
            textColor = damageText.color;
        }

        // Automatically destroy this popup after a set time
        Destroy(gameObject, destroyTime);
    }

    public void Setup(float damageAmount)
    {
        if (damageText != null)
        {
            damageText.text = Mathf.RoundToInt(damageAmount).ToString();
        }
    }

    void Update()
    {
        // 1. Drift upwards
        transform.position += Vector3.up * moveSpeed * Time.deltaTime;

        // 2. Fade out alpha smoothly
        if (damageText != null)
        {
            timer += Time.deltaTime;
            // Calculate percentage of life lived, map it from 1 to 0
            float alpha = Mathf.Lerp(1f, 0f, timer / destroyTime);
            
            // Apply the new alpha
            textColor.a = alpha;
            damageText.color = textColor;
        }
    }
}