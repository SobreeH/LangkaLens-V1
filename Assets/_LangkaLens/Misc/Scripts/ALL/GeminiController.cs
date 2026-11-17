using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeminiController : MonoBehaviour
{
    [Header("API Configuration")]
    [Tooltip("Optional local override. Leave blank to load geminiApiKey from gitignored Resources/LangkaLensSecrets.json or .env.")]
    public string apiKey = "";

    public string EffectiveApiKey => ApiSecretsConfig.GetGeminiApiKey(apiKey);
    [Tooltip("Gemini Flash or Flash-Lite model name for generation.")]
    public string modelName = "gemini-3.5-flash-lite";
    private string ApiEndpoint => $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key=";

    [Header("UI References (Optional)")]
    [Tooltip("Leave these blank if you only want to test via the Inspector.")]
    public TMP_Text chatLogText;
    public TMP_InputField inputField;
    public Button sendButton;

    [Header("Inspector Debugging")]
    [TextArea(3, 5)]
    public string debugPrompt = "Describe the historical architecture of the Yarang ancient city ruins.";

    void Start()
    {
        // Only hook up the button listener if a button is assigned
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessageFromUI);
        }
    }

    // Called by the physical Canvas button
    public void SendMessageFromUI()
    {
        string prompt = inputField != null ? inputField.text : "";
        if (string.IsNullOrWhiteSpace(prompt)) return;

        SendAndLog(prompt);
        
        if (inputField != null) inputField.text = "";
    }

    // Called by the Custom Inspector button
    public void SendDebugPrompt()
    {
        if (string.IsNullOrWhiteSpace(debugPrompt))
        {
            Debug.LogWarning("Debug Prompt is empty!");
            return;
        }
        SendAndLog(debugPrompt);
    }

    // Shared logic for UI updates and Console logging
    private void SendAndLog(string prompt)
    {
        if (chatLogText != null) chatLogText.text += $"\n<b>You:</b> {prompt}";
        
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

        string effectiveKey = EffectiveApiKey;
        if (string.IsNullOrWhiteSpace(effectiveKey))
        {
            Debug.LogError("[GeminiController] Gemini API key is missing! Configure Assets/Resources/LangkaLensSecrets.json or .env.");
            yield break;
        }

        string jsonPayload = JsonUtility.ToJson(requestData);
        using (UnityWebRequest request = new UnityWebRequest(ApiEndpoint + effectiveKey, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string rawError = request.downloadHandler != null ? request.downloadHandler.text : "No response from server.";
                
                Debug.LogError($"[Gemini Error]: {request.error}\n{rawError}");
                
                if (chatLogText != null) 
                {
                    chatLogText.text += $"\n<color=red><b>Debug Error:</b> {rawError}</color>";
                }
            }
            else
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (response != null && response.candidates.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text;
                    
                    Debug.Log($"<b><color=#00FFAA>[Gemini Response]:</color></b>\n{reply}");
                    
                    if (chatLogText != null) chatLogText.text += $"\n<b>Guide:</b> {reply}";
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
[CustomEditor(typeof(GeminiController))]
public class GeminiControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        GeminiController script = (GeminiController)target;
        SerializedProperty modelProp = serializedObject.FindProperty("modelName");

        GUILayout.Space(10);
        GeminiModelPicker.DrawModelSelector(serializedObject, modelProp, script.EffectiveApiKey, "Model");

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(15);
        
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

        // Create a large, clickable button in the inspector
        if (GUILayout.Button("Send Debug Prompt", buttonStyle, GUILayout.Height(40)))
        {
            if (Application.isPlaying)
            {
                script.SendDebugPrompt();
            }
            else
            {
                Debug.LogWarning("[System]: You must be in Play Mode (or using the XR Simulator) to send web requests.");
            }
        }
    }
}
#endif