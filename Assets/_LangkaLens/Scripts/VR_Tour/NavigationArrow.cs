using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    private TourNode targetNode;
    private TourManager tourManager;

    public void Setup(TourNode destination, TourManager manager)
    {
        targetNode = destination;
        tourManager = manager;
    }

    public void OnArrowSelected()
    {
        if (tourManager != null && targetNode != null)
        {
            // Pass the arrow's exact position to trigger a directional zoom
            tourManager.LoadNode(targetNode, transform.position);
        }
        else
        {
            Debug.LogWarning("Arrow clicked, but target node or manager is missing!");
        }
    }
}