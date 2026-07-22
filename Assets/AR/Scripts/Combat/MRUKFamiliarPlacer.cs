using UnityEngine;
using Meta.XR.MRUtilityKit;

public class MRUKFamiliarPlacer : MonoBehaviour
{
    [Header("Familiar Settings")]
    [Tooltip("Drag your Familiar GameObject here")]
    public GameObject familiarObject;
    
    [Tooltip("Optional: A poof particle effect for when he appears")]
    public GameObject spawnEffectPrefab;

    private CharacterController familiarController;

    void Start()
    {
        if (familiarObject == null)
        {
            Debug.LogError("[MRUKFamiliarPlacer] No Familiar assigned!");
            return;
        }

        familiarController = familiarObject.GetComponent<CharacterController>();

        // Disable the controller so gravity doesn't pull him down while MRUK loads
        if (familiarController != null)
        {
            familiarController.enabled = false;
        }

        if (MRUK.Instance != null)
        {
            MRUK.Instance.RoomCreatedEvent.AddListener(OnRoomLoaded);
        }
    }

    private void OnRoomLoaded(MRUKRoom room)
    {
        Debug.Log("[MRUKFamiliarPlacer] Room is ready! Placing Familiar...");

        Transform playerHead = Camera.main.transform;
        
        // The exact 5 arguments required by the new Meta MRUK update
        // Fixed: Replaced LabelFilter.Included with new LabelFilter
        bool foundSpot = room.GenerateRandomPositionOnSurface(
            MRUK.SurfaceType.FACING_UP,
            0.1f,
            new LabelFilter(MRUKAnchor.SceneLabels.FLOOR),
            out Vector3 spawnPosition,
            out Vector3 spawnNormal
        );

        if (foundSpot)
        {
            familiarObject.transform.position = spawnPosition;

            Vector3 lookPos = playerHead.position;
            lookPos.y = familiarObject.transform.position.y;
            familiarObject.transform.LookAt(lookPos);

            if (spawnEffectPrefab != null)
            {
                Instantiate(spawnEffectPrefab, spawnPosition, Quaternion.identity);
            }
        }
        else
        {
            Debug.LogWarning("[MRUKFamiliarPlacer] MRUK couldn't find a safe floor spot! Snapping to 1m in front of player.");
            Vector3 fallbackPos = playerHead.position + (playerHead.forward * 1.0f);
            fallbackPos.y = 0f; 
            familiarObject.transform.position = fallbackPos;
        }

        // Re-enable physics now that he is safely placed
        if (familiarController != null)
        {
            familiarController.enabled = true;
        }
    }
}