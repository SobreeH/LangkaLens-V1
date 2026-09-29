using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GeminiChatUI : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text chatLogText;
    public TMP_InputField inputField;
    public Button sendButton;
    [SerializeField] private TMP_Text sendButtonLabel;
    [Tooltip("Button to clear the chat messages from the viewport.")]
    public Button clearButton;
    [Tooltip("Drag your Scroll View object here to enable auto-scrolling.")]
    public ScrollRect scrollRect;

    [Header("API Configuration")]
    public string apiKey = "YOUR_API_KEY_HERE";
    [Tooltip("Gemini Flash or Flash-Lite model name for chat generation.")]
    public string modelName = "gemini-3.5-flash-lite";
    private string ApiEndpoint => $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key=";

    [Header("AI Settings")]
    [Tooltip("Maximum length of the AI's response in tokens. 100 tokens is roughly 60-80 words.")]
    public int maxOutputTokens = 150;

    [TextArea(2,4)]
    [Tooltip("English guide instructions, used when the app locale is English (or not Thai).")]
    public string systemPrompt = "You are a tour guide for Yarang Ancient City in Pattani, Thailand. Answer in English using short sentences and as few tokens as possible without losing accuracy. Avoid repetition and filler. If unsure, say so; do not invent facts.";

    [TextArea(2, 4)]
    [Tooltip("Thai guide instructions, used when the app locale is Thai.")]
    public string thaiSystemPrompt = "คุณคือมัคคุเทศก์เมืองโบราณยะรัง จังหวัดปัตตานี ประเทศไทย ตอบเป็นภาษาไทยด้วยประโยคสั้น ใช้โทเคนให้น้อยที่สุดโดยคงความถูกต้อง ไม่กล่าวซ้ำหรือเกริ่นเกินจำเป็น หากไม่แน่ใจให้บอกตามตรง ห้ามแต่งข้อมูล";

#if UNITY_EDITOR
    [Header("Inspector Debugging")]
    [TextArea(3, 5)]
    public string debugPrompt = "โปรดอธิบายลักษณะสถาปัตยกรรมทางประวัติศาสตร์ของซากเมืองโบราณยะรัง จังหวัดปัตตานี";
#endif

    private Coroutine currentRequestCoroutine;
    private UnityWebRequest currentWebRequest;
    private bool requestPending;
    private string pendingStatus;
    private LocalizeStringEvent sendLabelLocalization;
    private bool restoreLabelLocalization;
    private bool awaitingResponse;

    private void Awake()
    {
        if (sendButtonLabel == null && sendButton != null)
            sendButtonLabel = sendButton.GetComponentInChildren<TMP_Text>(true);
        if (sendButtonLabel != null)
            sendLabelLocalization = sendButtonLabel.GetComponent<LocalizeStringEvent>();
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        RefreshSendLabel();
        if (requestPending) ShowRequestStatus();
    }

    private void SetRequestPending(bool pending)
    {
        if (pending && !requestPending)
        {
            restoreLabelLocalization = sendLabelLocalization != null && sendLabelLocalization.enabled;
            if (restoreLabelLocalization) sendLabelLocalization.enabled = false;
        }
        requestPending = pending;
        if (!pending && restoreLabelLocalization && sendLabelLocalization != null)
        {
            sendLabelLocalization.enabled = true;
            sendLabelLocalization.RefreshString();
            restoreLabelLocalization = false;
        }
        RefreshSendLabel();
    }

    private void RefreshSendLabel()
    {
        if (sendButtonLabel != null)
            sendButtonLabel.text = requestPending
                ? (IsThaiLocale() ? "ยกเลิก" : "Cancel")
                : (IsThaiLocale() ? "ส่ง" : "Send");
    }

    private void ShowRequestStatus()
    {
        RemovePendingStatus();
        pendingStatus = awaitingResponse
            ? (IsThaiLocale() ? "\n\nกำลังรอคำตอบ..." : "\n\nAwaiting response...")
            : (IsThaiLocale() ? "\n\nกำลังส่ง..." : "\n\nSending...");
        if (chatLogText != null) chatLogText.text += pendingStatus;
        ScrollToBottom();
    }

    private void OnSendButtonClicked()
    {
        if (requestPending)
        {
            CancelRequest();
            if (chatLogText != null)
                chatLogText.text += IsThaiLocale() ? "\nยกเลิกคำขอแล้ว" : "\nRequest cancelled.";
            ScrollToBottom();
        }
        else SendMessageToGemini();
    }

    private void RemovePendingStatus()
    {
        if (chatLogText != null && !string.IsNullOrEmpty(pendingStatus) &&
            chatLogText.text.EndsWith(pendingStatus, StringComparison.Ordinal))
            chatLogText.text = chatLogText.text.Substring(0, chatLogText.text.Length - pendingStatus.Length);
        pendingStatus = null;
    }

    private void CancelRequest()
    {
        // Explicitly dispose on cancellation; stopped Unity coroutines may not unwind.
        var request = currentWebRequest;
        currentWebRequest = null;
        if (request != null)
            request.Abort();
        if (currentRequestCoroutine != null)
            StopCoroutine(currentRequestCoroutine);
        request?.Dispose();
        currentRequestCoroutine = null;
        SetRequestPending(false);
        RemovePendingStatus();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
        bool interrupted = requestPending;
        CancelRequest();
        if (interrupted && chatLogText != null)
            chatLogText.text += IsThaiLocale()
                ? "\nยกเลิกคำขอเมื่อปิดแชท กรุณาส่งอีกครั้ง"
                : "\nRequest cancelled when chat closed. Please send again.";
    }

    void Start()
    {
        if (sendButton != null)
        {
            sendButton.onClick.AddListener(OnSendButtonClicked);
        }
        if (clearButton != null)
        {
            clearButton.onClick.AddListener(ClearChatLog);
        }
        if (inputField != null)
        {
            inputField.onSubmit.AddListener(OnInputSubmitted);
        }
    }

    private void OnInputSubmitted(string value)
    {
        QuestKeyboardHandler keyboardHandler = inputField.GetComponent<QuestKeyboardHandler>();
        if (keyboardHandler == null || keyboardHandler.submitOnDone)
            SendMessageToGemini();
    }

    private void OnDestroy()
    {
        if (sendButton != null)
            sendButton.onClick.RemoveListener(OnSendButtonClicked);
        if (clearButton != null)
            clearButton.onClick.RemoveListener(ClearChatLog);
        if (inputField != null)
            inputField.onSubmit.RemoveListener(OnInputSubmitted);
    }

    public void SendMessageToGemini()
    {
        if (requestPending) return;
        string prompt = inputField != null ? inputField.text : "";
        if (string.IsNullOrWhiteSpace(prompt)) return;

        SendAndLog(prompt);

        if (inputField != null)
        {
            inputField.text = "";
            QuestKeyboardHandler keyboardHandler = inputField.GetComponent<QuestKeyboardHandler>();
            if (keyboardHandler != null)
            {
                keyboardHandler.CloseKeyboard();
            }
        }
    }

