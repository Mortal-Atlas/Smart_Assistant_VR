using UnityEngine;
using System.Collections;

public class FishingManager : MonoBehaviour
{
    private enum FishingState { Idle, Casted, Biting, Caught }
    private FishingState currentState = FishingState.Idle;

    [Header("Physics Setup")]
    [Tooltip("The Rigidbody of the physical bobber object in the water.")]
    public Rigidbody bobberRb;
    
    [Tooltip("An empty transform acting as the starting point of the fishing rod tip.")]
    public Transform bobberStartPoint;

    [Header("Tuning")]
    [Tooltip("How much force to multiply the phone's swipe velocity by.")]
    public float castForceMultiplier = 5.0f;

    private void OnEnable()
    {
        MqttQuestBridge.OnFishingCastReceived += HandleSwipeReceived;
        ResetBobber();
    }

    private void OnDisable()
    {
        MqttQuestBridge.OnFishingCastReceived -= HandleSwipeReceived;
    }

    private void HandleSwipeReceived(Vector2 velocity)
    {
        // Only react to swipes if this app is actually active
        if (!gameObject.activeInHierarchy) return;

        if (currentState == FishingState.Idle)
        {
            ExecuteCast(velocity);
        }
        else if (currentState == FishingState.Biting)
        {
            StartCoroutine(ExecuteCatch());
        }
        else if (currentState == FishingState.Casted)
        {
            Debug.Log("[Fishing] Reeled in too early! You spooked the fish.");
            ResetBobber();
        }
    }

    private void ExecuteCast(Vector2 velocity)
    {
        currentState = FishingState.Casted;
        
        if (bobberRb != null)
        {
            bobberRb.isKinematic = false; // Turn on gravity
            
            // Map the upward swipe (Y) to an Up and Forward 3D arc
            Vector3 throwForce = new Vector3(velocity.x, velocity.y, velocity.y) * castForceMultiplier;
            bobberRb.AddForce(throwForce, ForceMode.Impulse);
        }

        Debug.Log($"[Fishing] Bobber cast with force: {velocity.magnitude}");
        StartCoroutine(FishingLoop());
    }

    private IEnumerator FishingLoop()
    {
        // Wait for a random time between 3 and 8 seconds while the bobber is in the water
        float waitTime = Random.Range(3.0f, 8.0f);
        yield return new WaitForSeconds(waitTime);

        // Make sure they didn't reel it in early
        if (currentState == FishingState.Casted)
        {
            currentState = FishingState.Biting;
            
            // Yell at the physical phone to vibrate violently!
            MqttQuestBridge.Instance.PublishMessage(MargoTopics.PhoneRumble, "BITE");
            Debug.Log("[Fishing] FISH ON! Waiting for user to yank the rod...");

            // The user has 1.5 seconds to swipe up on their phone
            yield return new WaitForSeconds(1.5f);

            if (currentState == FishingState.Biting)
            {
                Debug.Log("[Fishing] The fish got away...");
                ResetBobber();
            }
        }
    }

    private IEnumerator ExecuteCatch()
    {
        currentState = FishingState.Caught;
        Debug.Log("[Fishing] CAUGHT IT! Adding Sushi to Tomodatchi Inventory!");

        // Pull the bobber back immediately
        ResetBobber();

        // Increment the cloud-saved pet inventory so you can feed it later
        if (TomodatchiManager.Instance != null)
        {
            TomodatchiManager.Instance.petState.sushi += 1;
            
            // Force the updated save state directly to the broker
            string saveJson = JsonUtility.ToJson(TomodatchiManager.Instance.petState);
            MqttQuestBridge.Instance.PublishMessage(MargoTopics.PetState, saveJson, true);
        }

        // Cool-down before you can cast again
        yield return new WaitForSeconds(2.0f);
        currentState = FishingState.Idle;
    }

    private void ResetBobber()
    {
        currentState = FishingState.Idle;
        if (bobberRb != null && bobberStartPoint != null)
        {
            bobberRb.isKinematic = true;
            bobberRb.linearVelocity = Vector3.zero;
            bobberRb.transform.position = bobberStartPoint.position;
        }
    }
}