using System;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace LangkaLens.Editor
{
    public static class GeminiLocalizationSetup
    {
        private const string Table = "Gemini_UI";
        private const string PrefabPath = "Assets/_LangkaLens/Mixed/3D Models/Prefabs/Center/Gemini_Canvas.prefab";

        private static readonly string[,] Copy =
        {
            { "UI_Button_Clear", "Clear", "ล้าง" },
            { "Chat_Sender_User", "You", "คุณ" },
            { "Chat_Sender_Guide", "Guide", "ไกด์" }
        };

        [MenuItem("Tools/LangkaLens/Gemini/Setup Localization for Clear Button")]
        public static void RunSetup()
        {
            PopulateTables();
            SetupPrefab();
            Debug.Log("[GeminiLocalizationSetup] Localization setup completed successfully.");
        }

        private static void PopulateTables()
        {
            var collection = LocalizationEditorSettings.GetStringTableCollection(Table);
            if (collection == null) throw new InvalidOperationException("Gemini_UI table collection is missing.");

            Undo.RecordObject(collection.SharedData, "Add Gemini UI keys");

            foreach (var table in collection.StringTables)
            {
                int column = table.LocaleIdentifier.Code == "th" ? 2 : table.LocaleIdentifier.Code == "en" ? 1 : -1;
                if (column < 0) continue;

                Undo.RecordObject(table, "Add Gemini UI translations");
                for (int i = 0; i < Copy.GetLength(0); i++)
                {
                    string key = Copy[i, 0];
                    string val = Copy[i, column];
                    table.AddEntry(key, val);
                }
                EditorUtility.SetDirty(table);
            }

            EditorUtility.SetDirty(collection.SharedData);
            EditorUtility.SetDirty(collection);
            LocalizationEditorSettings.EditorEvents.RaiseCollectionModified(typeof(GeminiLocalizationSetup), collection);
            AssetDatabase.SaveAssets();
        }

        private static void SetupPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                var clearButton = root.GetComponentsInChildren<UnityEngine.UI.Button>(true)
                    .FirstOrDefault(b => b.gameObject.name == "Clear_Button");

                if (clearButton == null)
                    throw new InvalidOperationException("Could not find Clear_Button in prefab.");

                var textTmp = clearButton.GetComponentInChildren<TextMeshProUGUI>(true);
                if (textTmp == null)
                    throw new InvalidOperationException("Could not find TextMeshProUGUI under Clear_Button.");

                textTmp.text = "Clear";

                var localizeEvent = textTmp.GetComponent<LocalizeStringEvent>();
                if (localizeEvent == null)
                {
                    localizeEvent = textTmp.gameObject.AddComponent<LocalizeStringEvent>();
                }

                localizeEvent.StringReference = new LocalizedString(Table, "UI_Button_Clear");

                // Ensure single persistent listener for set_text
                while (localizeEvent.OnUpdateString.GetPersistentEventCount() > 0)
                {
                    UnityEventTools.RemovePersistentListener(localizeEvent.OnUpdateString, 0);
                }

                var setter = (UnityAction<string>)Delegate.CreateDelegate(typeof(UnityAction<string>), textTmp, "set_text");
                UnityEventTools.AddPersistentListener(localizeEvent.OnUpdateString, setter);
                localizeEvent.OnUpdateString.SetPersistentListenerState(0, UnityEventCallState.EditorAndRuntime);
                localizeEvent.RefreshString();

                // Setup icon Image child matching Send_Button / Speak_Button pattern
                const string iconPath = "Assets/_LangkaLens/Mixed/3D Models/Icons/clear.png";
                AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

                var iconTransform = clearButton.transform.Find("Image");
                GameObject iconGo;
                if (iconTransform == null)
                {
                    iconGo = new GameObject("Image", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
                    iconGo.transform.SetParent(clearButton.transform, false);
                    iconGo.transform.SetSiblingIndex(0); // keep text rendered above or next to it
                }
                else
                {
                    iconGo = iconTransform.gameObject;
                }

                iconGo.layer = LayerMask.NameToLayer("UI");
                var iconRect = iconGo.GetComponent<RectTransform>();
                iconRect.anchorMin = new Vector2(0.5f, 0.5f);
                iconRect.anchorMax = new Vector2(0.5f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.anchoredPosition = new Vector2(-54f, 0f);
                iconRect.sizeDelta = new Vector2(20f, 20f);
                iconRect.localScale = Vector3.one;
                iconRect.localRotation = Quaternion.identity;

                var iconImage = iconGo.GetComponent<UnityEngine.UI.Image>();
                iconImage.sprite = sprite;
                iconImage.color = new Color(0.8117648f, 0.6627451f, 0.4156863f, 1f);
                iconImage.raycastTarget = false;
                iconImage.preserveAspect = true;

                // Shift Text (TMP) slightly to right to balance nicely with the icon
                var textRect = textTmp.GetComponent<RectTransform>();
                textRect.anchoredPosition = new Vector2(10f, 0f);
                textRect.sizeDelta = new Vector2(-24f, 0f);

                EditorUtility.SetDirty(iconImage);
                EditorUtility.SetDirty(iconGo);
                EditorUtility.SetDirty(localizeEvent);
                EditorUtility.SetDirty(textTmp.gameObject);
                EditorUtility.SetDirty(root);

                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }
    }
}
