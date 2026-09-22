using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpeechToTextHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Target input field where the transcribed text will be placed.")]
    public TMP_InputField targetInputField;

    [Tooltip("Button used to start and stop speech recording.")]
    public Button speakButton;

    [Tooltip("Optional: Text component of the Speak Button. Auto-detected from speakButton if not assigned.")]
    public TMP_Text speakButtonText;

    [Tooltip("Reference to GeminiChatUI to automatically inherit API configuration if left blank.")]
    public GeminiChatUI geminiChatUI;

    [Header("API Configuration")]
    [Tooltip("Google Gemini API Key. If left empty, will try to use the key from GeminiChatUI.")]
    public string apiKey = "";

    [Tooltip("Gemini model supporting multimodal audio input.")]
    public string modelName = "gemini-3.6-flash";

    [Header("Recording Settings")]
    [Tooltip("Maximum recording duration in seconds before automatically stopping and transcribing.")]
    [Range(3, 30)]
    public int maxRecordingDuration = 15;

    [Tooltip("Audio sample rate for microphone recording (16000 Hz recommended for speech).")]
    public int sampleRate = 16000;

    [Tooltip("Button tint color applied while recording is active.")]
    public Color recordingButtonColor = new Color(0.92f, 0.3f, 0.25f, 1f);

    [Header("State (Read Only)")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private bool isTranscribing = false;

    // Internal state cache
    private AudioClip recordingClip;
    private string microphoneDevice;
    private Coroutine recordingCoroutine;
    private Color originalButtonColor = Color.white;
    private Image buttonImage;
    private LocalizeStringEvent localizeStringEvent;

    public bool IsRecording => isRecording;
    public bool IsTranscribing => isTranscribing;

    private void Awake()
    {
        InitializeComponents();
    }

    private void Start()
    {
        if (speakButton != null)
        {
            speakButton.onClick.AddListener(OnSpeakButtonClicked);
        }

        CheckMicrophonePermission();
    }

    private void OnDestroy()
    {
        if (speakButton != null)
        {
            speakButton.onClick.RemoveListener(OnSpeakButtonClicked);
        }

        if (isRecording)
        {
            StopMicrophone();
        }
    }

    private void InitializeComponents()
    {
        if (geminiChatUI == null)
        {
            geminiChatUI = GetComponentInParent<GeminiChatUI>();
            if (geminiChatUI == null)
            {
                geminiChatUI = FindAnyObjectByType<GeminiChatUI>();
            }
        }

        if (targetInputField == null && geminiChatUI != null)
        {
            targetInputField = geminiChatUI.inputField;
        }

        if (speakButton == null)
        {
            // Look for a child named Speak_Button
            Transform btnTransform = transform.Find("Speak_Button") ?? transform.Find("Panel/Speak_Button");
            if (btnTransform != null)
            {
                speakButton = btnTransform.GetComponent<Button>();
            }
        }

        if (speakButton != null)
        {
            buttonImage = speakButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                originalButtonColor = buttonImage.color;
            }

            if (speakButtonText == null)
            {
                speakButtonText = speakButton.GetComponentInChildren<TMP_Text>();
            }

            if (speakButtonText != null)
            {
                localizeStringEvent = speakButtonText.GetComponent<LocalizeStringEvent>();
            }
        }
    }

    private void CheckMicrophonePermission()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            UnityEngine.Android.Permission.RequestUserPermission(UnityEngine.Android.Permission.Microphone);
        }
