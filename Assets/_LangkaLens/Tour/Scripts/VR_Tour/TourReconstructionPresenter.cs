using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

[DisallowMultipleComponent]
public class TourReconstructionPresenter : MonoBehaviour
{
    public TourManager tourManager;
    public InspectionManager inspectionManager;
    public TourReconstructionPlaceholder placeholderPrefab;

    [Header("Placeholder placement")]
    [Tooltip("Bring distant icon positions closer along the same direction so the globe stays readable and selectable inside the panorama.")]
    [Min(1f)] public float placeholderMaxDistance = 3f;

    [Header("Inspection approach")]
    [Tooltip("Desired eye distance from the restored model's centre after the player glides closer. Model bounds provide minimum clearance.")]
    [Min(0.5f)] public float inspectionViewingDistance = 0.8f;

    [Header("Inspection panel")]
    [Tooltip("Panel position relative to the user's horizontal view when inspection opens. Positive X is right.")]
    public Vector3 inspectionPanelOffset = new Vector3(1.2f, -0.2f, 1.2f);

    [Header("Inspection backdrop")]
    [FormerlySerializedAs("panoramaFade")]
    [Tooltip("How strongly the panorama loses focus while inspecting. Models and UI remain sharp.")]
    [Range(0f, 1f)] public float panoramaBlur = 0.85f;
    [FormerlySerializedAs("panoramaFadeDuration")]
    [Min(0f)] public float panoramaBlurDuration = 0.3f;

    public enum PresentationState { Unavailable, Ready, Loading, Inspecting }
    public PresentationState State { get; private set; }
    public bool IsBusy => State == PresentationState.Loading || State == PresentationState.Inspecting;
    public bool CanReturnToTourMenu => !IsBusy && Time.frameCount > lastCloseFrame &&
        (inspectionManager == null || inspectionManager.CanExitToMainMenu);
    public GameObject DisplayInstance => displayInstance;
    public TourReconstructionPlaceholder PlaceholderInstance => placeholder;

