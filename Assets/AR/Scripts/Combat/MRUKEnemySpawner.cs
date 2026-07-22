using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit;
using UnityEngine.InputSystem; // NEW: Required for the toggle button

public class MRUKEnemySpawner : MonoBehaviour
{
    [Header("Controls")]
    [Tooltip("Bind this to a button (like the Left Menu button) to pause/resume waves")]
    public InputActionReference toggleSpawnAction;

    [Header("Enemy Prefabs")]
    [Tooltip("The standard skeleton")]
    public GameObject basicEnemyPrefab;
    [Tooltip("The rare skeleton variant")]
    public GameObject rareEnemyPrefab;
    [Tooltip("The elite skeleton boss")]
    public GameObject eliteEnemyPrefab;

    [Header("Hack & Slash Wave Settings")]
    [Tooltip("Maximum enemies allowed alive at once to prevent lag")]
    public int maxActiveEnemies = 12; 
    [Tooltip("How often a new wave attempts to spawn (in seconds)")]
    public float waveInterval = 6f;

    [Header("Spawn Probabilities (Out of 100%)")]
    [Tooltip("Chance to spawn a single Rare enemy")]
    public int rareSpawnChance = 20; 
    [Tooltip("Chance to spawn an Elite with 2 Basic guards")]
    public int eliteSpawnChance = 10; 
    // Basic groups will fill the remaining percentage (70% in this case)

    private bool isRoomLoaded = false;
    private bool isSpawningActive = true; // Controls the pause state

    void OnEnable()
    {
        if (toggleSpawnAction != null)
        {
            toggleSpawnAction.action.Enable();
            toggleSpawnAction.action.performed += OnToggleSpawning;
        }
    }

    void OnDisable()
    {
        if (toggleSpawnAction != null)
        {
            toggleSpawnAction.action.Disable();
            toggleSpawnAction.action.performed -= OnToggleSpawning;
        }
    }

    void Start()
    {
        // Wait for MRUK to build the physical room before starting the horde
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
        }
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        Debug.Log("[MRUK] Room loaded! Starting Hack & Slash Waves.");
        isRoomLoaded = true;
        StartCoroutine(SpawnWavesRoutine());
    }

    private IEnumerator SpawnWavesRoutine()
    {
        while (isRoomLoaded)
        {
            // Wait for the interval before checking if we should spawn more
            yield return new WaitForSeconds(waveInterval);

            // NEW: Skip this wave if the spawner is paused!
            if (!isSpawningActive) continue;

            // Find how many enemies are currently alive in the scene
            AREnemy[] activeEnemies = FindObjectsByType<AREnemy>(FindObjectsSortMode.None);
            
            // Only spawn a new wave if we have room for them
            if (activeEnemies.Length < maxActiveEnemies)
            {
                DetermineAndSpawnWave();
            }
        }
    }

    // NEW: The method called when you press your pause button
    private void OnToggleSpawning(InputAction.CallbackContext context)
    {
        isSpawningActive = !isSpawningActive;
        Debug.Log($"[Spawner] Spawning is now {(isSpawningActive ? "ACTIVE" : "PAUSED")}");
    }

    private void DetermineAndSpawnWave()
    {
        int roll = Random.Range(0, 100);

        if (roll < eliteSpawnChance)
        {
            SpawnEliteGroup();
        }
        else if (roll < eliteSpawnChance + rareSpawnChance)
        {
            SpawnRareEnemy();
        }
        else
        {
            SpawnBasicGroup();
        }
    }

    private void SpawnBasicGroup()
    {
        int count = Random.Range(1, 5); // Spawns 1, 2, 3, or 4 basics
        Debug.Log($"[Spawner] Spawning a Basic Group of {count} enemies.");
        for (int i = 0; i < count; i++)
        {
            TrySpawn(basicEnemyPrefab);
        }
    }

    private void SpawnRareEnemy()
    {
        Debug.Log("[Spawner] Spawning a Rare Enemy!");
        TrySpawn(rareEnemyPrefab);
    }

    private void SpawnEliteGroup()
    {
        Debug.Log("[Spawner] Spawning an Elite with 2 Escorts!");
        // 1 Elite Boss
        TrySpawn(eliteEnemyPrefab);
        // 2 Basic Escorts
        TrySpawn(basicEnemyPrefab);
        TrySpawn(basicEnemyPrefab);
    }

    private bool TrySpawn(GameObject prefab)
    {
        if (prefab == null) return false;

        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null) return false;

        // Ask MRUK for a random, safe position on the physical floor
        bool foundSpot = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            0.3f, // Minimum clearance space needed
            new LabelFilter(MRUKAnchor.SceneLabels.FLOOR),
            out Vector3 spawnPosition,
            out Vector3 spawnNormal
        );

        if (foundSpot)
        {
            // Drop them slightly from the sky so they fall into place securely
            Vector3 dropPosition = spawnPosition + (Vector3.up * 1.0f);
            Instantiate(prefab, dropPosition, Quaternion.identity);
            return true;
        }
        
        return false;
    }
}