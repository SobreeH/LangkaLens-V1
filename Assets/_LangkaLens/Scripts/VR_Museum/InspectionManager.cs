using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

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
    private Vector3 _originalRigPosition;
    private Quaternion _originalRigRotation;
    private Vector3 _originalArtifactScale;
    private float _currentScaleMultiplier = 1f;
    private bool _isInspecting = false;
    private Collider _headCollider;

    private LocalizedString _currentTitle;
    private LocalizedString _currentDescription;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
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
        if (toggleModelButton == null && inspectionCanvas != null)
        {
            Transform t = inspectionCanvas.transform.Find("Panel/Toggle_btn");
            if (t != null) toggleModelButton = t.gameObject;
        }
    }

    private void OnTitleChanged(string s) { if (titleText != null) titleText.text = s; }
    private void OnDescriptionChanged(string s) { if (descriptionText != null) descriptionText.text = s; }

    public void EnterInspectionState(GameObject artifact, Transform origin, Transform uiAnchor, GameObject pedestal, LocalizedString title, LocalizedString description)
    {
        if (_isInspecting) return;

        _isInspecting = true;
        _currentArtifact = artifact;
        if (artifact != null) _currentArtifactData = artifact.GetComponentInParent<ArtifactData>();
        _currentOrigin = origin;
        _currentUIAnchor = uiAnchor; 
        
        _currentPedestal = pedestal;
        if (_currentPedestal != null)
        {
            _currentPedestal.SetActive(false);
        }

        if (locomotionScript != null) locomotionScript.enabled = false;
        
        // Ensure all locomotion is completely disabled
        GameObject playerController = GameObject.Find("PlayerController");
        if (playerController != null) playerController.SetActive(false);
        
        if (_headCollider != null) _headCollider.enabled = false;
        
        _originalRigPosition = playerRig.position;
        _originalRigRotation = playerRig.rotation;
        _originalArtifactScale = _currentArtifact.transform.localScale;
        _currentScaleMultiplier = 1f;

        if (titleText != null) titleText.text = "";
        if (descriptionText != null) descriptionText.text = "";

        _currentTitle = title;
        _currentDescription = description;

        if (_currentTitle != null) _currentTitle.StringChanged += OnTitleChanged;
        if (_currentDescription != null) _currentDescription.StringChanged += OnDescriptionChanged;

        UpdateModelStateUI();

        StartCoroutine(MovePlayerToArtifact());
    }

    public void ExitInspectionState()
    {
        if (!_isInspecting) return;

        if (_currentTitle != null) _currentTitle.StringChanged -= OnTitleChanged;
        if (_currentDescription != null) _currentDescription.StringChanged -= OnDescriptionChanged;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);

        playerRig.position = _originalRigPosition;
        playerRig.rotation = _originalRigRotation;

        if (_currentArtifact != null)
        {
            _currentArtifact.transform.localScale = _originalArtifactScale;
        }

        if (_currentPedestal != null)
        {
            _currentPedestal.SetActive(true);
            _currentPedestal = null;
        }

        if (locomotionScript != null) locomotionScript.enabled = true;

        // Re-enable locomotion
        Transform locomotorRoot = playerRig.Find("[BuildingBlock] OVRInteractionComprehensive/Locomotor");
        if (locomotorRoot != null)
        {
            Transform playerController = locomotorRoot.Find("PlayerController");
            if (playerController != null) playerController.gameObject.SetActive(true);
        }

        if (_headCollider != null) _headCollider.enabled = true;

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
    }

    private IEnumerator MovePlayerToArtifact()
    {
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
            }

            // OVRInput.Button.One = A button, Button.Two = B button
            // Button A (zoom out = scale down), Button B (zoom in = scale up)
            bool zoomOut = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
            bool zoomIn = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);

            if (zoomIn || zoomOut)
            {
                float scaleDelta = zoomSpeed * Time.deltaTime;
                if (zoomOut) scaleDelta = -scaleDelta;

                _currentScaleMultiplier += scaleDelta;
                _currentScaleMultiplier = Mathf.Clamp(_currentScaleMultiplier, minScaleMultiplier, maxScaleMultiplier);

                _currentArtifact.transform.localScale = _originalArtifactScale * _currentScaleMultiplier;
            }

            // Toggle Models with 'Y' Button (Button.Four on Left Touch)
            if (OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.LTouch) || 
                OVRInput.GetDown(OVRInput.Button.Four, OVRInput.Controller.Active))
            {
                OnToggleModelButtonClicked();
            }
        }
    }

    public void OnToggleModelButtonClicked()
    {
        if (_currentArtifactData != null)
        {
            _currentArtifactData.ToggleModel();
            UpdateModelStateUI();
        }
    }

    private void UpdateModelStateUI()
    {
        if (_currentArtifactData == null) return;

        bool hasAlternate = _currentArtifactData.alternateModel != null;
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
}