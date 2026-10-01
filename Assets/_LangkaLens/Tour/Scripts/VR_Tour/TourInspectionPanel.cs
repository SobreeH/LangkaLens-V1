using UnityEngine;

public class TourInspectionPanel : MonoBehaviour
{
    public InspectionManager inspection;
    public UnityEngine.UI.Button closeButton;
    public UnityEngine.UI.Button zoomInButton;
    public UnityEngine.UI.Button zoomOutButton;

    private void OnEnable()
    {
        if (inspection == null) return;
        closeButton.onClick.AddListener(inspection.ExitInspectionState);
        zoomInButton.onClick.AddListener(inspection.IncreaseScale);
        zoomOutButton.onClick.AddListener(inspection.DecreaseScale);
    }

    private void OnDisable()
    {
        if (inspection == null) return;
        closeButton.onClick.RemoveListener(inspection.ExitInspectionState);
        zoomInButton.onClick.RemoveListener(inspection.IncreaseScale);
        zoomOutButton.onClick.RemoveListener(inspection.DecreaseScale);
    }
}
