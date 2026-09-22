#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

public static class GeminiModelPicker
{
    private const string CachePrefKey = "LangkaLens_GeminiFlashModelsCache";

    public static readonly string[] DefaultFlashModels = new string[]
    {
        // Flash-Lite tier (lowest cost & lowest latency)
        "gemini-3.5-flash-lite",
        "gemini-flash-lite-latest",
        "gemini-3.1-flash-lite",
        "gemini-3.1-flash-lite-preview",
        
        // Flash tier (higher capability & multimodal audio STT)
        "gemini-3.8-flash",
        "gemini-flash-latest",
        "gemini-3.7-flash",
        "gemini-3.6-flash",
        "gemini-3.5-flash"
    };

    public static List<string> GetAvailableFlashModels()
    {
        List<string> list = new List<string>(DefaultFlashModels);

        string cached = EditorPrefs.GetString(CachePrefKey, "");
        if (!string.IsNullOrEmpty(cached))
        {
            string[] cachedModels = cached.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string m in cachedModels)
            {
                string trimmed = m.Trim();
                if (!string.IsNullOrEmpty(trimmed) && !list.Contains(trimmed))
                {
                    list.Add(trimmed);
                }
            }
        }

        return list;
    }

    public static void SaveCachedModels(IEnumerable<string> models)
    {
        string joined = string.Join(";", models.Distinct());
        EditorPrefs.SetString(CachePrefKey, joined);
    }

    public static void DrawModelSelector(SerializedObject serializedObject, SerializedProperty modelProp, string apiKey, string label = "Gemini Model")
    {
        if (modelProp == null) return;

        List<string> availableModels = GetAvailableFlashModels();
        string currentModel = modelProp.stringValue;
        if (string.IsNullOrWhiteSpace(currentModel))
        {
            currentModel = DefaultFlashModels[0];
            modelProp.stringValue = currentModel;
        }

        // Build popup list: models + "[Custom Model...]"
        List<string> displayOptions = new List<string>(availableModels);
        displayOptions.Add("[Custom Model...]");

        int selectedIndex = availableModels.IndexOf(currentModel);
        bool isCustom = selectedIndex < 0;
        int popupIndex = isCustom ? displayOptions.Count - 1 : selectedIndex;

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("AI Model Configuration (Budget Flash/Lite)", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        int newPopupIndex = EditorGUILayout.Popup(label, popupIndex, displayOptions.ToArray());
        if (EditorGUI.EndChangeCheck())
        {
            if (newPopupIndex >= 0 && newPopupIndex < availableModels.Count)
            {
                modelProp.stringValue = availableModels[newPopupIndex];
                isCustom = false;
            }
            else
            {
                isCustom = true;
            }
        }

        if (isCustom)
        {
            EditorGUI.indentLevel++;
            modelProp.stringValue = EditorGUILayout.TextField("Custom Model ID", modelProp.stringValue);
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(2);

        // Model Tier Info Box
        string tierInfo = modelProp.stringValue.Contains("lite", StringComparison.OrdinalIgnoreCase) 
            ? "⚡ Budget-Friendly Flash Lite Tier (Ultra low cost & low latency)"
            : "⚡ Fast Flash Tier (High quality multimodal & low cost)";
        EditorGUILayout.HelpBox($"{tierInfo}\nActive Model: {modelProp.stringValue}", MessageType.Info);

        // Fetch button
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Fetch Flash/Lite Models from API", GUILayout.Height(26)))
        {
            if (string.IsNullOrWhiteSpace(apiKey) || apiKey == "YOUR_API_KEY_HERE")
            {
                EditorUtility.DisplayDialog("Missing API Key", "Please enter a valid Gemini API Key above before fetching models.", "OK");
            }
            else
            {
                FetchModelsFromAPI(apiKey, modelProp);
            }
        }

        if (GUILayout.Button("Reset Defaults", GUILayout.Width(110), GUILayout.Height(26)))
        {
            EditorPrefs.DeleteKey(CachePrefKey);
            modelProp.stringValue = DefaultFlashModels[0];
            EditorUtility.DisplayDialog("Defaults Restored", "Model list has been reset to default Flash & Flash-Lite models.", "OK");
        }
        EditorGUILayout.EndHorizontal();

        // Check if editing a prefab instance in a scene
        UnityEngine.Object targetObj = serializedObject.targetObject;
        if (targetObj != null && PrefabUtility.IsPartOfPrefabInstance(targetObj))
        {
            EditorGUILayout.Space(4);
            if (GUILayout.Button("💾 Apply Model to Prefab Asset (Propagate to All Scenes)", GUILayout.Height(24)))
            {
                serializedObject.ApplyModifiedProperties();
                string assetPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(targetObj);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    PrefabUtility.ApplyPropertyOverride(modelProp, assetPath, InteractionMode.UserAction);
                    EditorUtility.DisplayDialog("Applied to Prefab", $"Model '{modelProp.stringValue}' has been applied to the prefab asset:\n{assetPath}\n\nAll scenes using this prefab will now use this model.", "OK");
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private static void FetchModelsFromAPI(string apiKey, SerializedProperty modelProp)
    {
        string url = $"https://generativelanguage.googleapis.com/v1beta/models?key={apiKey}";
        
        EditorUtility.DisplayProgressBar("Fetching Gemini Models", "Connecting to Google Generative Language API...", 0.3f);

        UnityWebRequest request = UnityWebRequest.Get(url);
        var operation = request.SendWebRequest();

        EditorApplication.CallbackFunction updateCallback = null;
        updateCallback = () =>
        {
            if (operation.isDone)
            {
                EditorApplication.update -= updateCallback;
                EditorUtility.ClearProgressBar();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    string err = request.downloadHandler != null ? request.downloadHandler.text : request.error;
                    EditorUtility.DisplayDialog("Fetch Failed", $"Error fetching models:\n{request.error}\n{err}", "OK");
                }
                else
                {
                    ProcessFetchedModels(request.downloadHandler.text, modelProp);
                }

                request.Dispose();
            }
        };

        EditorApplication.update += updateCallback;
    }

    private static void ProcessFetchedModels(string jsonResponse, SerializedProperty modelProp)
    {
        try
        {
            GeminiModelsListResponse response = JsonUtility.FromJson<GeminiModelsListResponse>(jsonResponse);
            if (response == null || response.models == null || response.models.Length == 0)
            {
                EditorUtility.DisplayDialog("No Models Found", "API returned a response with no models listed.", "OK");
                return;
            }

            List<string> filteredFlashModels = new List<string>();

            foreach (var m in response.models)
            {
                if (string.IsNullOrEmpty(m.name)) continue;

                string shortName = m.name.StartsWith("models/") ? m.name.Substring(7) : m.name;
                string lower = shortName.ToLowerInvariant();

                // Strictly filter for Flash and Lite models
                bool isFlashOrLite = lower.Contains("flash") || lower.Contains("lite");
                
                // Exclude specialized non-chat/audio models and discontinued/deprecated generations
                bool isExcluded = lower.Contains("image") || lower.Contains("tts") || lower.Contains("robotics") ||
                                  lower.Contains("veo") || lower.Contains("embedding") || lower.Contains("computer-use") ||
                                  lower.Contains("live") || lower.Contains("aqa") ||
                                  lower.Contains("2.5") || lower.Contains("2.0") || lower.Contains("1.5") || lower.Contains("1.0");

                bool supportsGenerate = m.supportedGenerationMethods != null && m.supportedGenerationMethods.Contains("generateContent");

                if (isFlashOrLite && !isExcluded && supportsGenerate)
                {
                    filteredFlashModels.Add(shortName);
                }
            }

            // Always ensure base default models are present
            foreach (var def in DefaultFlashModels)
            {
                if (!filteredFlashModels.Contains(def))
                {
                    filteredFlashModels.Add(def);
                }
            }

            // Sort: Flash Lite models first (cheapest), then Flash models
            filteredFlashModels = filteredFlashModels
                .OrderBy(m => m.Contains("lite") ? 0 : 1)
                .ThenBy(m => m)
                .ToList();

            SaveCachedModels(filteredFlashModels);

            EditorUtility.DisplayDialog("Gemini Models Updated", 
                $"Found {filteredFlashModels.Count} Flash & Flash-Lite models available on your API key:\n\n" + 
                string.Join("\n", filteredFlashModels.Take(10)) + 
                (filteredFlashModels.Count > 10 ? $"\n...and {filteredFlashModels.Count - 10} more." : ""), 
                "OK");

            if (modelProp != null && modelProp.serializedObject != null)
            {
                modelProp.serializedObject.ApplyModifiedProperties();
            }
        }
        catch (Exception ex)
        {
            EditorUtility.DisplayDialog("Parsing Error", $"Failed to parse models JSON:\n{ex.Message}", "OK");
        }
    }

    [Serializable]
    private class GeminiModelsListResponse
    {
        public GeminiModelInfo[] models;
    }

    [Serializable]
    private class GeminiModelInfo
    {
        public string name;
        public string[] supportedGenerationMethods;
    }
}
#endif
