using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Localization;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject inspectionCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    
    [Header("Spatial Toggle UI")]
    [Tooltip("The prefab for the 3D toggle button")]
    public GameObject spatialToggleButtonPrefab;
    
    private GameObject _spawnedToggleButton;

    // Removed the offset math variables completely

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

    private LocalizedString _currentTitle;
    private LocalizedString _currentDescription;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);

        if (spatialToggleButtonPrefab != null)
        {
            _spawnedToggleButton = Instantiate(spatialToggleButtonPrefab);
            _spawnedToggleButton.SetActive(false);
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

        if (_spawnedToggleButton != null)
        {
            _spawnedToggleButton.SetActive(true);
        }

        if (locomotionScript != null) locomotionScript.enabled = false;
        
        // Ensure all locomotion is completely disabled
        GameObject playerController = GameObject.Find("PlayerController");
        if (playerController != null) playerController.SetActive(false);
        
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

        _currentArtifact = null;
        if (_currentArtifactData != null)
        {
            // Reset to primary model on exit if needed
            _currentArtifactData.ResetToPrimaryModel();
            _currentArtifactData = null;
        }

        if (_spawnedToggleButton != null)
        {
            _spawnedToggleButton.SetActive(false);
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

        if (_spawnedToggleButton != null && _currentArtifactData != null && _currentArtifactData.spatialToggleButtonAnchor != null)
        {
            _spawnedToggleButton.transform.position = _currentArtifactData.spatialToggleButtonAnchor.position;
            _spawnedToggleButton.transform.rotation = _currentArtifactData.spatialToggleButtonAnchor.rotation;
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
                if (_currentArtifactData != null)
                {
                    _currentArtifactData.ToggleModel();
                }
            }
        }
    }

    public void OnToggleModelButtonClicked()
    {
        if (_isInspecting && _currentArtifactData != null)
        {
            _currentArtifactData.ToggleModel();
        }
    }
}