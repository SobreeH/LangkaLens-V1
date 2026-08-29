using UnityEngine;

public class NavigationArrow : MonoBehaviour
{
    private TourNode targetNode;
    private TourManager tourManager;
    private MeshRenderer meshRenderer;
    private Color originalColor;
    
    [SerializeField] private Color hoverGlowColor = new Color(0.3f, 0.3f, 0.3f, 1f);

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.material != null)
        {
            if (meshRenderer.material.HasProperty("_EmissionColor"))
            {
                originalColor = meshRenderer.material.GetColor("_EmissionColor");
            }
        }
    }

    public void Setup(TourNode destination, TourManager manager)
    {
        targetNode = destination;
        tourManager = manager;
    }

    public void OnHover()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.EnableKeyword("_EMISSION");
            meshRenderer.material.SetColor("_EmissionColor", hoverGlowColor);
        }
    }

    public void OnUnhover()
    {
        if (meshRenderer != null && meshRenderer.material != null)
        {
            meshRenderer.material.SetColor("_EmissionColor", originalColor);
            if (originalColor == Color.black || originalColor.a == 0)
            {
                meshRenderer.material.DisableKeyword("_EMISSION");
            }
        }
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