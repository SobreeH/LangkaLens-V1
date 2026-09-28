using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Offline Play Mode checks; no microphone, API calls, or chat messages are sent.</summary>
public static class BilingualKeyboardValidation
{
    [MenuItem("Tools/LangkaLens/Validate Bilingual Keyboard (Play Mode)")]
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Enter Play Mode first.");
        var asset = AssetDatabase.LoadAssetAtPath<GameObject>(BilingualKeyboardPrefabBuilder.PrefabPath);
        var first = UnityEngine.Object.Instantiate(asset);
        var second = UnityEngine.Object.Instantiate(asset);
        var field = CreateInput("KeyboardTestField");
        var other = CreateInput("KeyboardTestOther");
        try
        {
            var keyboard = first.GetComponent<BilingualVirtualKeyboard>();
            var independent = second.GetComponent<BilingualVirtualKeyboard>();
            keyboard.Show(field);
            independent.Show(other);
            int changes = 0;
            field.onValueChanged.AddListener(_ => changes++);
            Press(first, "Key2_0");
            Require(field.text == "a" && changes == 1, "English key inserts once");
            Press(first, "Shift"); Press(first, "Key2_0");
            Require(field.text == "aA", "English Shift");
            Press(first, "Language");
            field.text = "";
            Press(first, "Key2_2"); Press(first, "Key2_5");
            Require(field.text == "ก้", "Thai consonant and tone mark");
            Press(first, "Backspace");
            Require(field.text == "ก", "Backspace removes the tone mark only");
            Require(independent.CurrentLanguage == BilingualVirtualKeyboard.Language.English && other.text == "", "Instances are independent");
            Press(first, "Shift"); Press(first, "Key0_2");
            Require(field.text == "ก๑", "Thai shifted digit");
            field.text = "ABCD";
            field.selectionStringAnchorPosition = 1;
            field.selectionStringFocusPosition = 3;
            keyboard.CaptureSelection(field);
            keyboard.InsertText("ไทย");
            Require(field.text == "AไทยD", "Selection replacement");
            keyboard.MoveCaret(-1); keyboard.InsertText("!");
            Require(field.text == "Aไท!ยD", "Caret insertion");
            field.text = "x😀";
            keyboard.Backspace();
            Require(field.text == "x", "Surrogate pair deletion");
            field.characterLimit = 2;
            keyboard.InsertText("😀");
            Require(field.text == "x", "Character limit does not split a surrogate pair");
            field.characterLimit = 0;
            int submits = 0;
            field.onSubmit.AddListener(_ => submits++);
            Press(first, "Enter");
            Require(submits == 1 && !keyboard.IsOpen && independent.IsOpen, "Enter submits once and closes only this keyboard");
            keyboard.Show(field); keyboard.Hide(); keyboard.Show(field);
            changes = 0; keyboard.InsertText("z");
            Require(changes == 1, "Reopening does not duplicate subscriptions");
            keyboard.Show(other); keyboard.InsertText("b");
            Require(field.text == "xz" && other.text == "b", "Retargeting does not edit the old field");
            first.SetActive(false);
            Require(keyboard.Target == null, "Disable releases the target");
            Debug.Log("Bilingual keyboard: 14 offline Play Mode checks passed.");
        }
        finally
        {
            UnityEngine.Object.Destroy(first); UnityEngine.Object.Destroy(second);
            UnityEngine.Object.Destroy(field.gameObject); UnityEngine.Object.Destroy(other.gameObject);
        }
    }

    private static TMP_InputField CreateInput(string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var input = go.AddComponent<TMP_InputField>();
        var textObject = new GameObject("Text", typeof(RectTransform));
        textObject.transform.SetParent(go.transform, false);
        input.textComponent = textObject.AddComponent<TextMeshProUGUI>();
        input.textComponent.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/Sarabun-Medium SDF.asset");
        input.textViewport = textObject.GetComponent<RectTransform>();
        input.shouldHideSoftKeyboard = true;
        return input;
    }

    private static void Press(GameObject root, string name)
    {
        foreach (var key in root.GetComponentsInChildren<VirtualKeyboardKey>(true))
        {
            if (key.name != name) continue;
            // Exercise the actual Button.onClick subscription, not the controller method.
            ExecuteEvents.Execute(key.gameObject, new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left }, ExecuteEvents.pointerClickHandler);
            return;
        }
        throw new InvalidOperationException("Missing key: " + name);
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException("Keyboard check failed: " + name);
    }
}
