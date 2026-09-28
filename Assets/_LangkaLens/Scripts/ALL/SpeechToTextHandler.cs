using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Components;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class SpeechToTextHandler : MonoBehaviour
{
    public const string GroqApiKeyPrefKey = "LangkaLens_GroqApiKey";
    private const string GroqEndpoint = "https://api.groq.com/openai/v1/audio/transcriptions";

    public enum TranscriptionLanguageMode
    {
        [Tooltip("Automatically pass language code ('th' or 'en') to Whisper based on LocalizationSettings.SelectedLocale")]
        MatchCurrentLocale,
        [Tooltip("Do not specify language; let Whisper auto-detect language from audio")]
        AutoDetectByAudio,
        [Tooltip("Always force a specific ISO language code (e.g. 'th', 'en')")]
        ExplicitLanguage
    }

    [Header("UI References")]
    [Tooltip("Target input field where the transcribed text will be placed.")]
    public TMP_InputField targetInputField;

    [Tooltip("Button used to start and stop speech recording.")]
    public Button speakButton;

    [Tooltip("Optional: Text component of the Speak Button. Auto-detected from speakButton if not assigned.")]
    public TMP_Text speakButtonText;

    [Tooltip("Reference to GeminiChatUI (for chat interaction).")]
    public GeminiChatUI geminiChatUI;

    [Header("Groq Whisper API Configuration")]
    [Tooltip("Groq API Key (starts with gsk_...). If empty, checks PlayerPrefs.")]
    // This is a different provider's credential, not a rename of Gemini's apiKey.
    // Migrating apiKey also migrates old scene overrides over the valid prefab key.
    public string groqApiKey = "";

    [Tooltip("Groq Whisper model for audio transcription.")]
    public string modelName = "whisper-large-v3";

    [Header("Locale & Language Synchronization")]
    [Tooltip("How Whisper language should be determined. MatchCurrentLocale uses the active Unity Localization locale.")]
    public TranscriptionLanguageMode languageMode = TranscriptionLanguageMode.MatchCurrentLocale;

    [Tooltip("Explicit language code used only when languageMode is set to ExplicitLanguage (e.g. 'th', 'en').")]
    public string explicitLanguageCode = "th";

    [Header("Domain Vocabulary Hints (Whisper Prompt)")]
    [Tooltip("Context prompt passed to Whisper when transcribing in Thai. Helps with historical/cultural terms.")]
    [TextArea(2, 3)]
    public string thaiPromptContext = "ข้อความภาษาไทย เมืองโบราณยะรัง ลังกาสุกะ โบราณคดี ปัตตานี ประวัติศาสตร์ โบราณวัตถุ พิพิธภัณฑ์";

    [Tooltip("Context prompt passed to Whisper when transcribing in English.")]
    [TextArea(2, 3)]
    public string englishPromptContext = "English transcription regarding Yarang ancient city, Langkasuka archaeological site, museum history and artifacts.";

    [Tooltip("Fallback context prompt when language is auto-detected or other locales are active.")]
    [TextArea(2, 3)]
    public string defaultPromptContext = "Thai and English transcription / ข้อความภาษาไทยและภาษาอังกฤษ, Langkasuka, Yarang";

    [Header("Recording Settings")]
    [Tooltip("Maximum recording duration in seconds before automatically stopping and transcribing.")]
    [Range(3, 30)]
    public int maxRecordingDuration = 15;

    [Tooltip("Audio sample rate for microphone recording (16000 Hz optimal for Whisper).")]
    public int sampleRate = 16000;

    [Tooltip("Button tint color applied while recording is active.")]
    public Color recordingButtonColor = new Color(0.92f, 0.3f, 0.25f, 1f);

    [Tooltip("Optional explicit text color when the button is disabled/transcribing. If alpha is 0, auto-calculates from button disabled color.")]
    public Color disabledTextColor = new Color(0, 0, 0, 0);

    [Header("State (Read Only)")]
    [SerializeField] private bool isRecording = false;
    [SerializeField] private bool isTranscribing = false;

    // Internal state cache
    private AudioClip recordingClip;
    private string microphoneDevice;
    private Coroutine recordingCoroutine;
    private Coroutine statusCoroutine;
    private UnityWebRequest activeRequest;
    private int lastRecordingPosition;
    private Color originalButtonColor = Color.white;
    private Color originalTextColor = Color.white;
    private Color originalIconColor = Color.white;
    private Image buttonImage;
    private Image iconImage;
    private LocalizeStringEvent localizeStringEvent;

    public bool IsRecording => isRecording;
    public bool IsTranscribing => isTranscribing;

    public string EffectiveApiKey
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(groqApiKey) && groqApiKey != "YOUR_GROQ_API_KEY_HERE" && groqApiKey != "YOUR_API_KEY_HERE")
            {
                return groqApiKey.Trim();
            }

            string prefKey = PlayerPrefs.GetString(GroqApiKeyPrefKey, "");
            if (!string.IsNullOrWhiteSpace(prefKey))
            {
                return prefKey.Trim();
            }

            return "";
        }
    }

    public string EffectiveModelName
    {
        get
        {
            if (string.IsNullOrWhiteSpace(modelName) || modelName.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
            {
                return "whisper-large-v3";
            }
            return modelName.Trim();
        }
    }

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(modelName) || modelName.StartsWith("gemini", StringComparison.OrdinalIgnoreCase))
        {
            modelName = "whisper-large-v3";
        }

        InitializeComponents();
    }

    private void OnEnable()
    {
        // Listen for runtime locale changes (e.g. from LanguageToggler)
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        ResetButtonVisualState();
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        StopAllCoroutines();
        recordingCoroutine = null;
        statusCoroutine = null;
        activeRequest?.Abort();
        activeRequest?.Dispose();
        activeRequest = null;
        if (isRecording) StopMicrophone();
        ReleaseRecordingClip();
        isTranscribing = false;
        ResetButtonVisualState();
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

    private void OnSelectedLocaleChanged(Locale locale)
    {
        if (!isRecording && !isTranscribing)
        {
            ResetButtonVisualState();
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

            Transform iconTransform = speakButton.transform.Find("Image");
            if (iconTransform != null)
            {
                iconImage = iconTransform.GetComponent<Image>();
                if (iconImage != null)
                {
                    originalIconColor = iconImage.color;
                }
            }

            if (speakButtonText == null)
            {
                speakButtonText = speakButton.GetComponentInChildren<TMP_Text>();
            }

            if (speakButtonText != null)
            {
                originalTextColor = speakButtonText.color;
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
        if (!isActiveAndEnabled || isRecording || isTranscribing) return;
        CancelTemporaryStatus();
        if (targetInputField == null)
        {
            Debug.LogError("[SpeechToTextHandler] stage=input missing target field.");
            ShowTemporaryStatus(IsThaiLocale() ? "ไม่พบช่องข้อความ" : "Missing Input Field", 3f);
            return;
        }
        string key = EffectiveApiKey;
        if (string.IsNullOrWhiteSpace(key) || !key.StartsWith("gsk_", StringComparison.Ordinal))
        {
            Debug.LogError("[SpeechToTextHandler] stage=config missing or invalid Groq credential. Check scene overrides; Editor PlayerPrefs do not transfer to Quest.");
            ShowTemporaryStatus(IsThaiLocale() ? "ตรวจสอบ Groq API Key" : "Check Groq API Key", 3f);
            return;
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(UnityEngine.Android.Permission.Microphone))
        {
            Debug.LogWarning("[SpeechToTextHandler] stage=permission microphone permission not granted.");
            CheckMicrophonePermission();
            ShowTemporaryStatus(IsThaiLocale() ? "อนุญาตไมโครโฟน แล้วกดอีกครั้ง" : "Allow Microphone, Then Tap Again", 3f);
            return;
        }
#endif
        CheckMicrophonePermission();

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[SpeechToTextHandler] No microphone audio input devices found!");
            ShowTemporaryStatus(IsThaiLocale() ? "ไม่พบไมโครโฟน" : "No Microphone Found", 2f);
            return;
        }

        microphoneDevice = null; // Default device
        ReleaseRecordingClip();
        lastRecordingPosition = 0;

        // Leave headroom so the non-looping buffer has not expired at the UI timeout.
        // Poll its position using real time; timeScale can be zero in a VR menu.
        try
        {
            recordingClip = Microphone.Start(microphoneDevice, false,
                Mathf.Clamp(maxRecordingDuration, 3, 30) + 2, sampleRate);
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SpeechToTextHandler] stage=recording start failed ({exception.GetType().Name}).");
        }
        if (recordingClip == null)
        {
            ShowTemporaryStatus(IsThaiLocale() ? "เปิดไมโครโฟนไม่ได้" : "Microphone Start Failed", 3f);
            return;
        }
        isRecording = true;

        // Visual feedback for recording
        SetButtonVisualState(true, IsThaiLocale() ? "กำลังฟัง..." : "Listening...");

        if (recordingCoroutine != null) StopCoroutine(recordingCoroutine);
        recordingCoroutine = StartCoroutine(RecordingTimeoutRoutine());

        Debug.Log($"[SpeechToTextHandler] Microphone recording started. (Target Language: {GetEffectiveLanguageCode() ?? "Auto-Detect"})");
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
        recordingPosition = Mathf.Max(recordingPosition, lastRecordingPosition);
        StopMicrophone();

        if (recordingClip == null || recordingPosition <= 0)
        {
            Debug.LogWarning("[SpeechToTextHandler] stage=recording captured no audio samples.");
            ReleaseRecordingClip();
            ShowTemporaryStatus(IsThaiLocale() ? "ไม่ได้รับเสียง" : "No Audio Captured", 3f);
            return;
        }

        byte[] wavBytes;
        try
        {
            wavBytes = TrimAndEncodeToWav(recordingClip, Mathf.Min(recordingPosition, recordingClip.samples));
        }
        catch (Exception exception)
        {
            Debug.LogError($"[SpeechToTextHandler] stage=wav encoding failed ({exception.GetType().Name}).");
            ReleaseRecordingClip();
            ShowTemporaryStatus(IsThaiLocale() ? "อ่านเสียงไม่ได้" : "Audio Encoding Failed", 3f);
            return;
        }
        ReleaseRecordingClip();
        // Measure encoded PCM without logging or retaining the recording itself.
        double sumSquares = 0;
        int peak = 0;
        for (int i = 44; i < wavBytes.Length; i += 2)
        {
            int sample = (short)(wavBytes[i] | (wavBytes[i + 1] << 8));
            peak = Math.Max(peak, Math.Abs(sample));
            sumSquares += (double)sample * sample;
        }
        double rms = Math.Sqrt(sumSquares / ((wavBytes.Length - 44) / 2)) / 32768d;
        Debug.Log($"[SpeechToTextHandler] stage=wav frames={recordingPosition} bytes={wavBytes.Length} peak={peak / 32768f:F5} rms={rms:F5}");
        if (peak == 0)
        {
            ShowTemporaryStatus(IsThaiLocale() ? "ไม่ได้รับเสียง ตรวจสอบไมโครโฟน" : "Silent Audio: Check Microphone", 3f);
            return;
        }
        StartCoroutine(TranscribeAudioRoutine(wavBytes));
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
        float startedAt = Time.realtimeSinceStartup;
        while (isRecording)
        {
            int position = Microphone.GetPosition(microphoneDevice);
            lastRecordingPosition = Mathf.Max(lastRecordingPosition, position);
            float elapsed = Time.realtimeSinceStartup - startedAt;
            if (elapsed >= Mathf.Clamp(maxRecordingDuration, 3, 30) ||
                (elapsed > 2f && (lastRecordingPosition == 0 || !Microphone.IsRecording(microphoneDevice))))
            {
                recordingCoroutine = null;
                StopRecordingAndTranscribe();
                yield break;
            }
            yield return null;
        }
    }

    private void ReleaseRecordingClip()
    {
        if (recordingClip != null) Destroy(recordingClip);
        recordingClip = null;
    }

    private IEnumerator TranscribeAudioRoutine(byte[] wavBytes)
    {
        isTranscribing = true;

        bool isThai = IsThaiLocale();
        SetButtonVisualState(false, isThai ? "กำลังถอดความ..." : "Transcribing...", interactable: false);

        string effectiveKey = EffectiveApiKey;
        if (string.IsNullOrWhiteSpace(effectiveKey))
        {
            Debug.LogError("[SpeechToTextHandler] Groq API key is missing! Please configure groqApiKey in the Inspector or PlayerPrefs.");
            isTranscribing = false;
            ShowTemporaryStatus(isThai ? "ไม่มี Groq API Key" : "Missing Groq API Key", 2.5f);
            yield break;
        }

        string languageCode = GetEffectiveLanguageCode();
        string promptHint = GetEffectivePromptContext(languageCode);

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", wavBytes, "audio.wav", "audio/wav");
        form.AddField("model", EffectiveModelName);
        form.AddField("response_format", "json");

        // Explicitly set language based on locale if available
        if (!string.IsNullOrEmpty(languageCode))
        {
            form.AddField("language", languageCode);
        }

        // Add domain vocabulary prompt to help Whisper
        if (!string.IsNullOrWhiteSpace(promptHint))
        {
            form.AddField("prompt", promptHint);
        }

        Debug.Log($"[SpeechToTextHandler] Sending audio to Groq Whisper. Language: '{(languageCode ?? "auto-detect")}', Model: '{EffectiveModelName}'");

        using (UnityWebRequest request = UnityWebRequest.Post(GroqEndpoint, form))
        {
            activeRequest = request;
            request.SetRequestHeader("Authorization", "Bearer " + effectiveKey);
            request.timeout = 30;
            try
            {
                yield return request.SendWebRequest();
                Debug.Log($"[SpeechToTextHandler] stage=http status={request.responseCode} result={request.result}");

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // Only log allow-listed error categories. Server bodies can echo credentials.
                    Debug.LogError($"[SpeechToTextHandler] stage=http Groq HTTP {request.responseCode}; {GetHttpFailureCategory(request.responseCode)}. Response body withheld.");
                    ShowTemporaryStatus(isThai ? $"ถอดความล้มเหลว ({request.responseCode})" : $"{GetHttpFailureCategory(request.responseCode)} ({request.responseCode})", 4f);
                }
                else
                {
                    string transcription = null;
                    bool parsed = false;
                    try
                    {
                        transcription = ParseTranscript(request.downloadHandler.text);
                        parsed = true;
                    }
                    catch (Exception exception)
                    {
                        Debug.LogError($"[SpeechToTextHandler] stage=parse invalid response ({exception.GetType().Name}).");
                        ShowTemporaryStatus(isThai ? "อ่านผลลัพธ์ไม่ได้" : "Invalid Transcription Response", 3f);
                    }
                    if (parsed)
                    {
                        if (string.IsNullOrWhiteSpace(transcription))
                        {
                            Debug.LogWarning("[SpeechToTextHandler] stage=parse empty transcript.");
                            ShowTemporaryStatus(isThai ? "ไม่พบคำพูด" : "No Speech Detected", 3f);
                        }
                        else if (!ApplyTranscript(transcription))
                        {
                            Debug.LogError("[SpeechToTextHandler] stage=input target field missing.");
                            ShowTemporaryStatus(isThai ? "ไม่พบช่องข้อความ" : "Missing Input Field", 3f);
                        }
                        else
                        {
                            Debug.Log($"[SpeechToTextHandler] stage=input applied transcript characters={transcription.Length}");
                        }
                    }
                }
            }
            finally
            {
                activeRequest = null;
                isTranscribing = false;
                if (statusCoroutine == null) ResetButtonVisualState();
            }
        }
    }

    private static string GetHttpFailureCategory(long code)
    {
        switch (code)
        {
            case 0: return "Network or Timeout";
            case 400: return "Invalid Audio Request";
            case 401: return "Invalid Groq API Key";
            case 403: return "Groq Access Denied";
            case 404: return "Model Not Found";
            case 413: return "Audio Too Large";
            case 429: return "Groq Rate Limit";
            default: return "Transcription Failed";
        }
    }

    private static string ParseTranscript(string json)
    {
        GroqWhisperResponse response = JsonUtility.FromJson<GroqWhisperResponse>(json);
        if (response == null || response.text == null)
            throw new FormatException("Missing transcript field.");
        return response.text.Trim();
    }

    private bool ApplyTranscript(string transcription)
    {
        if (targetInputField == null) return false;
        // TMP remains the sole owner of native keyboard synchronization.
        targetInputField.text = string.IsNullOrWhiteSpace(targetInputField.text)
            ? transcription : targetInputField.text + " " + transcription;
        targetInputField.caretPosition = targetInputField.text.Length;
        return true;
    }

    /// <summary>
    /// Resolves the 2-letter ISO 639-1 language code based on current configuration and locale state.
    /// Returns null if language should be auto-detected by Whisper.
    /// </summary>
    public string GetEffectiveLanguageCode()
    {
        switch (languageMode)
        {
            case TranscriptionLanguageMode.AutoDetectByAudio:
                return null;

            case TranscriptionLanguageMode.ExplicitLanguage:
                return !string.IsNullOrWhiteSpace(explicitLanguageCode) ? explicitLanguageCode.Trim().ToLowerInvariant() : null;

            case TranscriptionLanguageMode.MatchCurrentLocale:
            default:
                return GetCurrentLocaleLanguageCode();
        }
    }

    /// <summary>
    /// Extracts the ISO 639-1 code (e.g. 'th' or 'en') from LocalizationSettings.SelectedLocale.
    /// </summary>
    public string GetCurrentLocaleLanguageCode()
    {
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null && !string.IsNullOrEmpty(currentLocale.Identifier.Code))
        {
            string code = currentLocale.Identifier.Code.Trim();
            // Handle variants like "th-TH" -> "th", "en-US" -> "en"
            int separatorIdx = code.IndexOfAny(new char[] { '-', '_' });
            if (separatorIdx > 0)
            {
                code = code.Substring(0, separatorIdx);
            }
            return code.ToLowerInvariant();
        }

        // Fallback to Application system language if localization is initializing
        return Application.systemLanguage == SystemLanguage.Thai ? "th" : "en";
    }

    public string GetEffectivePromptContext(string languageCode)
    {
        if (string.Equals(languageCode, "th", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(thaiPromptContext) ? thaiPromptContext : defaultPromptContext;
        }
        else if (string.Equals(languageCode, "en", StringComparison.OrdinalIgnoreCase))
        {
            return !string.IsNullOrWhiteSpace(englishPromptContext) ? englishPromptContext : defaultPromptContext;
        }

        return defaultPromptContext;
    }

    public bool IsThaiLocale()
    {
        return string.Equals(GetCurrentLocaleLanguageCode(), "th", StringComparison.OrdinalIgnoreCase);
    }

    private void SetButtonVisualState(bool recording, string statusText, bool interactable = true)
    {
        if (buttonImage != null)
        {
            buttonImage.color = recording ? recordingButtonColor : originalButtonColor;
        }

        if (speakButton != null)
        {
            speakButton.interactable = interactable;
        }

        // Temporarily disable localization auto-refresh when displaying custom status/transcribing text
        if (localizeStringEvent != null && (!interactable || recording || statusCoroutine != null))
        {
            localizeStringEvent.enabled = false;
        }

        if (speakButtonText != null)
        {
            speakButtonText.text = statusText;
            speakButtonText.color = interactable ? originalTextColor : GetEffectiveDisabledTextColor();
        }

        if (iconImage != null)
        {
            iconImage.color = interactable ? originalIconColor : GetEffectiveDisabledIconColor();
        }
    }

    private void ResetButtonVisualState()
    {
        if (buttonImage != null)
        {
            buttonImage.color = originalButtonColor;
        }

        if (speakButton != null)
        {
            speakButton.interactable = true;
        }

        if (speakButtonText != null)
        {
            speakButtonText.color = originalTextColor;
        }

        if (iconImage != null)
        {
            iconImage.color = originalIconColor;
        }

        if (localizeStringEvent != null)
        {
            localizeStringEvent.enabled = true;
            localizeStringEvent.RefreshString();
        }
        else if (speakButtonText != null)
        {
            speakButtonText.text = IsThaiLocale() ? "พูด" : "Speak";
        }
    }

    private Color GetEffectiveDisabledTextColor()
    {
        if (disabledTextColor.a > 0.001f)
        {
            return disabledTextColor;
        }

        Color mult = (speakButton != null) ? speakButton.colors.disabledColor : new Color(0.784f, 0.784f, 0.784f, 0.5f);
        return new Color(
            originalTextColor.r * mult.r,
            originalTextColor.g * mult.g,
            originalTextColor.b * mult.b,
            originalTextColor.a * mult.a
        );
    }

    private Color GetEffectiveDisabledIconColor()
    {
        Color mult = (speakButton != null) ? speakButton.colors.disabledColor : new Color(0.784f, 0.784f, 0.784f, 0.5f);
        return new Color(
            originalIconColor.r * mult.r,
            originalIconColor.g * mult.g,
            originalIconColor.b * mult.b,
            originalIconColor.a * mult.a
        );
    }

    private void ShowTemporaryStatus(string message, float duration)
    {
        CancelTemporaryStatus();
        statusCoroutine = StartCoroutine(ShowTemporaryStatusRoutine(message, duration));
    }

    private void CancelTemporaryStatus()
    {
        if (statusCoroutine != null) StopCoroutine(statusCoroutine);
        statusCoroutine = null;
    }

    private IEnumerator ShowTemporaryStatusRoutine(string message, float duration)
    {
        SetButtonVisualState(false, message);
        yield return new WaitForSecondsRealtime(duration);
        statusCoroutine = null;
        if (!isRecording && !isTranscribing) ResetButtonVisualState();
    }

    public static byte[] TrimAndEncodeToWav(AudioClip clip, int totalSamples)
    {
        if (clip == null) throw new ArgumentNullException(nameof(clip));
        if (totalSamples <= 0 || totalSamples > clip.samples)
            throw new ArgumentOutOfRangeException(nameof(totalSamples));
        int channels = clip.channels;
        int sampleRate = clip.frequency;

        float[] rawSamples = new float[totalSamples * channels];
        if (!clip.GetData(rawSamples, 0))
            throw new InvalidOperationException("Cannot read recorded PCM data.");

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int byteRate = sampleRate * channels * 2;
            int blockAlign = channels * 2;
            int subChunk2Size = rawSamples.Length * 2;
            int chunkSize = 36 + subChunk2Size;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1); // PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)16);

            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            for (int i = 0; i < rawSamples.Length; i++)
            {
                short sampleShort = (short)Mathf.Clamp(rawSamples[i] * 32767f, -32768f, 32767f);
                writer.Write(sampleShort);
            }

            return stream.ToArray();
        }
    }

    [Serializable]
    private class GroqWhisperResponse
    {
        public string text;
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(SpeechToTextHandler))]
public class SpeechToTextHandlerEditor : Editor
{
    private static readonly string[] GroqWhisperModels = new string[]
    {
        "whisper-large-v3",
        "whisper-large-v3-turbo"
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawDefaultInspector();

        SpeechToTextHandler handler = (SpeechToTextHandler)target;
        SerializedProperty modelProp = serializedObject.FindProperty("modelName");
        SerializedProperty keyProp = serializedObject.FindProperty("groqApiKey");

        GUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Groq Whisper Configuration & Locale Status", EditorStyles.boldLabel);

        // Display live locale info
        string activeLocaleCode = handler.GetCurrentLocaleLanguageCode();
        string effectiveLangCode = handler.GetEffectiveLanguageCode();
        string localeName = LocalizationSettings.SelectedLocale != null
            ? LocalizationSettings.SelectedLocale.LocaleName
            : $"System ({activeLocaleCode})";

        EditorGUILayout.HelpBox(
            $"🌐 Current Locale: {localeName}\n" +
            $"🎯 Active Whisper Target Language: '{(effectiveLangCode ?? "Auto-Detect")}'\n" +
            (effectiveLangCode == "th" ? "🇹🇭 Constraining Whisper decoder to Thai tokens for maximum accuracy." :
             effectiveLangCode == "en" ? "🇬🇧 Constraining Whisper decoder to English tokens." : "Auto-detecting language per audio."),
            MessageType.Info);

        // Model selector popup
        string currentModel = modelProp.stringValue;
        int selectedIndex = Array.IndexOf(GroqWhisperModels, currentModel);
        bool isCustom = selectedIndex < 0;

        List<string> options = new List<string>(GroqWhisperModels) { "[Custom Model...]" };
        int popupIndex = isCustom ? options.Count - 1 : selectedIndex;

        EditorGUI.BeginChangeCheck();
        int newPopupIndex = EditorGUILayout.Popup("Whisper Model", popupIndex, options.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (newPopupIndex >= 0 && newPopupIndex < GroqWhisperModels.Length)
            {
                modelProp.stringValue = GroqWhisperModels[newPopupIndex];
            }
        }

        if (newPopupIndex == options.Count - 1)
        {
            EditorGUI.indentLevel++;
            modelProp.stringValue = EditorGUILayout.TextField("Custom Model", modelProp.stringValue);
            EditorGUI.indentLevel--;
        }

        // Quick button to save key to PlayerPrefs
        if (!string.IsNullOrWhiteSpace(keyProp.stringValue))
        {
            if (GUILayout.Button("💾 Remember API Key Globally (PlayerPrefs)", GUILayout.Height(24)))
            {
                PlayerPrefs.SetString(SpeechToTextHandler.GroqApiKeyPrefKey, keyProp.stringValue.Trim());
                PlayerPrefs.Save();
                EditorUtility.DisplayDialog("Key Saved", "Groq API Key saved to this Editor's PlayerPrefs. These preferences are not included in the Quest APK. PlayerPrefs is not secure secret storage.", "OK");
            }
        }

        // Apply to Prefab if needed
        UnityEngine.Object targetObj = serializedObject.targetObject;
        if (targetObj != null && PrefabUtility.IsPartOfPrefabInstance(targetObj))
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("💾 Apply Settings to Prefab Asset (Propagate to Scenes)", GUILayout.Height(24)))
            {
                serializedObject.ApplyModifiedProperties();
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    PrefabUtility.ApplyPropertyOverride(modelProp, assetPath, InteractionMode.UserAction);
                    if (!string.IsNullOrWhiteSpace(keyProp.stringValue))
                    {
                        PrefabUtility.ApplyPropertyOverride(keyProp, assetPath, InteractionMode.UserAction);
                    }
                    EditorUtility.DisplayDialog("Applied to Prefab", $"Settings applied to prefab asset:\n{assetPath}", "OK");
                }
            }
        }

        EditorGUILayout.EndVertical();
        serializedObject.ApplyModifiedProperties();

        GUILayout.Space(15);
        GUIStyle boldBtn = new GUIStyle(GUI.skin.button) { fontStyle = FontStyle.Bold };

        if (!handler.IsRecording)
        {
            if (GUILayout.Button("🎤 Start Voice Recording", boldBtn, GUILayout.Height(35)))
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
            if (GUILayout.Button("⏹ Stop Recording & Transcribe", boldBtn, GUILayout.Height(35)))
            {
                handler.StopRecordingAndTranscribe();
            }
        }
    }
}
#endif