#endif
    }

    public void OnSpeakButtonClicked()
    {
        if (isTranscribing)
        {
            Debug.Log("[SpeechToTextHandler] Transcription currently in progress, please wait.");
            return;
        }

        if (isRecording)
        {
            StopRecordingAndTranscribe();
        }
        else
        {
            StartRecording();
        }
    }

    public void StartRecording()
    {
        CheckMicrophonePermission();

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[SpeechToTextHandler] No microphone audio input devices found!");
            ShowTemporaryStatus(IsThaiLocale() ? "ไม่พบไมโครโฟน" : "No Microphone Found", 2f);
            return;
        }

        microphoneDevice = null; // Default device
        isRecording = true;

        // Visual feedback for recording
        SetButtonVisualState(true, IsThaiLocale() ? "<sprite name=microphone> กำลังฟัง..." : "<sprite=\"microphone\" index=0> Listening...");

        // Start recording
        recordingClip = Microphone.Start(microphoneDevice, false, maxRecordingDuration, sampleRate);

        if (recordingCoroutine != null) StopCoroutine(recordingCoroutine);
        recordingCoroutine = StartCoroutine(RecordingTimeoutRoutine());

        Debug.Log("[SpeechToTextHandler] Microphone recording started.");
    }

    public void StopRecordingAndTranscribe()
    {
        if (!isRecording) return;

        if (recordingCoroutine != null)
        {
            StopCoroutine(recordingCoroutine);
            recordingCoroutine = null;
        }

        int recordingPosition = Microphone.GetPosition(microphoneDevice);
        StopMicrophone();

        if (recordingClip == null || recordingPosition <= 0)
        {
            Debug.LogWarning("[SpeechToTextHandler] Recording captured no audio samples.");
            ResetButtonVisualState();
            return;
        }

        StartCoroutine(TranscribeAudioRoutine(recordingClip, recordingPosition));
    }

    private void StopMicrophone()
    {
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
        isRecording = false;
    }

    private IEnumerator RecordingTimeoutRoutine()
    {
        yield return new WaitForSeconds(maxRecordingDuration);
        if (isRecording)
        {
            Debug.Log("[SpeechToTextHandler] Max recording duration reached. Auto-transcribing.");
            StopRecordingAndTranscribe();
        }
    }

    private IEnumerator TranscribeAudioRoutine(AudioClip clip, int sampleCount)
    {
        isTranscribing = true;

        // Visual feedback for transcribing
        bool isThai = IsThaiLocale();
        SetButtonVisualState(false, isThai ? "<sprite name=microphone> กำลังถอดความ..." : "<sprite=\"microphone\" index=0> Transcribing...", interactable: false);

        // Convert audio clip to 16-bit PCM WAV byte array
        byte[] wavBytes = TrimAndEncodeToWav(clip, sampleCount);
        string base64Audio = Convert.ToBase64String(wavBytes);

        // Get effective API key
        string effectiveKey = !string.IsNullOrWhiteSpace(apiKey) ? apiKey : (geminiChatUI != null ? geminiChatUI.apiKey : "");
        if (string.IsNullOrWhiteSpace(effectiveKey) || effectiveKey == "YOUR_API_KEY_HERE")
        {
            Debug.LogError("[SpeechToTextHandler] Gemini API key is missing or not configured!");
            ShowTemporaryStatus(isThai ? "ไม่มี API Key" : "Missing API Key", 2.5f);
            isTranscribing = false;
            ResetButtonVisualState();
            yield break;
        }

        // Prepare language prompt
        string promptText;
        if (isThai)
        {
            promptText = "Transcribe the spoken audio into Thai (ภาษาไทย). Output ONLY the verbatim transcribed text without any quotation marks, explanations, notes, or markdown formatting. If the audio is silent or contains no intelligible words, return an empty string.";
        }
        else
        {
            promptText = "Transcribe the spoken audio into English. Output ONLY the verbatim transcribed text without any quotation marks, explanations, notes, or markdown formatting. If the audio is silent or contains no intelligible words, return an empty string.";
        }

        // Construct request JSON
        GeminiAudioRequest requestPayload = new GeminiAudioRequest
        {
            contents = new GeminiAudioContent[]
            {
                new GeminiAudioContent
                {
                    parts = new GeminiAudioPart[]
                    {
                        new GeminiAudioPart
                        {
                            inlineData = new GeminiInlineData
                            {
                                mimeType = "audio/wav",
                                data = base64Audio
                            }
                        },
                        new GeminiAudioPart
                        {
                            text = promptText
                        }
                    }
                }
            },
            generationConfig = new GeminiAudioGenConfig
            {
                temperature = 0.0f,
                maxOutputTokens = 200
            }
        };

        string jsonPayload = JsonUtility.ToJson(requestPayload);
        string url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={effectiveKey}";

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string rawError = request.downloadHandler != null ? request.downloadHandler.text : "No server response.";
                Debug.LogError($"[SpeechToTextHandler] Error from Gemini API: {request.error}\n{rawError}");
                ShowTemporaryStatus(isThai ? "ถอดความล้มเหลว" : "Transcription Failed", 2f);
            }
            else
            {
                GeminiAudioResponse response = JsonUtility.FromJson<GeminiAudioResponse>(request.downloadHandler.text);
                if (response != null && response.candidates != null && response.candidates.Length > 0 &&
                    response.candidates[0].content != null && response.candidates[0].content.parts != null &&
                    response.candidates[0].content.parts.Length > 0)
                {
                    string transcription = response.candidates[0].content.parts[0].text;
                    if (!string.IsNullOrWhiteSpace(transcription))
                    {
                        transcription = transcription.Trim().Trim('"', '\'', '`');
                        Debug.Log($"<b><color=#00FFAA>[SpeechToText Result]:</color></b> {transcription}");

                        if (targetInputField != null)
                        {
                            targetInputField.text = transcription;
                            targetInputField.onValueChanged?.Invoke(transcription);
                            targetInputField.caretPosition = transcription.Length;
                        }
                    }
                    else
                    {
                        Debug.Log("[SpeechToTextHandler] No speech detected in audio.");
                    }
                }
            }
        }

        isTranscribing = false;
        ResetButtonVisualState();
    }

    public bool IsThaiLocale()
    {
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null)
        {
            string code = currentLocale.Identifier.Code;
            if (!string.IsNullOrEmpty(code) && code.StartsWith("th", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        // Fallback to Application system language if localization is not yet ready
        return Application.systemLanguage == SystemLanguage.Thai;
    }

    private void SetButtonVisualState(bool recording, string statusText, bool interactable = true)
    {
        if (speakButton != null)
        {
            speakButton.interactable = interactable;
        }

        if (buttonImage != null)
        {
            buttonImage.color = recording ? recordingButtonColor : originalButtonColor;
        }

        if (speakButtonText != null)
        {
            speakButtonText.text = statusText;
        }
    }

    private void ResetButtonVisualState()
    {
        if (speakButton != null)
        {
            speakButton.interactable = true;
        }

        if (buttonImage != null)
        {
            buttonImage.color = originalButtonColor;
        }

        // If LocalizeStringEvent is attached, refresh to restore the localized string
        if (localizeStringEvent != null)
        {
            localizeStringEvent.RefreshString();
        }
        else if (speakButtonText != null)
        {
            speakButtonText.text = IsThaiLocale() ? "<sprite name=microphone> พูด" : "<sprite=\"microphone\" index=0> Speak";
        }
    }

    private void ShowTemporaryStatus(string message, float duration)
    {
        StartCoroutine(ShowTemporaryStatusRoutine(message, duration));
    }

    private IEnumerator ShowTemporaryStatusRoutine(string message, float duration)
    {
        if (speakButtonText != null)
        {
            speakButtonText.text = message;
        }
        yield return new WaitForSeconds(duration);
        ResetButtonVisualState();
    }

    // Encodes the recorded portion of an AudioClip into a 16-bit Mono PCM WAV byte array
    public static byte[] TrimAndEncodeToWav(AudioClip clip, int totalSamples)
    {
        int channels = clip.channels;
        int sampleRate = clip.frequency;

        // Clip data array
        float[] rawSamples = new float[totalSamples * channels];
        clip.GetData(rawSamples, 0);

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int byteRate = sampleRate * channels * 2;
            int blockAlign = channels * 2;
            int subChunk2Size = rawSamples.Length * 2;
            int chunkSize = 36 + subChunk2Size;

            // RIFF chunk descriptor
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // "fmt " sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size (16 for PCM)
            writer.Write((short)1); // AudioFormat (1 for PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16); // BitsPerSample (16-bit)

            // "data" sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            // Audio samples conversion (float to 16-bit PCM short)
            for (int i = 0; i < rawSamples.Length; i++)
            {
                short sampleShort = (short)Mathf.Clamp(rawSamples[i] * 32767f, -32768f, 32767f);
                writer.Write(sampleShort);
            }

            return stream.ToArray();
        }
    }
}

