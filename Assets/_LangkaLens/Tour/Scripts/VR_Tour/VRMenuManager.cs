using UnityEngine;
using UnityEngine.UI;

public class VRMenuManager : MonoBehaviour
{
    [Header("System References")]
    public TourManager tourManager;
    public GameObject VRMenuCanvas; // We will turn this off when the tour starts

    [Header("Area Data")]
    public TourArea area2;
    public TourArea area3;
    public TourArea area8;
    public TourArea area9;

    [Header("UI Buttons")]
    public Button btnArea2;
    public Button btnArea3;
    public Button btnArea8;
    public Button btnArea9;

    private void Start()
    {
        // Link the physical UI buttons to our code logic
        btnArea2.onClick.AddListener(() => StartTour(area2));
        btnArea3.onClick.AddListener(() => StartTour(area3));
        btnArea8.onClick.AddListener(() => StartTour(area8));
        btnArea9.onClick.AddListener(() => StartTour(area9));
    }

    private void StartTour(TourArea selectedArea)
    {
        if (selectedArea != null && selectedArea.startingNode != null)
        {
            // 1. Tell the Tour Manager to load the first photo (e.g., 2F1)
            tourManager.LoadNode(selectedArea.startingNode);

            // 2. Hide the main menu canvas so the user can see the environment
            VRMenuCanvas.SetActive(false);
        }
        else
        {
            Debug.LogError("The selected area is missing its Starting Node data!");
        }
    }
}