using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

/// <summary>Authors the reusable UI; Meta ray setup is added with the SDK's canvas action before saving.</summary>
public static class BilingualKeyboardPrefabBuilder
{
    public const string PrefabPath = "Assets/_LangkaLens/Mixed/3D Models/Prefabs/Center/BilingualVirtualKeyboard.prefab";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset";

    public static GameObject Create()
    {
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (font == null) throw new System.InvalidOperationException("Sarabun TMP font is required.");
        var root = Rect("BilingualVirtualKeyboard", null);
        root.sizeDelta = new Vector2(2000, 800);
        root.localScale = Vector3.one * 0.001f;
        var keyboard = root.gameObject.AddComponent<BilingualVirtualKeyboard>();
        var panel = Rect("KeyboardPanel", root);
        Stretch(panel);
        panel.gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        panel.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        panel.gameObject.AddComponent<UnityEngine.UI.Image>().color = new Color(0.035f, 0.055f, 0.085f, 0.98f);

        var content = Rect("Layout", panel);
        Stretch(content);
        var layout = content.gameObject.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layout.padding = new RectOffset(24, 24, 20, 20);
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        var header = Row("Header", content, 64);
        var title = Label("LanguageLabel", header, font, "English - QWERTY", 38);
        title.alignment = TextAlignmentOptions.MidlineLeft;
        title.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().flexibleWidth = 1;
        Key("Language", "EN / ไทย", header, keyboard, font, VirtualKeyboardKey.Action.Language, 2.4f, 34);
        Key("Close", "Close / ปิด", header, keyboard, font, VirtualKeyboardKey.Action.Close, 2.2f, 30);
        var preview = Label("TextPreview", content, font, "|", 40);
        preview.alignment = TextAlignmentOptions.MidlineLeft;
        preview.margin = new Vector4(18, 0, 18, 0);
        preview.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = 88;

        var keys = new List<VirtualKeyboardKey>();
        for (int row = 0; row < BilingualVirtualKeyboard.EnglishRows.Length; row++)
        {
            var rowTransform = Row("CharacterRow" + row, content, 96);
            for (int col = 0; col < BilingualVirtualKeyboard.EnglishRows[row].Length; col++)
            {
                var key = Key("Key" + row + "_" + col, "", rowTransform, keyboard, font, VirtualKeyboardKey.Action.Character, 1, 48, row, col);
                keys.Add(key);
            }
            if (row == 0) Key("Backspace", "Backspace", rowTransform, keyboard, font, VirtualKeyboardKey.Action.Backspace, 2, 29);
        }
        var footer = Row("Actions", content, 108);
        var shift = Key("Shift", "Shift", footer, keyboard, font, VirtualKeyboardKey.Action.Shift, 1.7f, 36);
        Key("Left", "<", footer, keyboard, font, VirtualKeyboardKey.Action.Left, 1, 46);
        Key("Space", "Space / เว้นวรรค", footer, keyboard, font, VirtualKeyboardKey.Action.Space, 6, 36);
        Key("Right", ">", footer, keyboard, font, VirtualKeyboardKey.Action.Right, 1, 46);
        var enter = Key("Enter", "Enter / ส่ง", footer, keyboard, font, VirtualKeyboardKey.Action.Enter, 2.5f, 36);
        enter.GetComponent<UnityEngine.UI.Image>().color = new Color(0.07f, 0.43f, 0.47f);

        var serialized = new SerializedObject(keyboard);
        serialized.FindProperty("panel").objectReferenceValue = panel.gameObject;
        serialized.FindProperty("preview").objectReferenceValue = preview;
        serialized.FindProperty("languageLabel").objectReferenceValue = title;
        serialized.FindProperty("shiftKey").objectReferenceValue = shift;
        var property = serialized.FindProperty("characterKeys");
        property.arraySize = keys.Count;
        for (int i = 0; i < keys.Count; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = keys[i];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        foreach (var key in keys) key.RefreshLabel();
        UnityEngine.UI.LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
        return root.gameObject;
    }

    private static RectTransform Rect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.layer = LayerMask.NameToLayer("UI");
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        return rt;
    }

    private static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    private static RectTransform Row(string name, Transform parent, float height)
    {
        var rt = Rect(name, parent);
        rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>().preferredHeight = height;
        var layout = rt.gameObject.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        layout.spacing = 12;
        layout.childControlWidth = layout.childControlHeight = true;
        layout.childForceExpandWidth = layout.childForceExpandHeight = true;
        return rt;
    }

    private static TextMeshProUGUI Label(string name, Transform parent, TMP_FontAsset font, string text, float size)
    {
        var label = Rect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        label.font = font;
        label.text = text;
        label.fontSize = size;
        label.enableAutoSizing = false;
        label.alignment = TextAlignmentOptions.Center;
        label.color = new Color(0.94f, 0.97f, 1);
        label.raycastTarget = false;
        label.richText = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Truncate;
        return label;
    }

    private static VirtualKeyboardKey Key(string name, string caption, Transform parent, BilingualVirtualKeyboard keyboard,
        TMP_FontAsset font, VirtualKeyboardKey.Action action, float weight, float size, int row = 0, int col = 0)
    {
        var rt = Rect(name, parent);
        var image = rt.gameObject.AddComponent<UnityEngine.UI.Image>();
        image.color = action == VirtualKeyboardKey.Action.Character ? new Color(0.11f, 0.16f, 0.23f) : new Color(0.19f, 0.26f, 0.35f);
        var button = rt.gameObject.AddComponent<UnityEngine.UI.Button>();
        button.targetGraphic = image;
        button.navigation = new UnityEngine.UI.Navigation { mode = UnityEngine.UI.Navigation.Mode.None };
        var colors = button.colors;
        colors.highlightedColor = new Color(0.55f, 0.9f, 1);
        colors.pressedColor = new Color(0.3f, 0.7f, 0.8f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;
        var layout = rt.gameObject.AddComponent<UnityEngine.UI.LayoutElement>();
        layout.minWidth = 0;
        layout.preferredWidth = weight * 110;
        layout.flexibleWidth = weight;
        var label = Label("Label", rt, font, caption, size);
        Stretch(label.rectTransform);
        label.margin = new Vector4(6, 4, 6, 4);
        var key = rt.gameObject.AddComponent<VirtualKeyboardKey>();
        key.Configure(keyboard, action, label, row, col);
        return key;
    }
}
