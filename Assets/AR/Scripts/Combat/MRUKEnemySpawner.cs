using UnityEngine;
using System.Collections;
using Meta.XR.MRUtilityKit; 

public class MRUKEnemySpawner : MonoBehaviour
{
    [Header("Spawning Settings")]
    public GameObject enemyPrefab;
    public int maxEnemies = 5;
    public float spawnInterval = 3f;

    private bool isRoomLoaded = false;

    void Start()
    {
        // We must wait for MRUK to finish building the invisible room before we spawn anything!
        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
        }
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        Debug.Log("[MRUK] Room loaded physically! Starting enemy spawns.");
        isRoomLoaded = true;
        StartCoroutine(SpawnEnemiesRoutine());
    }

    private IEnumerator SpawnEnemiesRoutine()
    {
        int enemiesSpawned = 0;

        while (enemiesSpawned < maxEnemies)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnSingleEnemy();
            enemiesSpawned++;
        }
    }

    private void SpawnSingleEnemy()
    {
        MRUKRoom currentRoom = MRUK.Instance.GetCurrentRoom();
        if (currentRoom == null) return;

        // The exact 5 arguments required by the new Meta MRUK update
        // Fixed: Replaced LabelFilter.Included with new LabelFilter
        bool foundSpot = currentRoom.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            0.1f,
            new LabelFilter(MRUKAnchor.SceneLabels.FLOOR),
            out Vector3 spawnPosition,
            out Vector3 spawnNormal
        );

        if (foundSpot)
        {
            // Drop them from the sky to ensure they land securely on the floor mesh
            Vector3 dropPosition = spawnPosition + Vector3.up * 1.0f;
            Instantiate(enemyPrefab, dropPosition, Quaternion.identity);
            Debug.Log("Spawned enemy on the MRUK Floor!");
        }
    }
}