using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DisallowMultipleComponent]
public class TourReconstructionPresenter : MonoBehaviour
{
    public TourManager tourManager;
    public InspectionManager inspectionManager;
    [Header("Inspection approach")]
    [Tooltip("Desired eye distance from the restored model's centre after the player glides closer. Model bounds provide minimum clearance.")]
    [Min(0.5f)] public float inspectionViewingDistance = 0.8f;

    [Header("Inspection panel")]
    [Tooltip("Panel position relative to the user's horizontal view when inspection opens. Positive X is right.")]
    public Vector3 inspectionPanelOffset = new Vector3(1.2f, -0.2f, 1.2f);

    [Header("Inspection backdrop")]
    [FormerlySerializedAs("panoramaBlur")]
    [Tooltip("How strongly the panorama fades toward the canvas palette while inspecting. Models and UI stay clear.")]
    [Range(0f, 1f)] public float panoramaFade = 0.85f;
    [FormerlySerializedAs("panoramaBlurDuration")]
    [Min(0f)] public float panoramaFadeDuration = 0.3f;

    [Tooltip("Background tint matching the canvas Back button (#533A2D).")]
    public Color panoramaFadeColor = new Color(83f / 255f, 58f / 255f, 45f / 255f, 1f);

    public enum PresentationState { Unavailable, Ready, Loading, Inspecting }
    public PresentationState State { get; private set; }
    public bool IsBusy => State == PresentationState.Loading || State == PresentationState.Inspecting;
    public bool CanReturnToTourMenu => !IsBusy && Time.frameCount > lastCloseFrame &&
        (inspectionManager == null || inspectionManager.CanExitToMainMenu);
    public GameObject DisplayInstance => displayInstance;
    public bool CanToggleInspection => isActiveAndEnabled && node != null && currentDefinition != null &&
        currentDefinition.IsConfigured && tourManager != null && !tourManager.IsTransitioning &&
        !tourManager.IsMapOpen && !tourManager.IsAtMainMenu && tourManager.vrCamera != null &&
        inspectionManager != null && (State == PresentationState.Loading || ownsInspection ||
            (State == PresentationState.Ready && !inspectionManager.IsInspecting));

