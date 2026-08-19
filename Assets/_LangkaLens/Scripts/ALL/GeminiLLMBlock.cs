using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeminiLLMBlock : MonoBehaviour
{
    [Header("API Configuration")]
    public string apiKey = "YOUR_API_KEY_HERE";
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key=";
    [Header("Input")]
    [TextArea(5, 10)]
    public string prompt = "Describe the historical architecture of the Yarang ancient city ruins.";

    public void SendPrompt()
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            Debug.LogWarning("Prompt is empty!");
            return;
        }

        Debug.Log($"<b>[Gemini Input]:</b> {prompt}");
        StartCoroutine(SendRequest(prompt));
    }

    private IEnumerator SendRequest(string promptText)
    {
        GeminiRequest requestData = new GeminiRequest
        {
            contents = new Content[]
            {
                new Content { parts = new Part[] { new Part { text = promptText } } }
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        using (UnityWebRequest request = new UnityWebRequest(apiEndpoint + apiKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"[Gemini Error]: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (response != null && response.candidates.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text;
                    Debug.Log($"<b><color=#00FFAA>[Gemini Response]:</color></b>\n{reply}");
                }
            }
        }
    }

    // Data structures for JSON parsing
    [Serializable] public class GeminiRequest { public Content[] contents; }
    [Serializable] public class Content { public Part[] parts; }
    [Serializable] public class Part { public string text; }
    [Serializable] public class GeminiResponse { public Candidate[] candidates; }
    [Serializable] public class Candidate { public Content content; }
}

#if UNITY_EDITOR
// This custom editor generates the UI inside the Unity Inspector
[CustomEditor(typeof(GeminiLLMBlock))]
public class GeminiLLMBlockEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default variables (API Key, Prompt Text Area)
        DrawDefaultInspector();

        GeminiLLMBlock block = (GeminiLLMBlock)target;

        GUILayout.Space(15);

        // Create a large, clickable button in the inspector
        if (GUILayout.Button("Send to Gemini", GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                block.SendPrompt();
            }
            else
            {
                Debug.LogWarning("[System]: You must be in Play Mode (or using the XR Simulator) to send web requests.");
            }
        }
    }
}
#endif