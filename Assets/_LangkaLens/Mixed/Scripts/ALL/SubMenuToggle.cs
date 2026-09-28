using UnityEngine;

public class SubMenuToggle : MonoBehaviour
{
    [Tooltip("Drag your Menu Canvas here")]
    public GameObject menuCanvas;

    void Update()
    {
        // OVRInput.Button.Start maps to the Menu (hamburger) button on the Left Touch controller
        if (OVRInput.GetDown(OVRInput.Button.Start, OVRInput.Controller.LTouch))
        {
            // Toggles the active state: if it's on, turn it off. If it's off, turn it on.
            if (menuCanvas != null)
            {
                menuCanvas.SetActive(!menuCanvas.activeSelf);
            }
        }
    }
}