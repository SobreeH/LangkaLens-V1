using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(TMP_InputField))]
public class QuestKeyboardHandler : MonoBehaviour, UnityEngine.EventSystems.IPointerClickHandler
{
    [Header("Keyboard")]
    [Tooltip("Assign a scene-local bilingual keyboard. Leave empty to use the Quest system keyboard.")]
    [SerializeField] private BilingualVirtualKeyboard customKeyboard;
    private TMP_InputField inputField;
    private Image targetImage;
    private Color originalColor = Color.white;

    [Header("Visual Feedback Settings")]
    public Color activeBackgroundColor = new Color(0.88f, 0.94f, 1.0f, 1.0f);
    [Tooltip("Send the chat message when the input field is submitted, including keyboard Done.")]
    public bool submitOnDone = true;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
        // A custom keyboard suppresses the native overlay for this field only.
        inputField.shouldHideSoftKeyboard = customKeyboard != null;
        inputField.shouldHideMobileInput = false;
        inputField.shouldActivateOnSelect = true;
        inputField.keyboardType = TouchScreenKeyboardType.Default;
        if (customKeyboard != null)
        {
            inputField.onFocusSelectAll = false;
            inputField.resetOnDeActivation = false;
        }
        targetImage = GetComponent<Image>();
        if (targetImage != null)
        {
            originalColor = targetImage.color;
        }
    }

    private void OnEnable()
    {
        if (customKeyboard != null) customKeyboard.Closed += OnCustomKeyboardClosed;
        if (inputField != null)
        {
            inputField.onSelect.AddListener(OnInputFieldSelected);
            inputField.onDeselect.AddListener(OnInputFieldDeselected);
            inputField.onEndEdit.AddListener(OnInputFieldDeselected);
        }
    }

    private void OnDisable()
    {
        if (customKeyboard != null) customKeyboard.Closed -= OnCustomKeyboardClosed;
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(OnInputFieldSelected);
            inputField.onDeselect.RemoveListener(OnInputFieldDeselected);
            inputField.onEndEdit.RemoveListener(OnInputFieldDeselected);
        }
        CloseKeyboard();
    }

    private void OnInputFieldSelected(string text)
    {
        SetEditingVisual(true);
        if (customKeyboard != null) customKeyboard.Show(inputField);
    }

    private void OnCustomKeyboardClosed(TMP_InputField closedInput)
    {
        if (closedInput == inputField) SetEditingVisual(false);
    }

    private void OnInputFieldDeselected(string text)
    {
        if (customKeyboard != null && customKeyboard.Target == inputField)
        {
            customKeyboard.CaptureSelection(inputField);
            return;
        }
        SetEditingVisual(false);
    }

    public void OnPointerClick(UnityEngine.EventSystems.PointerEventData eventData)
    {
        if (eventData.button == UnityEngine.EventSystems.PointerEventData.InputButton.Left && customKeyboard != null)
            customKeyboard.Show(inputField);
    }

    public void OpenKeyboard()
    {
        if (inputField == null || !inputField.isActiveAndEnabled ||
            !inputField.IsInteractable() || inputField.readOnly)
            return;

        if (customKeyboard != null) customKeyboard.Show(inputField);
        inputField.ActivateInputField();
    }

    private void SetEditingVisual(bool active)
    {
        if (targetImage != null)
        {
            targetImage.color = active ? activeBackgroundColor : originalColor;
        }
    }

    public void CloseKeyboard()
    {
        if (customKeyboard != null) customKeyboard.HideFor(inputField);
        if (inputField != null)
            inputField.DeactivateInputField();
        SetEditingVisual(false);
    }
}
