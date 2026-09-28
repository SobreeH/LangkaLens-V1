using System;
using TMPro;
using UnityEngine;

/// <summary>A scene-local keyboard. Targets are supplied by input-field handlers, never searched globally.</summary>
public sealed class BilingualVirtualKeyboard : MonoBehaviour
{
    public enum Language { English, Thai }

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text preview;
    [SerializeField] private TMP_Text languageLabel;
    [SerializeField] private Language startingLanguage = Language.English;
    [SerializeField] private VirtualKeyboardKey[] characterKeys;
    [SerializeField] private VirtualKeyboardKey shiftKey;

    // Kedmanee, with the extra consonant key placed at the end of the QWERTY row.
    public static readonly string[] EnglishRows = { "`1234567890-=", "qwertyuiop[]\\", "asdfghjkl;'", "zxcvbnm,./" };
    public static readonly string[] EnglishShiftRows = { "~!@#$%^&*()_+", "QWERTYUIOP{}|", "ASDFGHJKL:\"", "ZXCVBNM<>?" };
    public static readonly string[] ThaiRows = { "_ๅ/-ภถุึคตจขช", "ๆไำพะัีรนยบลฃ", "ฟหกดเ้่าสวง", "ผปแอิืทมใฝ" };
    public static readonly string[] ThaiShiftRows = { "%+๑๒๓๔ู฿๕๖๗๘๙", "๐\"ฎฑธํ๊ณฯญฐ,ฅ", "ฤฆฏโฌ็๋ษศซ.", "()ฉฮฺ์?ฒฬฦ" };

    private TMP_InputField target;
    private Language language;
    private bool shifted;
    private bool initialized;
    private bool editing;
    private int anchor;
    private int focus;

    public TMP_InputField Target => target;
    public bool IsOpen => target != null && panel != null && panel.activeInHierarchy;
    public Language CurrentLanguage => language;
    public bool Shifted => shifted;
    public event Action<TMP_InputField> Closed;

    private void Awake() { Initialize(); }

    private void Initialize()
    {
        if (initialized) return;
        initialized = true;
        language = startingLanguage;
        RefreshKeys();
        if (panel != null) panel.SetActive(false);
    }

    public void Show(TMP_InputField input)
    {
        Initialize();
        if (input == null || !input.isActiveAndEnabled || input.readOnly || !input.IsInteractable()) return;
        if (target == input && IsOpen) return;
        Hide();
        target = input;
        anchor = focus = input.text.Length;
        input.onValueChanged.AddListener(OnTargetTextChanged);
        shifted = false;
        RefreshKeys();
        panel.SetActive(true);
        RefreshPreview();
    }

    public void Hide()
    {
        TMP_InputField previous = target;
        target = null;
        if (previous != null)
        {
            previous.onValueChanged.RemoveListener(OnTargetTextChanged);
            previous.DeactivateInputField();
        }
        if (panel != null) panel.SetActive(false);
        if (previous != null) Closed?.Invoke(previous);
    }

    public void HideFor(TMP_InputField input)
    {
        if (target == input) Hide();
    }

    private void OnDisable() { Hide(); }

    private void LateUpdate()
    {
        if (target == null) return;
        if (!target.isActiveAndEnabled) { Hide(); return; }
        if (target.isFocused) CaptureSelection(target);
    }

    public void CaptureSelection(TMP_InputField input)
    {
        if (target != input || target == null) return;
        int nextAnchor = VirtualKeyboardTextEditing.Boundary(input.text, input.selectionStringAnchorPosition);
        int nextFocus = VirtualKeyboardTextEditing.Boundary(input.text, input.selectionStringFocusPosition);
        if (anchor == nextAnchor && focus == nextFocus) return;
        anchor = nextAnchor;
        focus = nextFocus;
        RefreshPreview();
    }

    private void OnTargetTextChanged(string value)
    {
        if (!editing) anchor = focus = value.Length;
        RefreshPreview();
    }

    public void ToggleLanguage()
    {
        language = language == Language.English ? Language.Thai : Language.English;
        shifted = false;
        RefreshKeys();
    }

    public void ToggleShift() { shifted = !shifted; RefreshKeys(); }

    public string GetCharacter(int row, int column)
    {
        string[] rows = language == Language.Thai
            ? (shifted ? ThaiShiftRows : ThaiRows)
            : (shifted ? EnglishShiftRows : EnglishRows);
        return rows[row][column].ToString();
    }

