using System.Globalization;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(UnityEngine.UI.Button))]
public sealed class VirtualKeyboardKey : MonoBehaviour
{
    public enum Action { Character, Shift, Language, Space, Backspace, Left, Right, Enter, Close }
    [SerializeField] private BilingualVirtualKeyboard keyboard;
    [SerializeField] private Action action;
    [SerializeField] private int row;
    [SerializeField] private int column;
    [SerializeField] private TMP_Text label;
    [Header("Key Colours")]
    [SerializeField] private Color restingColor = new Color(0.19f, 0.26f, 0.35f);
    [SerializeField] private Color shiftedColor = new Color(0.1f, 0.5f, 0.62f);
    private UnityEngine.UI.Button button;

    private void Awake() { button = GetComponent<UnityEngine.UI.Button>(); }
    private void OnEnable()
    {
        if (button == null) button = GetComponent<UnityEngine.UI.Button>();
        button.onClick.AddListener(Press);
        RefreshLabel();
    }
    private void OnDisable() { if (button != null) button.onClick.RemoveListener(Press); }

    public void Configure(BilingualVirtualKeyboard owner, Action keyAction, TMP_Text text, int rowIndex = 0, int columnIndex = 0)
    {
        keyboard = owner; action = keyAction; label = text; row = rowIndex; column = columnIndex;
    }

    public void RefreshLabel()
    {
        if (keyboard == null || label == null || action != Action.Character) return;
        string value = keyboard.GetCharacter(row, column);
        UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(value, 0);
        // A neutral base makes isolated Thai vowel/tone marks visible on keycaps.
        label.text = category == UnicodeCategory.NonSpacingMark || category == UnicodeCategory.SpacingCombiningMark ? "อ" + value : value;
    }

    public void SetHighlighted(bool active)
    {
        var image = GetComponent<UnityEngine.UI.Image>();
        if (image != null) image.color = active ? shiftedColor : restingColor;
    }

    public void Press()
    {
        if (keyboard == null) return;
        switch (action)
        {
            case Action.Character: keyboard.InsertCharacter(row, column); break;
            case Action.Shift: keyboard.ToggleShift(); break;
            case Action.Language: keyboard.ToggleLanguage(); break;
            case Action.Space: keyboard.InsertText(" "); break;
            case Action.Backspace: keyboard.Backspace(); break;
            case Action.Left: keyboard.MoveCaret(-1); break;
            case Action.Right: keyboard.MoveCaret(1); break;
            case Action.Enter: keyboard.Submit(); break;
            case Action.Close: keyboard.Hide(); break;
        }
    }
}
