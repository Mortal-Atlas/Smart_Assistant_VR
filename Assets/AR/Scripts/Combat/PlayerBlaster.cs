using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PlayerBlaster : MonoBehaviour
{
    [Header("Input")]
    [Tooltip("Bind this to your Left Controller Trigger")]
    public InputActionReference shootAction;

    [Header("Gun Settings")]
    public Transform firePoint;
    public float fireRate = 0.15f; 
    public float maxHeat = 10f;
    public float heatPerShot = 1f;
    public float coolingRate = 3.5f; // How fast heat drains per second

    [Header("Visuals & UI")]
    public GameObject laserProjectilePrefab; 
    [Tooltip("Drag your new Radial Fill Image here")]
    public Image overheatRadialFill;
    public Color normalColor = Color.yellow;
    public Color overheatColor = Color.red;

    private float currentHeat = 0f;
    private bool isOverheated = false;
    private float nextFireTime = 0f;

    void OnEnable()
    {
        if (shootAction != null) shootAction.action.Enable();
    }

    void OnDisable()
    {
        if (shootAction != null) shootAction.action.Disable();
    }

    void Update()
    {
        HandleCooling();
        UpdateUI();
        HandleShooting();
    }

    private void HandleCooling()
    {
        // If we are overheated, we must wait for heat to reach exactly 0 before shooting again
        if (currentHeat > 0)
        {
            currentHeat -= coolingRate * Time.deltaTime;
            
            if (currentHeat <= 0)
            {
                currentHeat = 0;
                isOverheated = false; 
            }
        }
    }

    private void HandleShooting()
    {
        if (shootAction == null || isOverheated) return;

        // Continuous fire if the trigger is held down
        if (shootAction.action.IsPressed() && Time.time >= nextFireTime)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        nextFireTime = Time.time + fireRate;
        currentHeat += heatPerShot;

        // Fire the projectile
        if (laserProjectilePrefab != null && firePoint != null)
        {
            GameObject laser = Instantiate(laserProjectilePrefab, firePoint.position, firePoint.rotation);
            // Assuming your laser prefab has a script that moves it forward!
        }
        else
        {
            Debug.Log("PEW! (No projectile prefab assigned)");
        }

        // Check for Overheat
        if (currentHeat >= maxHeat)
        {
            currentHeat = maxHeat;
            isOverheated = true;
            Debug.Log("BLASTER OVERHEATED!");
        }
    }

    private void UpdateUI()
    {
        if (overheatRadialFill != null)
        {
            overheatRadialFill.fillAmount = currentHeat / maxHeat;
            overheatRadialFill.color = isOverheated ? overheatColor : normalColor;
        }
    }
}