    private void RefreshKeys()
    {
        if (characterKeys != null)
            foreach (VirtualKeyboardKey key in characterKeys)
                if (key != null) key.RefreshLabel();
        if (shiftKey != null) shiftKey.SetHighlighted(shifted);
        if (languageLabel != null) languageLabel.text = language == Language.Thai ? "ภาษาไทย - Kedmanee" : "English - QWERTY";
    }

    public void InsertCharacter(int row, int column)
    {
        InsertText(GetCharacter(row, column));
        // Shift is latched until tapped again, so Thai shifted characters remain accessible.
    }

    public void InsertText(string value)
    {
        if (!CanEdit() || string.IsNullOrEmpty(value)) return;
        if (!target.multiLine) value = value.Replace("\r", "").Replace("\n", "");
        string next = VirtualKeyboardTextEditing.Replace(target.text, anchor, focus, value, target.characterLimit, out int caret);
        ApplyText(next, caret);
    }

    public void Backspace()
    {
        if (!CanEdit()) return;
        int start = Mathf.Min(anchor, focus);
        int end = Mathf.Max(anchor, focus);
        if (start == end) start = VirtualKeyboardTextEditing.Previous(target.text, start);
        string next = VirtualKeyboardTextEditing.Replace(target.text, start, end, "", 0, out int caret);
        ApplyText(next, caret);
    }

    public void MoveCaret(int direction)
    {
        if (!CanEdit()) return;
        int caret = direction < 0 ? Mathf.Min(anchor, focus) : Mathf.Max(anchor, focus);
        if (anchor == focus)
            caret = direction < 0 ? VirtualKeyboardTextEditing.Previous(target.text, caret) : VirtualKeyboardTextEditing.Next(target.text, caret);
        anchor = focus = caret;
        target.stringPosition = caret;
        RefreshPreview();
    }

    public void Submit()
    {
        if (!CanEdit()) return;
        TMP_InputField input = target;
        string text = input.text;
        Hide();
        input.onSubmit.Invoke(text);
    }

    private bool CanEdit() => target != null && target.isActiveAndEnabled && !target.readOnly && target.IsInteractable();

    private void ApplyText(string value, int caret)
    {
        anchor = focus = caret;
        editing = true;
        try { target.text = value; }
        finally { editing = false; }
        if (target == null) return; // A consumer may close the keyboard from onValueChanged.
        anchor = focus = Mathf.Min(caret, target.text.Length);
        target.stringPosition = focus;
        RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (preview == null || target == null) return;
        // Keep a bounded window around the caret; show passwords as bullets only.
        string value = target.inputType == TMP_InputField.InputType.Password ? new string('*', target.text.Length) : target.text;
        int caret = Mathf.Clamp(focus, 0, value.Length);
        int start = VirtualKeyboardTextEditing.Boundary(value, Mathf.Max(0, caret - 45));
        int end = VirtualKeyboardTextEditing.Boundary(value, Mathf.Min(value.Length, start + 85));
        preview.text = (start > 0 ? "..." : "") + value.Substring(start, caret - start) + "|" + value.Substring(caret, end - caret) + (end < value.Length ? "..." : "");
    }
}

/// <summary>UTF-16-safe editing. Backspace removes a Thai combining mark before its base letter.</summary>
public static class VirtualKeyboardTextEditing
{
    public static int Boundary(string text, int index)
    {
        index = Mathf.Clamp(index, 0, text.Length);
        if (index > 0 && index < text.Length && char.IsLowSurrogate(text[index]) && char.IsHighSurrogate(text[index - 1])) index--;
        return index;
    }

    public static int Previous(string text, int index)
    {
        index = Boundary(text, index);
        return index == 0 ? 0 : Boundary(text, index - 1);
    }

    public static int Next(string text, int index)
    {
        index = Boundary(text, index);
        if (index == text.Length) return index;
        return index + (char.IsHighSurrogate(text[index]) && index + 1 < text.Length && char.IsLowSurrogate(text[index + 1]) ? 2 : 1);
    }

    public static string Replace(string text, int anchor, int focus, string inserted, int limit, out int caret)
    {
        text = text ?? "";
        inserted = inserted ?? "";
        int start = Boundary(text, Mathf.Min(anchor, focus));
        int end = Boundary(text, Mathf.Max(anchor, focus));
        if (limit > 0)
        {
            int available = Mathf.Max(0, limit - (text.Length - (end - start)));
            if (inserted.Length > available) inserted = inserted.Substring(0, Boundary(inserted, available));
        }
        caret = start + inserted.Length;
        return text.Substring(0, start) + inserted + text.Substring(end);
    }
}
