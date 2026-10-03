using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }
    public event System.Action InspectionExited;

    [Header("UI Elements")]
    public GameObject inspectionCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI modelStateText;
    public LocalizeStringEvent modelStateLocalizeEvent;
    public GameObject toggleModelButton;
    
    [Header("VR Rig References")]
    public Transform playerRig;
    public Transform centerEyeCamera;
    public MonoBehaviour locomotionScript;

    [Header("Movement Settings")]
    public float inspectionDistance = 0.6f;
    public float transitionDuration = 0.4f;

    [Header("Zoom Settings")]
    [Tooltip("Speed at which the model scales up and down.")]
    public float zoomSpeed = 1f;
    [Tooltip("The minimum scale multiplier for the model.")]
    public float minScaleMultiplier = 0.5f;
    [Tooltip("The maximum scale multiplier for the model.")]
    public float maxScaleMultiplier = 3.0f;

    private GameObject _currentArtifact;
    private ArtifactData _currentArtifactData;
    private Transform _currentOrigin;
    private Transform _currentUIAnchor; // <--- NEW
    private GameObject _currentPedestal;
    private GameObject _currentInfoIcon;
    private Vector3 _originalRigPosition;
    private Quaternion _originalRigRotation;
    private Vector3 _originalArtifactScale;
    private float _currentScaleMultiplier = 1f;
    private bool _isInspecting = false;
    public bool IsInspecting => _isInspecting;
    public bool IsTourInspection => _isInspecting && _stationaryInspection;
    public bool IsStationaryInspection => IsTourInspection && !_restoreRigOnExit;
    public bool IsMovingToInspection { get; private set; }
    public bool CanToggleInspectedModel => _isInspecting && !_stationaryInspection && _currentArtifactData != null && _currentArtifactData.alternateModel != null;
    private bool _stationaryInspection;
    private bool _restoreRigOnExit;
    private bool _locomotionWasEnabled;
    private bool _headColliderWasEnabled;
    private GameObject _playerController;
    private bool _playerControllerWasActive;
    private int _lastInspectionExitFrame = -1;
    // Keep the X press that exits inspection out of the main-menu confirmation,
    // regardless of which component receives Update first.
    public bool CanExitToMainMenu => !_isInspecting && Time.frameCount > _lastInspectionExitFrame;
    private Collider _headCollider;

    private LocalizedString _currentTitle;
    private LocalizedString _currentDescription;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        else Instance = this;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);

        if (centerEyeCamera != null)
        {
            _headCollider = centerEyeCamera.GetComponent<Collider>();
        }

        if (modelStateLocalizeEvent == null && modelStateText != null)
        {
            modelStateLocalizeEvent = modelStateText.GetComponent<LocalizeStringEvent>();
        }
        
        // Ensure manual binding of the localization event to bypass any broken YAML UnityEvent links
        if (modelStateLocalizeEvent != null && modelStateText != null)
        {
            modelStateLocalizeEvent.OnUpdateString.AddListener((s) => { modelStateText.text = s; });
        }

        if (toggleModelButton == null && inspectionCanvas != null)
        {
            Transform t = inspectionCanvas.transform.Find("Panel/Toggle_btn");
            if (t != null) toggleModelButton = t.gameObject;
        }

        if (toggleModelButton != null)
        {
            var btnText = toggleModelButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (btnText != null)
            {
                var btnLocalize = btnText.GetComponent<LocalizeStringEvent>();
                if (btnLocalize != null)
                {
                    btnLocalize.OnUpdateString.AddListener((s) => { btnText.text = s; });
                }
            }
        }
    }

    private void OnTitleChanged(string s) { if (titleText != null) titleText.text = s; }
    private void OnDescriptionChanged(string s) { if (descriptionText != null) descriptionText.text = s; }

    public void EnterInspectionState(GameObject artifact, Transform origin, Transform uiAnchor, GameObject pedestal, LocalizedString title, LocalizedString description)
    {
        EnterInspectionState(artifact, origin, uiAnchor, pedestal, null, title, description);
    }

    public void EnterInspectionState(GameObject artifact, Transform origin, Transform uiAnchor, GameObject pedestal, GameObject infoIcon, LocalizedString title, LocalizedString description)
    {
        BeginInspection(artifact, origin, uiAnchor, pedestal, infoIcon, title, description, false);
    }

    public bool EnterStationaryInspection(ArtifactData data)
    {
        if (data == null || data.modelContainer == null || data.uiAnchor == null) return false;
        return BeginInspection(data.modelContainer.gameObject, data.originalOrigin, data.uiAnchor,
            null, null, data.localizedTitle, data.localizedDescription, true);
    }

    public bool EnterTourInspection(ArtifactData data, Vector3 targetEyePosition)
    {
        if (data == null || data.modelContainer == null || data.uiAnchor == null) return false;
        return BeginInspection(data.modelContainer.gameObject, data.originalOrigin, data.uiAnchor,
            null, null, data.localizedTitle, data.localizedDescription, true, targetEyePosition);
    }

    private bool BeginInspection(GameObject artifact, Transform origin, Transform uiAnchor,
        GameObject pedestal, GameObject infoIcon, LocalizedString title, LocalizedString description, bool stationary,
        Vector3? targetEyePosition = null)
    {
        if (!isActiveAndEnabled || _isInspecting || artifact == null || playerRig == null ||
            centerEyeCamera == null || inspectionCanvas == null || (!stationary && origin == null)) return false;

        _isInspecting = true;
        _stationaryInspection = stationary;
        _restoreRigOnExit = !stationary || targetEyePosition.HasValue;
        _currentArtifact = artifact;
        if (artifact != null) _currentArtifactData = artifact.GetComponentInParent<ArtifactData>();
        _currentOrigin = origin;
        _currentUIAnchor = uiAnchor; 
        
        _currentPedestal = pedestal;
        if (_currentPedestal != null)
        {
            _currentPedestal.SetActive(false);
        }

        _currentInfoIcon = infoIcon;
        if (!stationary && _currentInfoIcon == null && _currentArtifactData != null)
        {
            _currentInfoIcon = _currentArtifactData.infoIcon;
        }
        if (_currentInfoIcon != null)
        {
            _currentInfoIcon.SetActive(false);
        }

        if (locomotionScript != null)
        {
            _locomotionWasEnabled = locomotionScript.enabled;
            locomotionScript.enabled = false;
        }
        // Pause locomotion on this rig while either inspection mode moves it.
        _playerController = null;
        if (_restoreRigOnExit)
        {
            Transform controller = playerRig.Find("[BuildingBlock] OVRInteractionComprehensive/Locomotor/PlayerController");
            if (controller != null)
            {
                _playerController = controller.gameObject;
                _playerControllerWasActive = _playerController.activeSelf;
                _playerController.SetActive(false);
            }
        }
        if (_headCollider != null)
        {
            _headColliderWasEnabled = _headCollider.enabled;
            _headCollider.enabled = false;
        }
        
        _originalRigPosition = playerRig.position;
        _originalRigRotation = playerRig.rotation;
        _originalArtifactScale = _currentArtifact.transform.localScale;
        _currentScaleMultiplier = 1f;

        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";

        _currentTitle = title;
        _currentDescription = description;

        if (_currentTitle != null && !_currentTitle.IsEmpty) _currentTitle.StringChanged += OnTitleChanged;
        if (_currentDescription != null && !_currentDescription.IsEmpty) _currentDescription.StringChanged += OnDescriptionChanged;

        UpdateModelStateUI();

        if (targetEyePosition.HasValue)
            StartCoroutine(MovePlayerForTourInspection(targetEyePosition.Value));
        else if (stationary)
        {
            PositionCanvas();
            inspectionCanvas.SetActive(true);
        }
        else StartCoroutine(MovePlayerToArtifact());
        return true;
    }

    public void ExitInspectionState()
    {
        if (!_isInspecting) return;

        _lastInspectionExitFrame = Time.frameCount;
        StopAllCoroutines();
        IsMovingToInspection = false;

        if (_currentTitle != null) _currentTitle.StringChanged -= OnTitleChanged;
        if (_currentDescription != null) _currentDescription.StringChanged -= OnDescriptionChanged;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);

        if (_restoreRigOnExit && playerRig != null)
        {
            playerRig.position = _originalRigPosition;
            playerRig.rotation = _originalRigRotation;
        }

        if (_currentArtifact != null)
        {
            _currentArtifact.transform.localScale = _originalArtifactScale;
        }

        if (_currentPedestal != null)
        {
            _currentPedestal.SetActive(true);
            _currentPedestal = null;
        }

        if (_currentInfoIcon != null)
        {
            _currentInfoIcon.SetActive(true);
            _currentInfoIcon = null;
        }

        if (locomotionScript != null) locomotionScript.enabled = _locomotionWasEnabled;
        if (_playerController != null) _playerController.SetActive(_playerControllerWasActive);
        _playerController = null;
        if (_headCollider != null) _headCollider.enabled = _headColliderWasEnabled;

        _currentArtifact = null;
        if (_currentArtifactData != null)
        {
            // Reset to primary model on exit if needed
            _currentArtifactData.ResetToPrimaryModel();
            _currentArtifactData = null;
        }

        _currentOrigin = null;
        _currentUIAnchor = null; 
        _isInspecting = false;
        _stationaryInspection = false;
        _restoreRigOnExit = false;
        _currentTitle = null;
        _currentDescription = null;
        InspectionExited?.Invoke();
    }

    private IEnumerator MovePlayerForTourInspection(Vector3 targetEyePosition)
    {
        IsMovingToInspection = true;
        Vector3 start = playerRig.position;
        Vector3 movement = targetEyePosition - centerEyeCamera.position;
        movement.y = 0f;
        Vector3 target = start + movement;
        float elapsed = 0f;
        while (elapsed < transitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);
            playerRig.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
        playerRig.position = target;
        IsMovingToInspection = false;
        PositionCanvas();
        inspectionCanvas.SetActive(true);
    }

    private IEnumerator MovePlayerToArtifact()
    {
        IsMovingToInspection = true;
        Vector3 startRigPos = playerRig.position;
        Quaternion startRigRot = playerRig.rotation;

        Vector3 artifactPosFlat = new Vector3(_currentOrigin.position.x, 0, _currentOrigin.position.z);
        
        // Grab the forward direction of the pedestal (ignoring upward/downward tilt)
        Vector3 artifactForwardFlat = new Vector3(_currentOrigin.forward.x, 0, _currentOrigin.forward.z).normalized;

        // Place the camera at the set distance perfectly in front of the artifact's forward axis
        Vector3 targetCameraPos = artifactPosFlat + (artifactForwardFlat * inspectionDistance);
        targetCameraPos.y = centerEyeCamera.position.y; 

        // Move the rig so the camera hits that exact spot
        Vector3 rigOffset = playerRig.position - centerEyeCamera.position;
        rigOffset.y = 0; 
        Vector3 targetRigPos = targetCameraPos + rigOffset;
        targetRigPos.y = playerRig.position.y; 

        // Force the player to look exactly opposite to the pedestal's forward direction (facing it head-on)
        Vector3 lookDir = -artifactForwardFlat;
        Quaternion targetCameraRot = Quaternion.LookRotation(lookDir);
        float yRotationOffset = playerRig.eulerAngles.y - centerEyeCamera.eulerAngles.y;
        Quaternion targetRigRot = Quaternion.Euler(0, targetCameraRot.eulerAngles.y + yRotationOffset, 0);

        float elapsedTime = 0f;
        while (elapsedTime < transitionDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, elapsedTime / transitionDuration);

            playerRig.position = Vector3.Lerp(startRigPos, targetRigPos, t);
            playerRig.rotation = Quaternion.Lerp(startRigRot, targetRigRot, t);

            yield return null;
        }

        playerRig.position = targetRigPos;
        playerRig.rotation = targetRigRot;
        IsMovingToInspection = false;

        PositionCanvas();
        inspectionCanvas.SetActive(true);
    }

    private void PositionCanvas()
    {
        if (inspectionCanvas != null && _currentUIAnchor != null)
        {
            inspectionCanvas.transform.position = _currentUIAnchor.position;
            inspectionCanvas.transform.rotation = _currentUIAnchor.rotation;
        }

    }

    private void Update()
    {
        if (_isInspecting && _currentArtifact != null)
        {
            // Exit inspection with X button on Left Controller
            if (OVRInput.GetDown(OVRInput.RawButton.X))
            {
                ExitInspectionState();
                return;
            }

            // OVRInput.Button.One = A button, Button.Two = B button
            // Button A (zoom out = scale down), Button B (zoom in = scale up)
            bool zoomOut = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bool zoomIn = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);

            if (zoomIn || zoomOut)
            {
                float scaleDelta = zoomSpeed * Time.deltaTime;
                if (zoomOut) scaleDelta = -scaleDelta;

                AdjustScale(scaleDelta);
            }

            // Toggle Models with 'Y' Button (Button.Four on Left Touch)
            if (CanToggleInspectedModel && (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.LTouch) ||
                OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.Active)))
            {
                OnToggleModelButtonClicked();
            }
        }
    }

    public void OnToggleModelButtonClicked()
    {
        if (CanToggleInspectedModel)
        {
            _currentArtifactData.ToggleModel();
            UpdateModelStateUI();
        }
    }

    private void UpdateModelStateUI()
    {
        bool hasAlternate = CanToggleInspectedModel;
        if (toggleModelButton != null) toggleModelButton.SetActive(hasAlternate);
        if (modelStateText != null) modelStateText.gameObject.SetActive(hasAlternate);

        if (!hasAlternate) return;

        bool isPrimary = _currentArtifactData.IsShowingPrimary;
        string key = isPrimary ? "model_state_current" : "model_state_original";

        if (modelStateLocalizeEvent != null)
        {
            modelStateLocalizeEvent.StringReference.SetReference("Artifacts_UI", key);
            modelStateLocalizeEvent.RefreshString();
        }
        else if (modelStateText != null)
        {
            modelStateText.text = isPrimary ? "Current Model" : "Original Model";
        }
    }

    public void IncreaseScale() => AdjustScale(0.15f);
    public void DecreaseScale() => AdjustScale(-0.15f);

    public void AdjustScale(float delta)
    {
        if (!_isInspecting || _currentArtifact == null) return;
        _currentScaleMultiplier = Mathf.Clamp(_currentScaleMultiplier + delta, minScaleMultiplier, maxScaleMultiplier);
        _currentArtifact.transform.localScale = _originalArtifactScale * _currentScaleMultiplier;
    }

    private void OnDisable() => ExitInspectionState();

    private void OnDestroy()
    {
        ExitInspectionState();
        if (Instance == this) Instance = null;
    }
}
