using UnityEngine;
using UnityEngine.Localization.Components;

public class TourReconstructionPlaceholder : MonoBehaviour
{
    [SerializeField] private Renderer markerRenderer;
    [SerializeField] private LocalizeStringEvent statusLabel;
    [SerializeField] private Color hoverColor = new Color(0.3f, 0.9f, 1f);

    private TourReconstructionPresenter presenter;
    private MaterialPropertyBlock propertyBlock;
    private Color originalColor;
    private bool isLoading;
    private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

    private void Awake()
    {
        if (CompareTag("EditorOnly"))
        {
            gameObject.SetActive(false);
            return;
        }
        propertyBlock = new MaterialPropertyBlock();
        if (markerRenderer != null && markerRenderer.sharedMaterial != null)
            originalColor = markerRenderer.sharedMaterial.GetColor(BaseColor);
    }

    public void Setup(TourReconstructionPresenter owner)
    {
        presenter = owner;
        SetLoading(false);
    }

    public void OnSelected()
    {
        if (presenter != null && !isLoading) presenter.OpenInspection();
    }

    public void OnHover() => SetColor(hoverColor);
    public void OnUnhover() => SetColor(originalColor);

    public void SetLoading(bool loading)
    {
        isLoading = loading;
        SetStatus(loading ? "reconstruction_loading" : "reconstruction_open");
    }

    public void ShowLoadFailure()
    {
        isLoading = false;
        SetStatus("reconstruction_retry");
    }

    private void SetStatus(string key)
    {
        if (statusLabel != null) statusLabel.StringReference.SetReference("VTour_UI", key);
    }

    private void SetColor(Color color)
    {
        if (markerRenderer == null || propertyBlock == null) return;
        markerRenderer.GetPropertyBlock(propertyBlock);
        propertyBlock.SetColor(BaseColor, color);
        markerRenderer.SetPropertyBlock(propertyBlock);
    }

    private void OnDisable() => OnUnhover();
}
