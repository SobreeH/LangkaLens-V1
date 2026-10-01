using UnityEngine;

public class HoverIcon : MonoBehaviour
{
    private TourManager tourManager;

    // Called by TourManager when the icon is spawned
    public void Setup(TourManager manager)
    {
        tourManager = manager;
    }

    // Link this to your Meta XR Wrapper's Hover Event!
    public void OnHoverEnter()
    {
    }

    // Link this to your Meta XR Wrapper's Unhover Event!
    public void OnHoverExit()
    {
    }
}