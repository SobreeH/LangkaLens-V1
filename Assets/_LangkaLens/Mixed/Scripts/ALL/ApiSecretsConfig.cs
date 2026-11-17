using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Loads local API keys and service tokens from gitignored configuration instead of serialized scene/prefab fields.
/// Resolution order:
/// 1. Explicit non-placeholder Inspector override (for temporary local testing only).
/// 2. Assets/Resources/LangkaLensSecrets.json (gitignored; bundled into builds by Unity Resources).
/// 3. Project root .env file (Editor/Desktop convenience; gitignored).
/// 4. Process environment variables (GEMINI_API_KEY, GROQ_API_KEY, etc.).
/// </summary>
public static class ApiSecretsConfig
{
    public const string ResourceName = "LangkaLensSecrets";

    [Serializable]
    public class SecretsPayload
    {
        public string geminiApiKey = "";
        public string groqApiKey = "";
        public string elevenLabsApiKey = "";
        public string devAgentAccessToken = "";
    }

    private static SecretsPayload cachedPayload;
    private static Dictionary<string, string> cachedDotEnv;
    private static bool loaded;

    public static void Reload()
    {
        cachedPayload = LoadJsonSecrets();
        cachedDotEnv = LoadDotEnv();
        loaded = true;
    }

    public static string GetGeminiApiKey(string inspectorOverride = null)
    {
        return Resolve(inspectorOverride, p => p.geminiApiKey, "GEMINI_API_KEY", "YOUR_API_KEY_HERE");
    }

    public static string GetGroqApiKey(string inspectorOverride = null)
    {
        return Resolve(inspectorOverride, p => p.groqApiKey, "GROQ_API_KEY", "YOUR_GROQ_API_KEY_HERE", "YOUR_API_KEY_HERE");
    }

    public static string GetElevenLabsApiKey(string inspectorOverride = null)
    {
        return Resolve(inspectorOverride, p => p.elevenLabsApiKey, "ELEVENLABS_API_KEY", "YOUR_API_KEY_HERE");
    }

    public static string GetDevAgentAccessToken(string inspectorOverride = null)
    {
        return Resolve(inspectorOverride, p => p.devAgentAccessToken, "DEV_AGENT_ACCESS_TOKEN");
    }

    public static string GetCredentialSource(
        string inspectorOverride,
        Func<SecretsPayload, string> selector,
        string envVarName,
        params string[] placeholders)
    {
        if (IsUsable(inspectorOverride, placeholders))
        {
            return "Inspector override (remember to clear before committing)";
        }

        EnsureLoaded(forceReloadInEditor: true);

        if (cachedPayload != null && selector != null && IsUsable(selector(cachedPayload), placeholders))
        {
            return $"Assets/Resources/{ResourceName}.json (gitignored)";
        }

        if (cachedDotEnv != null && !string.IsNullOrEmpty(envVarName) &&
            cachedDotEnv.TryGetValue(envVarName, out string dotEnvVal) && IsUsable(dotEnvVal, placeholders))
        {
            return ".env (gitignored)";
        }

        if (!string.IsNullOrEmpty(envVarName) && IsUsable(Environment.GetEnvironmentVariable(envVarName), placeholders))
        {
            return $"Environment variable ({envVarName})";
        }

        return $"Missing (create Assets/Resources/{ResourceName}.json from {ResourceName}.example.json)";
    }

    private static string Resolve(
        string inspectorOverride,
        Func<SecretsPayload, string> selector,
        string envVarName,
        params string[] placeholders)
    {
        if (IsUsable(inspectorOverride, placeholders))
        {
            return inspectorOverride.Trim();
        }

        EnsureLoaded(forceReloadInEditor: Application.isEditor);

        if (cachedPayload != null && selector != null)
        {
            string jsonValue = selector(cachedPayload);
            if (IsUsable(jsonValue, placeholders))
            {
                return jsonValue.Trim();
            }
        }

        if (cachedDotEnv != null && !string.IsNullOrEmpty(envVarName) &&
            cachedDotEnv.TryGetValue(envVarName, out string dotEnvValue) &&
            IsUsable(dotEnvValue, placeholders))
        {
            return dotEnvValue.Trim();
        }

        if (!string.IsNullOrEmpty(envVarName))
        {
            string envValue = Environment.GetEnvironmentVariable(envVarName);
            if (IsUsable(envValue, placeholders))
            {
                return envValue.Trim();
            }
        }

        return "";
    }

    private static void EnsureLoaded(bool forceReloadInEditor)
    {
        if (!loaded || forceReloadInEditor)
        {
            Reload();
        }
    }

    private static bool IsUsable(string value, params string[] placeholders)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        string trimmed = value.Trim();
        if (placeholders != null)
        {
            for (int i = 0; i < placeholders.Length; i++)
            {
                if (string.Equals(trimmed, placeholders[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static SecretsPayload LoadJsonSecrets()
    {
#if UNITY_EDITOR
        try
        {
            string editorPath = Path.Combine(Application.dataPath, "Resources", ResourceName + ".json");
            if (File.Exists(editorPath))
            {
                string raw = File.ReadAllText(editorPath);
                if (!string.IsNullOrWhiteSpace(raw))
                {
                    return JsonUtility.FromJson<SecretsPayload>(raw) ?? new SecretsPayload();
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiSecretsConfig] Failed reading Editor secrets file ({ex.GetType().Name}).");
        }
#endif
        TextAsset asset = Resources.Load<TextAsset>(ResourceName);
        if (asset != null && !string.IsNullOrWhiteSpace(asset.text))
        {
            try
            {
                return JsonUtility.FromJson<SecretsPayload>(asset.text) ?? new SecretsPayload();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ApiSecretsConfig] Failed parsing Resources/{ResourceName} ({ex.GetType().Name}).");
            }
        }

        return new SecretsPayload();
    }

    private static Dictionary<string, string> LoadDotEnv()
    {
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR || UNITY_STANDALONE
        try
        {
            string dataParent = Directory.GetParent(Application.dataPath)?.FullName;
            if (string.IsNullOrEmpty(dataParent)) return map;

            string envPath = Path.Combine(dataParent, ".env");
            if (!File.Exists(envPath)) return map;

            foreach (string rawLine in File.ReadAllLines(envPath))
            {
                string line = rawLine.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith("#", StringComparison.Ordinal)) continue;
                if (line.StartsWith("export ", StringComparison.Ordinal))
                {
                    line = line.Substring("export ".Length).Trim();
                }

                int eqIndex = line.IndexOf('=');
                if (eqIndex <= 0) continue;

                string key = line.Substring(0, eqIndex).Trim();
                string value = line.Substring(eqIndex + 1).Trim();
                if ((value.StartsWith("\"", StringComparison.Ordinal) && value.EndsWith("\"", StringComparison.Ordinal) && value.Length >= 2) ||
                    (value.StartsWith("'", StringComparison.Ordinal) && value.EndsWith("'", StringComparison.Ordinal) && value.Length >= 2))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                if (!string.IsNullOrEmpty(key))
                {
                    map[key] = value;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ApiSecretsConfig] Failed reading .env ({ex.GetType().Name}).");
        }
#endif
        return map;
    }
}
