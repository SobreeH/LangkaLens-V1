using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(TMP_InputField))]
public class QuestKeyboardHandler : MonoBehaviour, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    private TMP_InputField inputField;
    private TouchScreenKeyboard overlayKeyboard;

    private void Awake()
    {
        inputField = GetComponent<TMP_InputField>();
    }

    private void OnEnable()
    {
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
        CloseKeyboard();
    }

    public void OpenKeyboard()
    {
        if (TouchScreenKeyboard.isSupported)
        {
            // Hide the default 2D mobile input bar so text appears directly in the VR canvas
            TouchScreenKeyboard.hideInput = true;

            string placeholderText = "";
            if (inputField.placeholder is TMP_Text placeholderTmp)
            {
                placeholderText = placeholderTmp.text;
            }

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
            Debug.Log("[QuestKeyboardHandler] Keyboard requested. Note: Meta Quest System Keyboard overlay is only active when running on the Quest headset.");
        }
    }

    private void Update()
    {
        if (overlayKeyboard == null) return;

        if (overlayKeyboard.status == TouchScreenKeyboard.Status.Visible)
        {
            inputField.text = overlayKeyboard.text;
        }
        else if (overlayKeyboard.status == TouchScreenKeyboard.Status.Done)
        {
            inputField.text = overlayKeyboard.text;
            inputField.onEndEdit?.Invoke(inputField.text);
            overlayKeyboard = null;
        }
        else if (overlayKeyboard.status == TouchScreenKeyboard.Status.Canceled ||
                 overlayKeyboard.status == TouchScreenKeyboard.Status.LostFocus)
        {
            overlayKeyboard = null;
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
    }
}