    private TourNode node;
    private TourReconstructionDefinition currentDefinition;
    private GameObject displayInstance;
    private AsyncOperationHandle<GameObject> displayHandle;
    private int generation;
    private int lastCloseFrame = -1;
    private bool ownsInspection;
    private static readonly int PanoramaFadeId = Shader.PropertyToID("_InspectionFade");
    private static readonly int PanoramaFadeColorId = Shader.PropertyToID("_InspectionFadeColor");
    private Color originalPanoramaFadeColor;
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
            EnterNode(tourManager.CurrentNode, tourManager.currentArea?.reconstruction);
    }

    public void EnterNode(TourNode nextNode, TourReconstructionDefinition definition)
    {
        ClearNode();
        node = nextNode;
        currentDefinition = definition;
        if (!isActiveAndEnabled || node == null || currentDefinition == null ||
            !currentDefinition.IsConfigured) return;

        State = PresentationState.Ready;
    }

    public void ToggleInspection()
    {
        if (!CanToggleInspection) return;
        if (State == PresentationState.Loading || State == PresentationState.Inspecting)
            CloseInspection();
        else
            OpenInspection();
    }

    public void OpenInspection()
    {
        if (!CanToggleInspection || State != PresentationState.Ready) return;

        State = PresentationState.Loading;
        tourManager.RefreshNavigationVisibility();
        int request = ++generation;
        var definition = currentDefinition;

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
            State == PresentationState.Loading && node != null && currentDefinition == definition;
        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Addressables.Release(handle);
            if (staging != null) Destroy(staging);
            if (accepted)
            {
                State = PresentationState.Ready;
                Debug.LogWarning("Reconstruction loading failed. Press left Y to retry.", definition);
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
            Debug.LogWarning("Reconstruction display is invalid. Press left Y to retry.", definition);
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
        Vector3 targetEyePosition = InspectionEyePosition(data, head.position);
        Vector3 panelPosition = targetEyePosition + rotation * inspectionPanelOffset;
        Vector3 panelForward = Vector3.ProjectOnPlane(panelPosition - targetEyePosition, Vector3.up);
        Quaternion panelRotation = panelForward.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(panelForward, Vector3.up) : rotation;
        data.uiAnchor.SetPositionAndRotation(panelPosition, panelRotation);
        displayInstance.SetActive(true);
        if (!inspectionManager.EnterTourInspection(data, targetEyePosition))
        {
            CloseInspection();
            return;
        }
        ownsInspection = true;
        State = PresentationState.Inspecting;
        FocusPanorama();
    }

    private Vector3 InspectionEyePosition(ArtifactData data, Vector3 eyePosition)
    {
        Vector3 centre = data.modelContainer.position;
        float radius = 0f;
        foreach (var renderer in data.primaryModel.GetComponentsInChildren<Renderer>(true))
        {
            Bounds bounds = renderer.bounds;
            float centreOffset = Vector3.ProjectOnPlane(bounds.center - centre, Vector3.up).magnitude;
            radius = Mathf.Max(radius, centreOffset + new Vector2(bounds.extents.x, bounds.extents.z).magnitude);
        }
        // Include the largest allowed zoom so rotating/scaling cannot put the model around the eye.
        float distance = Mathf.Max(inspectionViewingDistance,
            radius * Mathf.Max(1f, inspectionManager.maxScaleMultiplier) + 0.15f);
        Vector3 direction = Vector3.ProjectOnPlane(centre - eyePosition, Vector3.up);
        float approach = Mathf.Max(0f, direction.magnitude - distance);
        return eyePosition + direction.normalized * approach;
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
        State = node != null && currentDefinition != null && currentDefinition.IsConfigured
            ? PresentationState.Ready : PresentationState.Unavailable;
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
        currentDefinition = null;
        CloseInspection();
        // A new panorama or disabled presenter must never inherit the inspection tint.
        RestorePanorama(true);
        State = PresentationState.Unavailable;
    }

    private void Update()
    {
        if (IsBusy && tourManager != null && tourManager.IsMapOpen) CloseInspection();
        if (State == PresentationState.Loading && OVRInput.GetDown(OVRInput.RawButton.X)) CloseInspection();
        if (OVRInput.GetDown(OVRInput.RawButton.Y, OVRInput.Controller.LTouch)) ToggleInspection();
        UpdatePanoramaFade();
    }

    private void FocusPanorama()
    {
        if (panoramaRenderer == null)
        {
            var renderer = tourManager != null ? tourManager.photoSphereRenderer : null;
            if (renderer == null || renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(PanoramaFadeId) ||
                !renderer.sharedMaterial.HasProperty(PanoramaFadeColorId)) return;
            panoramaRenderer = renderer;
            if (panoramaProperties == null) panoramaProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(panoramaProperties);
            originalPanoramaFade = panoramaProperties.HasFloat(PanoramaFadeId)
                ? panoramaProperties.GetFloat(PanoramaFadeId) : renderer.sharedMaterial.GetFloat(PanoramaFadeId);
            originalPanoramaFadeColor = panoramaProperties.HasColor(PanoramaFadeColorId)
                ? panoramaProperties.GetColor(PanoramaFadeColorId) : renderer.sharedMaterial.GetColor(PanoramaFadeColorId);
            currentPanoramaFade = originalPanoramaFade;
        }
        SetPanoramaFadeTarget(Mathf.Lerp(originalPanoramaFade, 1f, panoramaFade));
    }

    private void RestorePanorama(bool immediate = false)
    {
        if (panoramaRenderer == null) return;
        SetPanoramaFadeTarget(originalPanoramaFade);
        if (!immediate) return;
        ApplyPanoramaFade(originalPanoramaFade, true);
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
        ApplyPanoramaFade(Mathf.Lerp(startPanoramaFade, targetPanoramaFade, Mathf.SmoothStep(0f, 1f, t)), t >= 1f && !ownsInspection);
        if (t >= 1f && !ownsInspection) panoramaRenderer = null;
    }

    private void ApplyPanoramaFade(float fade, bool restoreColor = false)
    {
        currentPanoramaFade = fade;
        // Only this renderer changes; keep its texture and any other overrides intact.
        panoramaRenderer.GetPropertyBlock(panoramaProperties);
        panoramaProperties.SetFloat(PanoramaFadeId, fade);
        panoramaProperties.SetColor(PanoramaFadeColorId, restoreColor ? originalPanoramaFadeColor : panoramaFadeColor);
        panoramaRenderer.SetPropertyBlock(panoramaProperties);
    }

    private void OnDisable()
    {
        ClearNode();
        if (inspectionManager != null) inspectionManager.InspectionExited -= OnInspectionExited;
    }
}
