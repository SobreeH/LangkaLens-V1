# Runs the production request coroutine against an in-memory HTTP double.
# No Unity installation, credentials, or network access is used.
$ErrorActionPreference = 'Stop'
$repo = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$source = Get-Content (Join-Path $repo 'Assets/_LangkaLens/Mixed/Scripts/ALL/GeminiChatUI.cs') -Raw
$start = $source.IndexOf('    private IEnumerator SendRequest(string prompt)')
$end = $source.IndexOf('    // Forces the Scroll View', $start)
if ($start -lt 0 -or $end -lt 0) { throw 'Request coroutine could not be located.' }
$methods = $source.Substring($start, $end - $start)
$dataStart = $source.IndexOf('[Serializable] public class GeminiRequest')
$dataEnd = $source.IndexOf('#if UNITY_EDITOR', $dataStart)
$types = $source.Substring($dataStart, $dataEnd - $dataStart)
$prefix = @'
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
public sealed class ChatFallbackHarness {
    public string modelName = "main", fallbackModelName = "backup", apiKey = "test-only";
    public bool enableFallback = true, awaitingResponse, usingFallback, pending = true;
    public int maxOutputTokens = 150;
    public UnityWebRequest currentWebRequest;
    public object currentRequestCoroutine;
    public TextStub chatLogText = new TextStub();
    public bool backupStatusShown;
    string GetEffectiveApiKey() { return apiKey; }
    string GetLocalizedSystemPrompt() { return "Test guide instructions"; }
    bool IsThaiLocale() { return false; }
    string GetSenderGuideLabel() { return "Guide"; }
    void ShowRequestStatus() { if(usingFallback) backupStatusShown = true; }
    void RemovePendingStatus() {}
    void ScrollToBottom() {}
    void SetRequestPending(bool value) { pending = value; }
    public IEnumerator Begin() { return SendRequest("test question"); }
'@
$suffix = @'
}
public class TextStub { public string text = "User question"; }
public static class Debug {
    public static int errors;
    public static void Log(string text) {}
    public static void LogWarning(string text) {}
    public static void LogError(string text) { errors++; }
}
public static class JsonUtility {
    public static GeminiResponse response;
    public static string ToJson(object value) { return "identical-payload"; }
    public static T FromJson<T>(string value) { return (T)(object)response; }
}
public class UploadHandlerRaw {
    public byte[] data;
    public UploadHandlerRaw(byte[] value) { data = value; }
}
public class DownloadHandlerBuffer { public string text = "test response"; }
public class Operation { public bool isDone; }
public class UnityWebRequest : IDisposable {
    public enum Result { Success, ProtocolError, ConnectionError }
    public static Queue<long> statuses = new Queue<long>();
    public static List<string> urls = new List<string>(), bodies = new List<string>();
    public static int alive;
    public long responseCode;
    public Result result;
    public string error;
    public int timeout;
    public float uploadProgress = 1f;
    public UploadHandlerRaw uploadHandler;
    public DownloadHandlerBuffer downloadHandler;
    bool disposed;
    public UnityWebRequest(string url, string method) {
        if(alive != 0) throw new Exception("Overlapping requests or undisposed primary");
        alive++; urls.Add(url);
        responseCode = statuses.Dequeue();
        result = responseCode == 200 ? Result.Success :
            responseCode <= 0 ? Result.ConnectionError : Result.ProtocolError;
        error = "simulated failure";
    }
    public void SetRequestHeader(string key, string value) {}
    public Operation SendWebRequest() {
        bodies.Add(Encoding.UTF8.GetString(uploadHandler.data));
        return new Operation { isDone = responseCode != -1 };
    }
    public void Dispose() { if(!disposed) { disposed = true; alive--; } }
}
public static class FallbackTests {
    static int passed;
    static void Check(bool value, string message) { if(!value) throw new Exception(message); }
    static void Reset(params long[] statuses) {
        UnityWebRequest.statuses = new Queue<long>(statuses);
        UnityWebRequest.urls.Clear(); UnityWebRequest.bodies.Clear();
        UnityWebRequest.alive = 0; Debug.errors = 0;
        JsonUtility.response = new GeminiResponse { candidates = new[] {
            new Candidate { content = new GeminiResponseContent {
                parts = new[] { new GeminiResponsePart { text = "answer" } }
            }}
        }};
    }
    static void Run(ChatFallbackHarness chat) {
        var routine = chat.Begin();
        while(routine.MoveNext()) throw new Exception("Unexpected pending request");
        Check(!chat.pending && chat.currentWebRequest == null, "Busy state not reset");
        Check(UnityWebRequest.alive == 0, "Request was not disposed");
    }
    public static string Execute() {
        Reset(200); var chat = new ChatFallbackHarness(); Run(chat);
        Check(UnityWebRequest.urls.Count == 1 && !chat.backupStatusShown, "Success triggered fallback"); passed++;
        foreach(long status in new long[]{404,408,429,500,502,503,504}) {
            Reset(status,200); chat = new ChatFallbackHarness(); Run(chat);
            Check(UnityWebRequest.urls.Count == 2 && UnityWebRequest.urls[1].Contains("/backup:"), "Wrong fallback target");
            Check(chat.backupStatusShown && Debug.errors == 0, "Recovered error shown as final failure");
            Check(UnityWebRequest.bodies[0] == UnityWebRequest.bodies[1], "Prompt changed on fallback");
            Check(chat.chatLogText.text == "User question\n\n<color=#00FFAA><b>Guide:</b></color> answer", "Duplicate chat entry"); passed++;
        }
        foreach(long status in new long[]{0,400,401,402,403}) {
            Reset(status); chat = new ChatFallbackHarness(); Run(chat);
            Check(UnityWebRequest.urls.Count == 1 && Debug.errors == 1, "Non-recoverable error retried"); passed++;
        }
        Reset(503,503); chat = new ChatFallbackHarness(); Run(chat);
        Check(UnityWebRequest.urls.Count == 2 && Debug.errors == 1, "Attempts not bounded or missing final error"); passed++;
        Reset(503); chat = new ChatFallbackHarness { enableFallback = false }; Run(chat);
        Check(UnityWebRequest.urls.Count == 1, "Disabled fallback used"); passed++;
        foreach(string backup in new[]{"", "  ", "models/main", " MAIN "}) {
            Reset(503); chat = new ChatFallbackHarness { fallbackModelName = backup }; Run(chat);
            Check(UnityWebRequest.urls.Count == 1, "Blank or duplicate model retried"); passed++;
        }
        Reset(503,200); chat = new ChatFallbackHarness { fallbackModelName = " models/backup " }; Run(chat);
        Check(UnityWebRequest.urls[1].Contains("/models/backup:"), "Model name not normalized"); passed++;
        Reset(200); JsonUtility.response = new GeminiResponse { promptFeedback = new GeminiPromptFeedback { blockReason="SAFETY" } };
        chat = new ChatFallbackHarness(); Run(chat);
        Check(UnityWebRequest.urls.Count == 1, "Blocked response retried"); passed++;
        foreach(var sequence in new[]{new long[]{-1}, new long[]{503,-1}}) {
            Reset(sequence); chat = new ChatFallbackHarness(); var routine = chat.Begin();
            Check(routine.MoveNext() && chat.pending, "Request did not stay pending");
            ((IDisposable)routine).Dispose();
            Check(!chat.pending && UnityWebRequest.alive == 0 && chat.currentWebRequest == null, "Cancellation cleanup failed"); passed++;
        }
        return "PASS: " + passed + " offline coroutine scenarios; no network requests.";
    }
}
'@
Add-Type -TypeDefinition ($prefix + $methods + $suffix + $types)
[FallbackTests]::Execute()
