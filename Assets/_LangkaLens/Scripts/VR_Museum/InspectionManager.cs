using System.Collections;
using UnityEngine;
using TMPro;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    [Header("UI Elements")]
    public GameObject inspectionCanvas;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;

    // Removed the offset math variables completely

    [Header("VR Rig References")]
    public Transform playerRig;
    public Transform centerEyeCamera;
    public MonoBehaviour locomotionScript;

    [Header("Movement Settings")]
    public float inspectionDistance = 0.6f;
    public float transitionDuration = 0.4f;

    private GameObject _currentArtifact;
    private Transform _currentOrigin;
    private Transform _currentUIAnchor; // <--- NEW
    private Vector3 _originalRigPosition;
    private Quaternion _originalRigRotation;
    private bool _isInspecting = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this.gameObject);
        else Instance = this;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);
    }

    // Notice the new uiAnchor parameter here
    public void EnterInspectionState(GameObject artifact, Transform origin, Transform uiAnchor, string title, string description)
    {
        if (_isInspecting) return;

        _isInspecting = true;
        _currentArtifact = artifact;
        _currentOrigin = origin;
        _currentUIAnchor = uiAnchor; // <--- NEW

        if (locomotionScript != null) locomotionScript.enabled = false;
        
        _originalRigPosition = playerRig.position;
        _originalRigRotation = playerRig.rotation;

        if (titleText != null) titleText.text = title;
        if (descriptionText != null) descriptionText.text = description;

        StartCoroutine(MovePlayerToArtifact());
    }

    public void ExitInspectionState()
    {
        if (!_isInspecting) return;

        if (inspectionCanvas != null) inspectionCanvas.SetActive(false);

        playerRig.position = _originalRigPosition;
        playerRig.rotation = _originalRigRotation;

        if (locomotionScript != null) locomotionScript.enabled = true;

        _currentArtifact = null;
        _currentOrigin = null;
        _currentUIAnchor = null; // <--- NEW
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
        // Simple and clean: just copy the transform of the empty GameObject
        if (inspectionCanvas == null || _currentUIAnchor == null) return;
        
        inspectionCanvas.transform.position = _currentUIAnchor.position;
        inspectionCanvas.transform.rotation = _currentUIAnchor.rotation;
    }
}