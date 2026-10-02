using UnityEngine;

public class TourInspectionPanel : MonoBehaviour
{
    public InspectionManager inspection;
    public UnityEngine.UI.Button closeButton;

    private void OnEnable()
    {
        if (inspection == null || closeButton == null) return;
        closeButton.onClick.AddListener(inspection.ExitInspectionState);
    }

    private void OnDisable()
    {
        if (inspection == null || closeButton == null) return;
        closeButton.onClick.RemoveListener(inspection.ExitInspectionState);
    }
}
