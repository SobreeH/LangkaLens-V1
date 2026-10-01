using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DisallowMultipleComponent]
public class TourReconstructionPresenter : MonoBehaviour
{
    public TourManager tourManager;
    public InspectionManager inspectionManager;
    public TourReconstructionPlaceholder placeholderPrefab;

    [Header("Inspection panel")]
    [Tooltip("Panel position relative to the user's horizontal view when inspection opens. Positive X is right.")]
    public Vector3 inspectionPanelOffset = new Vector3(1.2f, -0.2f, 1.2f);

    [Header("Inspection backdrop")]
    [Tooltip("How far the panorama fades toward black while inspecting. Models and UI stay visible.")]
    [Range(0f, 1f)] public float panoramaFade = 0.85f;
    [Min(0f)] public float panoramaFadeDuration = 0.3f;

    public enum PresentationState { Unavailable, Ready, Loading, Inspecting }
    public PresentationState State { get; private set; }
    public bool IsBusy => State == PresentationState.Loading || State == PresentationState.Inspecting;
    public bool CanReturnToTourMenu => !IsBusy && Time.frameCount > lastCloseFrame &&
        (inspectionManager == null || inspectionManager.CanExitToMainMenu);
    public GameObject DisplayInstance => displayInstance;
    public TourReconstructionPlaceholder PlaceholderInstance => placeholder;

    private TourNode node;
    private TourReconstructionPlaceholder placeholder;
    private GameObject displayInstance;
    private AsyncOperationHandle<GameObject> displayHandle;
    private int generation;
    private int lastCloseFrame = -1;
    private bool ownsInspection;
    private static readonly int PanoramaFadeId = Shader.PropertyToID("_InspectionFade");
    private MeshRenderer panoramaRenderer;
    private MaterialPropertyBlock panoramaProperties;
    private float originalPanoramaFade;
    private float currentPanoramaFade;
    private float startPanoramaFade;
    private float targetPanoramaFade;
    private float panoramaFadeElapsed;

    private void OnEnable()
    {
        if (inspectionManager != null) inspectionManager.InspectionExited += OnInspectionExited;
        if (tourManager != null && tourManager.CurrentNode != null && !tourManager.IsTransitioning)
            EnterNode(tourManager.CurrentNode);
    }

    public void EnterNode(TourNode nextNode)
    {
        ClearNode();
        node = nextNode;
        if (!isActiveAndEnabled || node == null || node.reconstruction == null ||
            !node.reconstruction.IsConfigured || placeholderPrefab == null) return;

        placeholder = Instantiate(placeholderPrefab, node.reconstructionPosition,
            Quaternion.Euler(node.reconstructionRotation), transform);
        placeholder.transform.localScale = node.reconstructionScale;
        placeholder.Setup(this);
        State = PresentationState.Ready;
        RefreshVisibility();
    }

    public void OpenInspection()
    {
        if (!isActiveAndEnabled || State != PresentationState.Ready || node == null ||
            inspectionManager == null || inspectionManager.IsInspecting ||
            tourManager == null || tourManager.IsTransitioning || tourManager.IsMapOpen ||
            tourManager.vrCamera == null || !node.reconstruction.IsConfigured) return;

        State = PresentationState.Loading;
        placeholder.SetLoading(true);
        tourManager.RefreshNavigationVisibility();
        int request = ++generation;
        var definition = node.reconstruction;

        // An inactive staging parent prevents SDK Start/OnEnable before placement and wiring.
        var staging = new GameObject("Reconstruction Loading");
        staging.transform.SetParent(transform, false);
        staging.SetActive(false);
        var operation = Addressables.InstantiateAsync(definition.inspectionPrefab, staging.transform, false);
        operation.Completed += handle => CompleteLoad(handle, request, definition, staging);
    }

    private void CompleteLoad(AsyncOperationHandle<GameObject> handle, int request,
        TourReconstructionDefinition definition, GameObject staging)
    {
        bool accepted = this != null && isActiveAndEnabled && request == generation &&
            State == PresentationState.Loading && node != null && node.reconstruction == definition;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
            if (staging != null) Destroy(staging);
            if (accepted)
            {
                State = PresentationState.Ready;
                placeholder.ShowLoadFailure();
                tourManager.RefreshNavigationVisibility();
            }
            return;
        }
        if (!accepted)
        {
            Addressables.ReleaseInstance(handle);
            if (staging != null) Destroy(staging);
            return;
        }

        displayInstance = handle.Result;
        displayHandle = handle;
        displayInstance.SetActive(false);
        displayInstance.transform.SetParent(transform, false);
        if (staging != null) Destroy(staging);
        var data = displayInstance.GetComponentInChildren<ArtifactData>(true);
        if (data == null || data.modelContainer == null || data.primaryModel == null || data.uiAnchor == null)
        {
            Debug.LogError("Reconstruction display is missing its model or inspection anchors.", definition);
            CloseInspection();
            placeholder.ShowLoadFailure();
            return;
        }