// Data structures for Gemini Multimodal Audio API
[Serializable]
public class GeminiAudioRequest
{
    public GeminiAudioContent[] contents;
    public GeminiAudioGenConfig generationConfig;
}

[Serializable]
public class GeminiAudioContent
{
    public GeminiAudioPart[] parts;
}

[Serializable]
public class GeminiAudioPart
{
    public GeminiInlineData inlineData;
    public string text;
}

[Serializable]
public class GeminiInlineData
{
    public string mimeType;
    public string data;
}

[Serializable]
public class GeminiAudioGenConfig
{
    public float temperature;
    public int maxOutputTokens;
}

[Serializable]
public class GeminiAudioResponse
{
    public GeminiAudioCandidate[] candidates;
}

[Serializable]
public class GeminiAudioCandidate
{
    public GeminiAudioCandidateContent content;
}

[Serializable]
public class GeminiAudioCandidateContent
{
    public GeminiAudioCandidatePart[] parts;
}

[Serializable]
public class GeminiAudioCandidatePart
{
    public string text;
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpeechToTextHandler))]
public class SpeechToTextHandlerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        SpeechToTextHandler handler = (SpeechToTextHandler)target;
        string effectiveKey = !string.IsNullOrWhiteSpace(handler.apiKey) ? handler.apiKey : (handler.geminiChatUI != null ? handler.geminiChatUI.apiKey : "");
        SerializedProperty modelProp = serializedObject.FindProperty("modelName");

        GUILayout.Space(10);
        GeminiModelPicker.DrawModelSelector(serializedObject, modelProp, effectiveKey, "STT Model");

        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(15);
        GUIStyle boldBtn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

        if (!handler.IsRecording)
        {
            if (GUILayout.Button("Start Voice Recording", boldBtn, GUILayout.Height(35)))
            {
                if (Application.isPlaying)
                {
                    handler.StartRecording();
                }
                else
                {
                    Debug.LogWarning("[SpeechToTextHandler] Enter Play Mode to test microphone recording.");
                }
            }
        }
        else
        {
            if (GUILayout.Button("Stop Recording & Transcribe", boldBtn, GUILayout.Height(35)))
            {
                handler.StopRecordingAndTranscribe();
            }
        }
    }
}
#endif
