using UnityEngine;

public class ArtifactData : MonoBehaviour
{
    [Header("Artifact Details")]
    public string artifactTitle;
    [TextArea(5, 10)] 
    public string artifactDescription;

    [Header("Transforms")]
    [Tooltip("The parent empty GameObject that stays on the podium")]
    public Transform originalOrigin;
    
    [Tooltip("The empty GameObject representing exactly where the UI should appear")]
    public Transform uiAnchor; // <--- NEW

    public void TriggerInspection()
    {
        InspectionManager.Instance.EnterInspectionState(
            this.gameObject, 
            originalOrigin, 
            uiAnchor, // <--- NEW
            artifactTitle, 
            artifactDescription
        );
    }
}