    private TourNode node;
    private TourReconstructionDefinition currentDefinition;
    private TourReconstructionPlaceholder placeholder;
    private GameObject displayInstance;
    private AsyncOperationHandle<GameObject> displayHandle;
    private int generation;
    private int lastCloseFrame = -1;
    private bool ownsInspection;
    private static readonly int PanoramaBlurId = Shader.PropertyToID("_InspectionBlur");
    private MeshRenderer panoramaRenderer;
    private MaterialPropertyBlock panoramaProperties;
    private float originalPanoramaBlur;
    private float currentPanoramaBlur;
    private float startPanoramaBlur;
    private float targetPanoramaBlur;
    private float panoramaBlurElapsed;

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
            !currentDefinition.IsConfigured || placeholderPrefab == null) return;

        placeholder = Instantiate(placeholderPrefab, node.iconPosition,
            Quaternion.Euler(node.iconRotation), transform);
        placeholder.transform.localScale = node.iconScale;
        PlacePlaceholder();
        placeholder.Setup(this);
        State = PresentationState.Ready;
        RefreshVisibility();
    }

    private void PlacePlaceholder()
    {
        Transform head = tourManager != null ? tourManager.vrCamera : null;
        if (head == null) return;

        // Icon positions default to the panorama origin, which is also the tour eye position.
        // Keep authored bearings, but never put the marker around the eye or on the sphere wall.
        // Renderer bounds update with the transform immediately; physics bounds can still
        // describe the prefab's old position until the next physics synchronization.
        float radius = 0f;
        foreach (var renderer in placeholder.GetComponentsInChildren<Renderer>())
        {
            if (renderer.GetComponent<TMPro.TMP_Text>() != null) continue;
            Bounds bounds = renderer.bounds;
            radius = Mathf.Max(radius, Vector3.Distance(bounds.center, placeholder.transform.position) + bounds.extents.magnitude);
        }
        var camera = head.GetComponent<Camera>();
        float minimumDistance = radius + (camera != null ? camera.nearClipPlane : 0.1f) + 0.1f;
        float maximumDistance = Mathf.Max(minimumDistance, placeholderMaxDistance);
        Vector3 offset = node.iconPosition - head.position;
        if (node.iconPosition.sqrMagnitude < 0.0001f || offset.sqrMagnitude < minimumDistance * minimumDistance)
        {
            Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            offset = forward.normalized * 2f + Vector3.down * 0.2f;
        }
        placeholder.transform.position = head.position + offset.normalized *
            Mathf.Clamp(offset.magnitude, minimumDistance, maximumDistance);
    }

    public void OpenInspection()
    {
        if (!isActiveAndEnabled || State != PresentationState.Ready || node == null ||
            inspectionManager == null || inspectionManager.IsInspecting ||
            tourManager == null || tourManager.IsTransitioning || tourManager.IsMapOpen ||
            tourManager.vrCamera == null || !currentDefinition.IsConfigured) return;

        State = PresentationState.Loading;
        placeholder.SetLoading(true);
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
        placeholder.gameObject.SetActive(false);
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
        State = node != null && currentDefinition != null && currentDefinition.IsConfigured &&
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
        currentDefinition = null;
        CloseInspection();
        // A new panorama or disabled presenter must never inherit inspection blur.
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
        UpdatePanoramaBlur();
    }

    private void FocusPanorama()
    {
        if (panoramaRenderer == null)
        {
            var renderer = tourManager != null ? tourManager.photoSphereRenderer : null;
            if (renderer == null || renderer.sharedMaterial == null ||
                !renderer.sharedMaterial.HasProperty(PanoramaBlurId)) return;
            panoramaRenderer = renderer;
            if (panoramaProperties == null) panoramaProperties = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(panoramaProperties);
            originalPanoramaBlur = panoramaProperties.HasFloat(PanoramaBlurId)
                ? panoramaProperties.GetFloat(PanoramaBlurId) : renderer.sharedMaterial.GetFloat(PanoramaBlurId);
            currentPanoramaBlur = originalPanoramaBlur;
        }
        SetPanoramaBlurTarget(Mathf.Lerp(originalPanoramaBlur, 1f, panoramaBlur));
    }

    private void RestorePanorama(bool immediate = false)
    {
        if (panoramaRenderer == null) return;
        SetPanoramaBlurTarget(originalPanoramaBlur);
        if (!immediate) return;
        ApplyPanoramaBlur(originalPanoramaBlur);
        panoramaRenderer = null;
    }

    private void SetPanoramaBlurTarget(float target)
    {
        startPanoramaBlur = currentPanoramaBlur;
        targetPanoramaBlur = target;
        panoramaBlurElapsed = 0f;
    }

    private void UpdatePanoramaBlur()
    {
        if (panoramaRenderer == null) return;
        panoramaBlurElapsed += Time.unscaledDeltaTime;
        float t = panoramaBlurDuration > 0f ? Mathf.Clamp01(panoramaBlurElapsed / panoramaBlurDuration) : 1f;
        ApplyPanoramaBlur(Mathf.Lerp(startPanoramaBlur, targetPanoramaBlur, Mathf.SmoothStep(0f, 1f, t)));
        if (t >= 1f && !ownsInspection) panoramaRenderer = null;
    }

    private void ApplyPanoramaBlur(float blur)
    {
        currentPanoramaBlur = blur;
        // Only this renderer changes; keep its texture and any other overrides intact.
        panoramaRenderer.GetPropertyBlock(panoramaProperties);
        panoramaProperties.SetFloat(PanoramaBlurId, blur);
        panoramaRenderer.SetPropertyBlock(panoramaProperties);
    }

    private void OnDisable()
    {
        ClearNode();
        if (inspectionManager != null) inspectionManager.InspectionExited -= OnInspectionExited;
    }
}
