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

public class GeminiChatUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text chatLogText;
    public TMP_InputField inputField;
    public Button sendButton;
    [Tooltip("Drag your Scroll View object here to enable auto-scrolling.")]
    public ScrollRect scrollRect; 

    [Header("API Configuration")]
    public string apiKey = "YOUR_API_KEY_HERE"; 
    private string apiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-3.5-flash-lite:generateContent?key=";
    
    [Header("AI Settings")]
    [Tooltip("Maximum length of the AI's response in tokens. 100 tokens is roughly 60-80 words.")]
    public int maxOutputTokens = 150; 
    
    [TextArea(2,4)]
    [Tooltip("Hidden instructions to dictate the AI's behavior and constraints.")]
    public string systemPrompt = "You are an expert AI guide for the Yarang Ancient City VR museum. Keep all responses strictly under 2 sentences.";

#if UNITY_EDITOR
    [Header("Inspector Debugging")]
    [TextArea(3, 5)]
    public string debugPrompt = "โปรดอธิบายลักษณะสถาปัตยกรรมทางประวัติศาสตร์ของซากเมืองโบราณยะรัง จังหวัดปัตตานี";
#endif

    void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(SendMessageToGemini);
        }
    }

    public void SendMessageToGemini()
    {
        string prompt = inputField != null ? inputField.text : "";
        if (string.IsNullOrWhiteSpace(prompt)) return;

        SendAndLog(prompt);
        
        if (inputField != null) inputField.text = "";
    }

#if UNITY_EDITOR
    public void SendDebugPrompt()
    {
        if (string.IsNullOrWhiteSpace(debugPrompt)) return;
        SendAndLog(debugPrompt);
    }
#endif

    private void SendAndLog(string prompt)
    {
        // Visual distinction for the User Query using Rich Text Colors
        if (chatLogText != null) 
        {
            chatLogText.text += $"\n\n<color=#55AAFF><b>You:</b></color> {prompt}";
            ScrollToBottom();
        }
        
        Debug.Log($"<b>[Gemini Input]:</b> {prompt}");
        StartCoroutine(SendRequest(prompt));
    }

    private IEnumerator SendRequest(string prompt)
    {
        // Construct the payload with GenerationConfig and SystemInstruction
        GeminiRequest requestData = new GeminiRequest
        {
            systemInstruction = new SystemInstructionData
            {
                role = "system",
                parts = new Part[] { new Part { text = systemPrompt } }
            },
            contents = new Content[]
            {
                new Content { parts = new Part[] { new Part { text = prompt } } }
            },
            generationConfig = new GenerationConfigData
            {
                maxOutputTokens = this.maxOutputTokens
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestData);
        string url = apiEndpoint + apiKey;

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
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
                    ScrollToBottom();
                }
            }
            else
            {
                GeminiResponse response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                if (response != null && response.candidates.Length > 0)
                {
                    string reply = response.candidates[0].content.parts[0].text;
                    
                    Debug.Log($"<b><color=#00FFAA>[Gemini Response]:</color></b>\n{reply}");
                    
                    // Visual distinction for the AI Response using Rich Text Colors
                    if (chatLogText != null) 
                    {
                        chatLogText.text += $"\n\n<color=#00FFAA><b>Guide:</b></color> {reply}";
                        ScrollToBottom();
                    }
                }
            }
        }
    }
    
    // Forces the Scroll View to snap to the newest message
    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }
}

// Data structures mapping to the API payload
[Serializable] public class GeminiRequest 
{ 
    public SystemInstructionData systemInstruction;
    public Content[] contents; 
    public GenerationConfigData generationConfig;
}
[Serializable] public class SystemInstructionData 
{ 
    public string role; 
    public Part[] parts; 
}
[Serializable] public class GenerationConfigData 
{ 
    public int maxOutputTokens; 
}
[Serializable] public class Content { public Part[] parts; }
[Serializable] public class Part { public string text; }
[Serializable] public class GeminiResponse { public Candidate[] candidates; }
[Serializable] public class Candidate { public Content content; }

#if UNITY_EDITOR
[CustomEditor(typeof(GeminiChatUI))]
public class GeminiChatUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GeminiChatUI chatScript = (GeminiChatUI)target;
        GUILayout.Space(15);
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

        if (GUILayout.Button("Send Debug Prompt", buttonStyle, GUILayout.Height(35)))
        {
            if (Application.isPlaying)
            {
                chatScript.SendDebugPrompt();
            }
            else
            {
                Debug.LogWarning("[System]: You must be in Play Mode to send web requests.");
            }
        }
    }
}
#endif