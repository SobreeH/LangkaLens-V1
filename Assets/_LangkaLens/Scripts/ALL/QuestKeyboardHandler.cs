using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class QuestKeyboardHandler : MonoBehaviour, IPointerDownHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard overlayKeyboard;
    private Image targetImage;
    private Color originalColor = Color.white;
    private bool isEditingActive = false;

    [Header("Visual Feedback Settings")]
    [Tooltip("Color applied to the input field background when awaiting input.")]
    public Color activeBackgroundColor = new Color(0.88f, 0.94f, 1.0f, 1.0f);

    [Tooltip("Whether to automatically send onSubmit when user presses Done on virtual keyboard.")]
    public bool submitOnDone = true;

    private void Awake()
    {
        EnsureInitialized();
    }

    private void EnsureInitialized()
    {
        if (inputField == null)
        {
            inputField = GetComponent<TMP_InputField>();
        }
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage != null)
            {
                originalColor = targetImage.color;
            }
        }
    }

    private void OnEnable()
    {
        EnsureInitialized();
        if (inputField != null)
        {
            inputField.onSelect.AddListener(OnInputFieldSelected);
        }
    }

    private void OnDisable()
    {
        if (inputField != null)
        {
            inputField.onSelect.RemoveListener(OnInputFieldSelected);
        }
        CloseKeyboard();
    }

    private void OnInputFieldSelected(string text)
    {
        OpenKeyboard();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        OpenKeyboard();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OpenKeyboard();
    }

    public void OnSelect(BaseEventData eventData)
    {
        OpenKeyboard();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        // In VR with RayInteractors, releasing the trigger or glancing away causes an immediate OnDeselect event.
        // We only close the keyboard if the overlay keyboard is no longer active.
        if (overlayKeyboard == null || !overlayKeyboard.active)
        {
            SetEditingVisual(false);
        }
    }

    public void OpenKeyboard()
    {
        // Activate input field to show caret and enable text input
        if (inputField != null)
        {
            inputField.ActivateInputField();
            inputField.Select();
        }

        SetEditingVisual(true);

        if (overlayKeyboard != null && overlayKeyboard.active)
        {
            return;
        }

        if (TouchScreenKeyboard.isSupported)
        {
            string placeholderText = "";
            if (inputField.placeholder is TMP_Text placeholderTmp)
            {
                placeholderText = placeholderTmp.text;
            }

            // Open the Meta Quest system keyboard overlay
            overlayKeyboard = TouchScreenKeyboard.Open(
                inputField.text,
                TouchScreenKeyboardType.Default,
                autocorrection: false,
                multiline: inputField.multiLine,
                secure: inputField.contentType == TMP_InputField.ContentType.Password,
                alert: false,
                textPlaceholder: placeholderText
            );
        }
        else
        {
            Debug.Log("[QuestKeyboardHandler] Keyboard requested. Input field activated for input. (System Keyboard overlay will display on Quest hardware).");
        }
    }

    private void Update()
    {
        if (overlayKeyboard == null) return;

        // Note: On Meta Quest, the system keyboard overlay may cause TouchScreenKeyboard to temporarily
        // report LostFocus while the user is typing on the overlay. Do NOT cancel the keyboard on LostFocus.
        if (overlayKeyboard.status == TouchScreenKeyboard.Status.Visible || overlayKeyboard.active)
        {
            if (inputField.text != overlayKeyboard.text)
            {
                inputField.text = overlayKeyboard.text;
                inputField.caretPosition = inputField.text.Length;
            }
        }
        else if (overlayKeyboard.status == TouchScreenKeyboard.Status.Done)
        {
            inputField.text = overlayKeyboard.text;
            inputField.caretPosition = inputField.text.Length;
            inputField.onEndEdit?.Invoke(inputField.text);

            if (submitOnDone)
            {
                inputField.onSubmit?.Invoke(inputField.text);
            }

            CloseKeyboard();
        }
        else if (overlayKeyboard.status == TouchScreenKeyboard.Status.Canceled)
        {
            CloseKeyboard();
        }
    }

    private void SetEditingVisual(bool active)
    {
        EnsureInitialized();
        isEditingActive = active;
        if (targetImage != null)
        {
            targetImage.color = active ? activeBackgroundColor : originalColor;
        }
    }

    public void CloseKeyboard()
    {
        if (overlayKeyboard != null)
        {
            if (overlayKeyboard.active)
            {
                overlayKeyboard.active = false;
            }
            overlayKeyboard = null;
        }
        SetEditingVisual(false);
    }
}
