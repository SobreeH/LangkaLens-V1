using UnityEngine;

public class ArtifactData : MonoBehaviour
{
    [Header("Artifact Details")]
    public string artifactTitle;
    [TextArea(5, 10)] 
    public string artifactDescription;

    [Header("Toggleable Models")]
    public GameObject primaryModel;
    public GameObject alternateModel;
    
    private bool _isShowingPrimary = true;

    [Header("Transforms")]
    [Tooltip("The parent empty GameObject that stays on the podium")]
    public Transform originalOrigin;
    
    [Tooltip("The empty GameObject representing exactly where the UI should appear")]
    public Transform uiAnchor; 

    [Tooltip("The empty GameObject representing where the spatial toggle button should appear")]
    public Transform spatialToggleButtonAnchor;
    
    [Tooltip("The parent transform where the models will be spawned")]
    public Transform modelContainer;

    [Tooltip("The pedestal GameObject to hide during inspection")]
    public GameObject pedestal;

    private void Start()
    {
        if (modelContainer == null)
        {
            modelContainer = transform.Find("ObjectMesh");
        }
    }

    public void TriggerInspection()
    {
        // Pass the modelContainer as the object to be scaled by InspectionManager
        GameObject artifactToScale = modelContainer != null ? modelContainer.gameObject : this.gameObject;
        
        InspectionManager.Instance.EnterInspectionState(
            artifactToScale, 
            originalOrigin, 
            uiAnchor, 
            pedestal,
            artifactTitle, 
            artifactDescription
        );
    }

    public void ToggleModel()
    {
        if (primaryModel == null || alternateModel == null) return;
        
        _isShowingPrimary = !_isShowingPrimary;
        
        primaryModel.SetActive(_isShowingPrimary);
        alternateModel.SetActive(!_isShowingPrimary);
    }

    public void ResetToPrimaryModel()
    {
        if (!_isShowingPrimary)
        {
            ToggleModel();
        }
    }
}