#if UNITY_EDITOR
    public void SendDebugPrompt()
    {
        if (string.IsNullOrWhiteSpace(debugPrompt)) return;
        SendAndLog(debugPrompt);
    }
#endif

    /// <summary>
    /// Clears the chat messages from the viewport and resets the scroll position to the top.
    /// Cancels any active in-flight AI response.
    /// </summary>
    public void ClearChatLog()
    {
        CancelRequest();

        if (chatLogText != null)
        {
            chatLogText.text = string.Empty;
        }

        ResetScrollPosition();

        Debug.Log("<b>[GeminiChatUI]:</b> Viewport content cleared.");
    }

    /// <summary>
    /// Alias for ClearChatLog.
    /// </summary>
    public void ClearChat() => ClearChatLog();

    /// <summary>
    /// Alias for ClearChatLog.
    /// </summary>
    public void ClearContent() => ClearChatLog();

    /// <summary>
    /// Resets the Scroll View position back to the top.
    /// </summary>
    public void ResetScrollPosition()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
            if (scrollRect.content != null)
            {
                scrollRect.content.anchoredPosition = Vector2.zero;
            }
        }
    }

    private string GetSenderUserLabel()
    {
        try
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("Gemini_UI", "Chat_Sender_User");
            if (!string.IsNullOrEmpty(localized)) return localized;
        }
        catch { }
        return IsThaiLocale() ? "คุณ" : "You";
    }

    private string GetSenderGuideLabel()
    {
        try
        {
            string localized = LocalizationSettings.StringDatabase.GetLocalizedString("Gemini_UI", "Chat_Sender_Guide");
            if (!string.IsNullOrEmpty(localized)) return localized;
        }
        catch { }
        return IsThaiLocale() ? "ไกด์" : "Guide";
    }

    private bool IsThaiLocale()
    {
        var currentLocale = LocalizationSettings.SelectedLocale;
        return currentLocale != null && currentLocale.Identifier.Code.StartsWith("th");
    }

    private string GetLocalizedSystemPrompt()
    {
        return IsThaiLocale() ? thaiSystemPrompt : systemPrompt;
    }

    private void SendAndLog(string prompt)
    {
        if (requestPending || !isActiveAndEnabled) return;
        // Visual distinction for the User Query using Rich Text Colors
        if (chatLogText != null)
        {
            string userLabel = GetSenderUserLabel();
            chatLogText.text += $"\n\n<color=#55AAFF><b>{userLabel}:</b></color> {prompt}";
            ScrollToBottom();
        }

        Debug.Log($"<b>[Gemini Input]:</b> {prompt}");
        awaitingResponse = false;
        SetRequestPending(true);
        ShowRequestStatus();
        currentRequestCoroutine = StartCoroutine(SendRequest(prompt));
    }

    private IEnumerator SendRequest(string prompt)
    {
        try
        {
            // Construct the payload with GenerationConfig and SystemInstruction
            GeminiRequest requestData = new GeminiRequest
            {
                system_instruction = new SystemInstructionData
                {
                    role = "system",
                    parts = new Part[] { new Part { text = GetLocalizedSystemPrompt() } }
                },
                contents = new Content[]
                {
                    new Content { parts = new Part[] { new Part { text = prompt } } }
                },
                generation_config = new GenerationConfigData
                {
                    max_output_tokens = this.maxOutputTokens
                }
            };

            string jsonPayload = JsonUtility.ToJson(requestData);
            string url = ApiEndpoint + apiKey;

            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                currentWebRequest = request;
                request.timeout = 60;
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                {
                    if (!awaitingResponse && request.uploadProgress >= 1f)
                    {
                        awaitingResponse = true;
                        ShowRequestStatus();
                    }
                    yield return null;
                }
                RemovePendingStatus();

                // If the request was aborted during clear, exit cleanly
                if (request.result == UnityWebRequest.Result.ConnectionError && request.error == "Request aborted")
                {
                    currentWebRequest = null;
                    currentRequestCoroutine = null;
                    yield break;
                }

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string rawError = request.downloadHandler != null ? request.downloadHandler.text : "";
                    if (string.IsNullOrWhiteSpace(rawError)) rawError = request.error ?? "No response from server.";
                    Debug.LogError($"[Gemini Error]: {request.error}\n{rawError}");

                    if (chatLogText != null)
                    {
                        string errorLabel = IsThaiLocale() ? "ข้อผิดพลาด:" : "Debug Error:";
                        chatLogText.text += $"\n<color=red><b>{errorLabel}</b> {rawError}</color>";
                        ScrollToBottom();
                    }
                }
                else
                {
                    GeminiResponse response = null;
                    try
                    {
                        response = JsonUtility.FromJson<GeminiResponse>(request.downloadHandler.text);
                    }
                    catch (ArgumentException)
                    {
                        Debug.LogWarning("[GeminiChatUI] The service returned an invalid JSON response.");
                    }
                    string reply = GetReplyText(response);
                    if (!string.IsNullOrWhiteSpace(reply))
                    {
                        Debug.Log($"<b><color=#00FFAA>[Gemini Response]:</color></b>\n{reply}");

                        // Visual distinction for the AI Response using Rich Text Colors
                        if (chatLogText != null)
                        {
                            string guideLabel = GetSenderGuideLabel();
                            chatLogText.text += $"\n\n<color=#00FFAA><b>{guideLabel}:</b></color> {reply}";
                            ScrollToBottom();
                        }
                    }
                    else
                    {
                        string reason = response?.promptFeedback?.blockReason;
                        if (string.IsNullOrEmpty(reason) && response?.candidates != null && response.candidates.Length > 0)
                            reason = response.candidates[0]?.finishReason;
                        Debug.LogWarning($"[GeminiChatUI] No answer text returned. Model={modelName}, finishReason={reason ?? "unknown"}, maxOutputTokens={maxOutputTokens}");
                        if (chatLogText != null)
                        {
                            chatLogText.text += IsThaiLocale()
                                ? "\nไม่ได้รับข้อความคำตอบ กรุณาลองอีกครั้ง"
                                : "\nNo answer text was returned. Please try again.";
                            ScrollToBottom();
                        }
                    }
                }
                currentWebRequest = null;
            }
        }
        finally
        {
            currentWebRequest = null;
            currentRequestCoroutine = null;
            RemovePendingStatus();
            SetRequestPending(false);
        }
    }

    private static string GetReplyText(GeminiResponse response)
    {
        if (response?.candidates == null || response.candidates.Length == 0) return "";
        var parts = response.candidates[0]?.content?.parts;
        if (parts == null) return "";
        var text = new StringBuilder();
        foreach (var part in parts)
            if (part != null && !part.thought && !string.IsNullOrEmpty(part.text))
                text.Append(part.text);
        return text.ToString();
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
    public SystemInstructionData system_instruction;
    public Content[] contents;
    public GenerationConfigData generation_config;
}
[Serializable] public class SystemInstructionData
{
    public string role;
    public Part[] parts;
}
[Serializable] public class GenerationConfigData
{
    public int max_output_tokens;
}
[Serializable] public class Content { public Part[] parts; }
[Serializable] public class Part { public string text; }
[Serializable] public class GeminiResponse { public Candidate[] candidates; public GeminiPromptFeedback promptFeedback; }
[Serializable] public class Candidate { public GeminiResponseContent content; public string finishReason; }
[Serializable] public class GeminiResponseContent { public GeminiResponsePart[] parts; }
[Serializable] public class GeminiResponsePart { public string text; public bool thought; }
[Serializable] public class GeminiPromptFeedback { public string blockReason; }

#if UNITY_EDITOR
[CustomEditor(typeof(GeminiChatUI))]
public class GeminiChatUIEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        GeminiChatUI chatScript = (GeminiChatUI)target;
        SerializedProperty modelProp = serializedObject.FindProperty("modelName");

        GUILayout.Space(10);
        GeminiModelPicker.DrawModelSelector(serializedObject, modelProp, chatScript.apiKey, "Chat Model");

        serializedObject.ApplyModifiedProperties();

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

        if (GUILayout.Button("Clear Chat Content", buttonStyle, GUILayout.Height(30)))
        {
            chatScript.ClearChatLog();
        }
    }
}
#endif
