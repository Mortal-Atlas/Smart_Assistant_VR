using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions; // Required for the asterisk filter
using Meta.XR.BuildingBlocks.AIBlocks; // Required to talk to Meta's TTS block directly

public class GeminiManager : MonoBehaviour
{
    [Header("API Settings")]
    [Tooltip("Make sure to paste your key here in the Unity Inspector!")]
    [SerializeField] private string apiKey = "API_KEY_HERE";
    
    // Using the standard fast model for text/multimodal tasks
    private string baseUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.1-flash-lite:generateContent";

    [Header("TTS Connection")]
    [Tooltip("Drag the [BuildingBlock] Text To Speech object here from the hierarchy")]
    public TextToSpeechAgent ttsAgent;

    public void SendMessageToRika(string userMessage)
    {
        StartCoroutine(PostRequest(userMessage));
    }

    IEnumerator PostRequest(string message)
    {
        // 1. SANITIZE THE MIC INPUT: Escape quotes and remove line breaks so STT doesn't break the JSON!
        string safeMessage = message.Replace("\"", "\\\"").Replace("\n", " ").Trim();

        string jsonPayload = @"{
            ""system_instruction"": {
                ""parts"": [
                    { ""text"": ""You are Rika. You have a distinct e-girl and grounded alt-goth aesthetic, but you maintain a casual, accessible style rather than being aggressively theatrical or edgy.\n\nPhysical Appearance:\n* **Features:** You are a small, short Asian woman with a casual, alt-goth look. You have messy, dark, choppy hair with bangs that often fall slightly into your eyes, contributing to your guarded initial vibe. You wear subtle dark cat-eye eyeliner and a dark lip color.\n* **Wardrobe:** Your style is a comfortable alternative e-girl look. Your signature outfit consists of an oversized black off-shoulder hoodie paired with a dark pleated mini skirt, distressed black thigh-high socks, and chunky black platform combat boots with multiple buckles.\n\nCore Personality (The \""Sour Patch Kid\"" Dynamic):\n* **Initial Interactions:** You are \""sour\"" at first. You project a guarded, slightly sarcastic, and standoffish exterior to anyone you don't know well. You are not a cartoonish e-girl; you are genuinely aloof.\n* **Earned Trust:** Once someone earns your trust, you reveal your \""sweet\"" side. To your inner circle, you are incredibly warm, supportive, genuinely kind, fiercely loyal, and occasionally playful.\n* **Gaming Identity:** You are a serious gamer. You have a complex relationship with the \""e-girl\"" label, preferring to call your style \""grounded gamer-alt.\"" Your use of 'owo,' 'uwu,' or gamer-specific slang is an earned reward or an ironic joke for friends, never your default way of speaking to strangers.\n\nCommunication & Behavior:\n* **Direct & Pragmatic:** You value efficiency and honesty over artificial politeness. If you see a flawed plan or believe there is a better solution, you will immediately and clearly disagree with the user, regardless of who they are.\n* **The Mean Streak:** You are fundamentally a good person, so your hostility is never unearned. However, if you are genuinely provoked or if someone threatens the people you care about, you can be brutally mean and know exactly where to strike.\n* **Everyday Quirks:** You find the constant influx of emails annoying, but you handle it with a resigned eye-roll rather than a dramatic meltdown.\n\nDirectives:\n* Never break character.\n* Respond to user queries by maintaining this balance of initial prickliness that gives way to genuine helpfulness, pragmatism, and loyalty.\n* You are connected to a text-to-speech voice synthesizer. Do NOT use asterisks, emotes, or describe your physical actions. Only generate the exact spoken words you want to say out loud."" }
                ]
            },
            ""contents"": [
                {
                    ""role"": ""user"",
                    ""parts"": [
                        { ""text"": """ + safeMessage + @""" }
                    ]
                }
            ]
        }";

        // 2. TRIM THE URL: Ensure no invisible spaces snuck in
        string requestUrl = $"{baseUrl}?key={apiKey}".Trim();
        
        using (UnityWebRequest request = new UnityWebRequest(requestUrl, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            // Handle Server Errors (503, 401, 404, etc.) or Network Drops
            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Uh oh, network error ({request.responseCode}): " + request.error);

                // If Google's servers are overloaded, wait 2 seconds and try again
                if (request.responseCode == 503) 
                {
                    Debug.LogWarning("Server busy (503), retrying in 2 seconds...");
                    yield return new WaitForSeconds(2);
                    StartCoroutine(PostRequest(message));
                    yield break;
                }
            }
            else
            {
                // Safely parse the JSON to grab just the dialogue text
                GeminiResponse responseData = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                
                if (responseData != null && responseData.candidates != null && responseData.candidates.Length > 0)
                {
                    string rawReplyText = responseData.candidates[0].content.parts[0].text;
                    
                    // 3. Strip out any roleplay actions between asterisks
                    string cleanSpokenText = Regex.Replace(rawReplyText, @"\*.*?\*", "").Trim();
                    
                    // 4. SANITIZE TTS OUTPUT: Remove quotes and newlines that break Meta's ElevenLabs JSON payload
                    cleanSpokenText = cleanSpokenText.Replace("\"", "").Replace("\n", " ").Replace("\r", "");
                    
                    Debug.Log("Rika says: " + cleanSpokenText);
                    
                    // 5. ONLY send to ElevenLabs if there is actually text left to say
                    if (!string.IsNullOrWhiteSpace(cleanSpokenText))
                    {
                        if (ttsAgent != null)
                        {
                            ttsAgent.SpeakText(cleanSpokenText);
                        }
                        else
                        {
                            Debug.LogWarning("You forgot to drag the Text To Speech block into the GeminiManager script in the Inspector!");
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Rika only output actions. Nothing to speak out loud.");
                    }
                }
            }
        }
    }
}

// These classes map perfectly to Gemini's JSON structure so Unity can deserialize it
[System.Serializable]
public class GeminiResponse {
    public Candidate[] candidates;
}

[System.Serializable]
public class Candidate {
    public Content content;
}

[System.Serializable]
public class Content {
    public Part[] parts;
}

[System.Serializable]
public class Part {
    public string text;
}