        FitModel(data, definition.displaySize);
        Transform head = tourManager.vrCamera;
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.ProjectOnPlane(head.up, Vector3.up);
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        Quaternion rotation = Quaternion.LookRotation(forward.normalized, Vector3.up);
        Vector3 position = head.position + rotation * Vector3.forward * definition.viewingDistance +
            Vector3.up * definition.verticalOffset;
        displayInstance.transform.SetPositionAndRotation(position, rotation);
        Vector3 panelPosition = head.position + rotation * inspectionPanelOffset;
        Vector3 panelForward = Vector3.ProjectOnPlane(panelPosition - head.position, Vector3.up);
        Quaternion panelRotation = panelForward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(panelForward, Vector3.up) : rotation;
        data.uiAnchor.SetPositionAndRotation(panelPosition, panelRotation);
        displayInstance.SetActive(true);
        if (!inspectionManager.EnterStationaryInspection(data))
        {
            CloseInspection();
            return;
        }
        ownsInspection = true;
        State = PresentationState.Inspecting;
        placeholder.gameObject.SetActive(false);
        FocusPanorama();
    }

    private static void FitModel(ArtifactData data, float size)
    {
        // Prefabs are authored with a centred rotation pivot and a unit-size envelope.
        data.modelContainer.localScale = Vector3.one * Mathf.Max(0.1f, size);
    }

    public void CloseInspection()
    {
        ++generation;
        lastCloseFrame = Time.frameCount;
        if (ownsInspection && inspectionManager != null)
        {
            ownsInspection = false;
            inspectionManager.ExitInspectionState();
        }
        ReleaseDisplay();
        RestorePanorama();
        State = node != null && node.reconstruction != null && node.reconstruction.IsConfigured &&
            placeholder != null ? PresentationState.Ready : PresentationState.Unavailable;
        if (placeholder != null) placeholder.SetLoading(false);
        RefreshVisibility();
        if (tourManager != null) tourManager.RefreshNavigationVisibility();
    }

    private void OnInspectionExited()
    {
        if (!ownsInspection) return;
        ownsInspection = false;
        CloseInspection();
    }

    private void ReleaseDisplay()
    {
        // Disable first to cancel SDK selections before returning Addressables ownership.
        if (displayInstance != null) displayInstance.SetActive(false);
        // Retain the handle so scene destruction cannot hide ownership behind Unity's null check.
        if (displayHandle.IsValid()) Addressables.ReleaseInstance(displayHandle);
        displayHandle = default;
        displayInstance = null;
    }

    public void ClearNode()
    {
        node = null;
        CloseInspection();
        // A new panorama or disabled presenter must never inherit inspection dimming.
        RestorePanorama(true);
        if (placeholder != null)
        {
            placeholder.gameObject.SetActive(false);
            Destroy(placeholder.gameObject);
            placeholder = null;
        }
        State = PresentationState.Unavailable;
    }

    public void RefreshVisibility()
    {
        if (placeholder != null)
            placeholder.gameObject.SetActive(State != PresentationState.Inspecting && tourManager != null &&
                !tourManager.IsTransitioning && !tourManager.IsMapOpen);
    }

    private void Update()
    {
        if (IsBusy && tourManager != null && tourManager.IsMapOpen) CloseInspection();
        if (State == PresentationState.Loading && OVRInput.GetDown(OVRInput.RawButton.X)) CloseInspection();
        UpdatePanoramaFade();
    }

    private void FocusPanorama()
    {
        if (panoramaRenderer == null)
        {
            var renderer = tourManager != null ? tourManager.photoSphereRenderer : null;
            if (renderer == null || renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(PanoramaFadeId)) return;
            panoramaRenderer = renderer;
            if (panoramaProperties == null) panoramaProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(panoramaProperties);
            originalPanoramaFade = panoramaProperties.HasFloat(PanoramaFadeId)
                ? panoramaProperties.GetFloat(PanoramaFadeId) : renderer.sharedMaterial.GetFloat(PanoramaFadeId);
            currentPanoramaFade = originalPanoramaFade;
        }
        SetPanoramaFadeTarget(Mathf.Lerp(originalPanoramaFade, 1f, panoramaFade));
    }

    private void RestorePanorama(bool immediate = false)
    {
        if (panoramaRenderer == null) return;
        SetPanoramaFadeTarget(originalPanoramaFade);
        if (!immediate) return;
        ApplyPanoramaFade(originalPanoramaFade);
        panoramaRenderer = null;
    }

    private void SetPanoramaFadeTarget(float target)
    {
        startPanoramaFade = currentPanoramaFade;
        targetPanoramaFade = target;
        panoramaFadeElapsed = 0f;
    }

    private void UpdatePanoramaFade()
    {
        if (panoramaRenderer == null) return;
        panoramaFadeElapsed += Time.unscaledDeltaTime;
        float t = panoramaFadeDuration > 0f ? Mathf.Clamp01(panoramaFadeElapsed / panoramaFadeDuration) : 1f;
        ApplyPanoramaFade(Mathf.Lerp(startPanoramaFade, targetPanoramaFade, Mathf.SmoothStep(0f, 1f, t)));
        if (t >= 1f && !ownsInspection) panoramaRenderer = null;
    }

    private void ApplyPanoramaFade(float fade)
    {
        currentPanoramaFade = fade;
        // Only this renderer changes; keep its texture and any other overrides intact.
        panoramaRenderer.GetPropertyBlock(panoramaProperties);
        panoramaProperties.SetFloat(PanoramaFadeId, fade);
        panoramaRenderer.SetPropertyBlock(panoramaProperties);
    }

    private void OnDisable()
    {
        ClearNode();
        if (inspectionManager != null) inspectionManager.InspectionExited -= OnInspectionExited;
    }
}
