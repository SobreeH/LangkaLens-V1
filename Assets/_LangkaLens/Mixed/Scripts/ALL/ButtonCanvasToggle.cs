using UnityEngine;

public class ButtonCanvasToggle : MonoBehaviour
{
    [Tooltip("Drag the Canvas you want to open/close here")]
    public GameObject canvasToToggle;

    // This method will be called when the button is clicked
    public void ToggleTargetCanvas()
    {
        if (canvasToToggle != null)
        {
            canvasToToggle.SetActive(!canvasToToggle.activeSelf);
        }
    }
}