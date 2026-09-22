using UnityEngine;
using UnityEngine.Localization;

public class ArtifactData : MonoBehaviour
{
    [Header("Artifact Details")]
    public LocalizedString localizedTitle;
    public LocalizedString localizedDescription;

    [Header("Toggleable Models")]
    public GameObject primaryModel;
    public GameObject alternateModel;
    
    private bool _isShowingPrimary = true;
    public bool IsShowingPrimary => _isShowingPrimary;

    [Header("Transforms")]
    [Tooltip("The parent empty GameObject that stays on the podium")]
    public Transform originalOrigin;
    
    [Tooltip("The empty GameObject representing exactly where the UI should appear")]
    public Transform uiAnchor; 

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
            localizedTitle, 
            localizedDescription
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