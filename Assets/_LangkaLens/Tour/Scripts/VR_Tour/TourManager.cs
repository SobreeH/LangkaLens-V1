using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class TourManager : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("Drag your giant 3D Photo Sphere GameObject here")]
    public MeshRenderer photoSphereRenderer; 
    public GameObject arrowPrefab;
    public GameObject hoverIconPrefab;
    
    [Header("Title Screen")]
    public AssetReferenceTexture2D titleScreenPhoto; 

    [Header("Street View Transition Settings")]
    public float transitionDuration = 0.5f; 
    [Tooltip("How far the camera lunges forward in 3D space.")]
    public float lungeDistance = 3.0f; 

    [Header("Warp Stretch Settings")]
    [Tooltip("Drag the empty GameObject that contains the Photo Sphere here")]
    public Transform photoSphereContainer;
    [Tooltip("How much the image stretches forward during transition")]
    public float stretchMultiplier = 3.0f; 
    [Tooltip("How much the image pinches in on the sides")]
    public float narrowMultiplier = 0.5f; 

    [Header("VR Rig Setup")]
    [Tooltip("Drag the top-level OVRCameraRig or XR Origin here")]
    public Transform vrRigParent;
    [Tooltip("Drag the CenterEyeAnchor or Main Camera here")]
    public Transform vrCamera;

    [Header("UI Menus")]
    public GameObject mapMenuCanvas;
    public GameObject returnHomeCanvas;
    
    [Header("Controller UI")]
    public GameObject[] tourOnlyLabels;

    private TourNode currentNode;
    private AsyncOperationHandle<Texture2D> currentTextureHandle;
    private GameObject currentIconInstance;
    private List<GameObject> activeArrows = new List<GameObject>();
    public IReadOnlyList<GameObject> ActiveArrows => activeArrows;
    public bool IsTransitioning { get; private set; }
    public bool IsAtMainMenu => currentNode == null && !IsTransitioning;

    private GameObject currentOverlayInstance;
    private AsyncOperationHandle<GameObject> currentOverlayHandle;

    private void Start()
    {
        if (mapMenuCanvas != null) mapMenuCanvas.SetActive(true);
        if (returnHomeCanvas != null) returnHomeCanvas.SetActive(false);

        if (titleScreenPhoto.RuntimeKeyIsValid())
        {
            currentTextureHandle = Addressables.LoadAssetAsync<Texture2D>(titleScreenPhoto);
            currentTextureHandle.Completed += OnTitleScreenLoaded;
        }
        
        SetTourLabelsActive(false);
    }

    private void OnTitleScreenLoaded(AsyncOperationHandle<Texture2D> handle)
    {
        if (handle.Status == AsyncOperationStatus.Succeeded && photoSphereRenderer != null)
            photoSphereRenderer.material.mainTexture = handle.Result;
    }

    private void SetTourLabelsActive(bool active)
    {
        if (tourOnlyLabels == null) return;
        foreach (var label in tourOnlyLabels)
        {
            if (label != null) label.SetActive(active);
        }
    }

    public void ReturnToMainMenu()
    {
        if (IsTransitioning) return;
        StartCoroutine(MainMenuRoutine());
    }

    private IEnumerator MainMenuRoutine()
    {
        IsTransitioning = true;
        SetActiveArrowsVisible(false);
        // 0. START LOADING NEW TEXTURE IMMEDIATELY to hide load time behind the transition animation
        AsyncOperationHandle<Texture2D> nextTextureHandle = Addressables.LoadAssetAsync<Texture2D>(titleScreenPhoto);

        Vector3 moveDirection = vrCamera != null ? vrCamera.forward : Vector3.forward;
        moveDirection.y = 0; 
        if (moveDirection.magnitude < 0.001f) moveDirection = Vector3.forward;
        moveDirection.Normalize();

        Vector3 initialRigPos = vrRigParent.position;
        Vector3 lungeTargetPos = initialRigPos + (moveDirection * lungeDistance);

        // Setup the Warp Container
        Quaternion originalSphereRot = Quaternion.identity;
        if (photoSphereRenderer != null) originalSphereRot = photoSphereRenderer.transform.rotation;
        
        Vector3 originalContainerScale = Vector3.one;

        if (photoSphereContainer != null && photoSphereRenderer != null)
        {
            originalContainerScale = photoSphereContainer.localScale;
            photoSphereContainer.rotation = Quaternion.LookRotation(moveDirection);
            // Counter-rotate the child so the 360 photo doesn't spin
            photoSphereRenderer.transform.rotation = originalSphereRot;
        }

        Vector3 targetContainerScale = new Vector3(originalContainerScale.x * narrowMultiplier, originalContainerScale.y * narrowMultiplier, originalContainerScale.z * stretchMultiplier);

        // 1. LUNGE & WARP FORWARD (Ease-In)
        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = t * t; 
            
            if (vrRigParent != null) vrRigParent.position = Vector3.Lerp(initialRigPos, lungeTargetPos, t);
            if (photoSphereContainer != null) photoSphereContainer.localScale = Vector3.Lerp(originalContainerScale, targetContainerScale, t);
            
            yield return null; 
        }

        // 2. CLEANUP
        ClearActiveArrows();
        ClearOverlayModel();
        currentNode = null; 

        if (mapMenuCanvas != null) mapMenuCanvas.SetActive(true);
        if (returnHomeCanvas != null) returnHomeCanvas.SetActive(false);

        SetTourLabelsActive(false);

        // 3. LOAD TITLE SCREEN
        if (currentTextureHandle.IsValid()) Addressables.Release(currentTextureHandle);
        
        currentTextureHandle = nextTextureHandle;
        if (!currentTextureHandle.IsDone)
        {
            yield return currentTextureHandle;
        }

        if (currentTextureHandle.Status == AsyncOperationStatus.Succeeded && photoSphereRenderer != null)
        {
            photoSphereRenderer.material.mainTexture = currentTextureHandle.Result;
        }

        // 4. RESET PLAYER VIEW & WARP CONTAINER
        ResetPlayerView();
        
        if (photoSphereContainer != null && photoSphereRenderer != null)
        {
            photoSphereContainer.localScale = originalContainerScale;
            photoSphereRenderer.transform.rotation = originalSphereRot;
        }

        // 5. SETTLE (Ease-Out)
        Vector3 idealCenterPos = vrRigParent.position;
        Vector3 arrivalStartPos = idealCenterPos - (moveDirection * lungeDistance);

        timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = 1f - (1f - t) * (1f - t); 
            if (vrRigParent != null) vrRigParent.position = Vector3.Lerp(arrivalStartPos, idealCenterPos, t);
            yield return null;
        }
        
        if (vrRigParent != null) vrRigParent.position = idealCenterPos;
        IsTransitioning = false;
    }

    private void ResetPlayerView(bool resetRotation = true)
    {
        if (vrRigParent != null && vrCamera != null)
        {
            if (resetRotation)
            {
                vrRigParent.rotation = Quaternion.identity;
                float currentCameraYaw = vrCamera.eulerAngles.y;
                vrRigParent.Rotate(0, -currentCameraYaw, 0, Space.World);

                Vector3 cameraLocalPos = vrCamera.localPosition;
                vrRigParent.position = -cameraLocalPos; 
            }
            else
            {
                Vector3 worldOffset = vrRigParent.rotation * vrCamera.localPosition;
                vrRigParent.position = -worldOffset;
            }
        }
    }

    public void LoadNode(TourNode nextNode, Vector3? targetWorldPosition = null)
    {
        if (nextNode == null || IsTransitioning) return;
        StartCoroutine(TransitionRoutine(nextNode, targetWorldPosition));
    }

    private IEnumerator TransitionRoutine(TourNode nextNode, Vector3? targetWorldPosition)
    {
        IsTransitioning = true;
        SetActiveArrowsVisible(false);
        // 0. START LOADING NEW TEXTURE IMMEDIATELY to hide load time behind the transition animation
        AsyncOperationHandle<Texture2D> nextTextureHandle = Addressables.LoadAssetAsync<Texture2D>(nextNode.photoReference);

        Vector3 moveDirection;
        if (targetWorldPosition.HasValue) moveDirection = (targetWorldPosition.Value - vrCamera.position);
        else moveDirection = vrCamera != null ? vrCamera.forward : Vector3.forward;
        
        moveDirection.y = 0;
        if (moveDirection.magnitude < 0.001f) moveDirection = Vector3.forward; 
        moveDirection.Normalize();

        Vector3 initialRigPos = vrRigParent.position;
        Vector3 lungeTargetPos = initialRigPos + (moveDirection * lungeDistance);

        // Setup the Warp Container
        Quaternion originalSphereRot = Quaternion.identity;
        if (photoSphereRenderer != null) originalSphereRot = photoSphereRenderer.transform.rotation;
        
        Vector3 originalContainerScale = Vector3.one;

        if (photoSphereContainer != null && photoSphereRenderer != null)
        {
            originalContainerScale = photoSphereContainer.localScale;
            photoSphereContainer.rotation = Quaternion.LookRotation(moveDirection);
            // Counter-rotate the child so the 360 photo doesn't spin
            photoSphereRenderer.transform.rotation = originalSphereRot;
        }

        Vector3 targetContainerScale = new Vector3(originalContainerScale.x * narrowMultiplier, originalContainerScale.y * narrowMultiplier, originalContainerScale.z * stretchMultiplier);

        // 1. LUNGE & WARP FORWARD (Ease-In)
        float timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = t * t; 
            
            if (vrRigParent != null) vrRigParent.position = Vector3.Lerp(initialRigPos, lungeTargetPos, t);
            if (photoSphereContainer != null) photoSphereContainer.localScale = Vector3.Lerp(originalContainerScale, targetContainerScale, t);
            
            yield return null; 
        }

        // 2. CLEANUP ARROWS & 3D MODELS
        ClearActiveArrows();
        ClearOverlayModel(); 

        // 3. LOAD NEW 360 IMAGE
        if (currentTextureHandle.IsValid()) Addressables.Release(currentTextureHandle);

        currentNode = nextNode;
        currentTextureHandle = nextTextureHandle;
        
        if (!currentTextureHandle.IsDone)
        {
            yield return currentTextureHandle; 
        }

        if (currentTextureHandle.Status == AsyncOperationStatus.Succeeded)
        {
            if (photoSphereRenderer != null) photoSphereRenderer.material.mainTexture = currentTextureHandle.Result;
            
            SpawnNavigationArrows();
            
            if (currentNode.overlayModel.RuntimeKeyIsValid())
            {
                LoadOverlayModel();
                SpawnHoverIcon();
            }

            if (mapMenuCanvas != null) mapMenuCanvas.SetActive(false);
            if (returnHomeCanvas != null) returnHomeCanvas.SetActive(true);
            
            SetTourLabelsActive(true);
        }

        // 4. RESET PLAYER VIEW & WARP CONTAINER
        ResetPlayerView(false);
        
        if (photoSphereContainer != null && photoSphereRenderer != null)
        {
            photoSphereContainer.localScale = originalContainerScale;
            photoSphereRenderer.transform.rotation = originalSphereRot;
        }
        
        // 5. SETTLE IN NEW NODE (Ease-Out)
        Vector3 idealCenterPos = vrRigParent.position;
        Vector3 arrivalStartPos = idealCenterPos - (moveDirection * lungeDistance);

        timer = 0f;
        while (timer < transitionDuration)
        {
            timer += Time.deltaTime;
            float t = timer / transitionDuration;
            t = 1f - (1f - t) * (1f - t); 
            if (vrRigParent != null) vrRigParent.position = Vector3.Lerp(arrivalStartPos, idealCenterPos, t);
            yield return null;
        }
        
        if (vrRigParent != null) vrRigParent.position = idealCenterPos;
        IsTransitioning = false;
        SetActiveArrowsVisible(true);
    }

    private void SpawnHoverIcon()
    {
        if (hoverIconPrefab != null)
        {
            currentIconInstance = Instantiate(hoverIconPrefab, currentNode.iconPosition, Quaternion.Euler(currentNode.iconRotation));
            currentIconInstance.transform.localScale = currentNode.iconScale;

            HoverIcon iconScript = currentIconInstance.GetComponent<HoverIcon>();
            if (iconScript != null) iconScript.Setup(this);
        }
    }

    private void LoadOverlayModel()
    {
        currentOverlayHandle = Addressables.InstantiateAsync(
            currentNode.overlayModel, 
            currentNode.modelPosition, 
            Quaternion.Euler(currentNode.modelRotation)
        );

        currentOverlayHandle.Completed += (handle) =>
        {
            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                currentOverlayInstance = handle.Result;
                currentOverlayInstance.transform.localScale = currentNode.modelScale;
                currentOverlayInstance.SetActive(false); 
            }
        };
    }

    private void ClearOverlayModel()
    {
        if (currentOverlayInstance != null)
        {
            Addressables.ReleaseInstance(currentOverlayInstance);
            currentOverlayInstance = null;
        }

        if (currentIconInstance != null) 
        {
            Destroy(currentIconInstance);
            currentIconInstance = null;
        }
    }

    private void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.X))
        {
            if (currentNode != null)
            {
                ReturnToMainMenu();
            }
        }
    }

    public void ShowOverlay()
    {
        if (currentOverlayInstance != null) currentOverlayInstance.SetActive(true);
    }

    public void HideOverlay()
    {
        if (currentOverlayInstance != null) currentOverlayInstance.SetActive(false);
    }
    
    public void ToggleMapMenu()
    {
        if (mapMenuCanvas != null)
        {
            bool isOpeningMap = !mapMenuCanvas.activeSelf;
            mapMenuCanvas.SetActive(isOpeningMap);

            if (returnHomeCanvas != null) returnHomeCanvas.SetActive(!isOpeningMap);

            if (isOpeningMap && currentOverlayInstance != null)
            {
                currentOverlayInstance.SetActive(false);
            }
        }
    }

    private void SpawnNavigationArrows()
    {
        if (currentNode.previousPath != null && currentNode.previousPath.targetNode != null)
            SpawnSingleArrow(currentNode.previousPath);

        if (currentNode.nextPath != null && currentNode.nextPath.targetNode != null)
            SpawnSingleArrow(currentNode.nextPath);
    }

    private void SpawnSingleArrow(NodeConnection connection)
    {
        GameObject newArrow = Instantiate(arrowPrefab, connection.arrowPosition, Quaternion.Euler(connection.arrowRotation));
        activeArrows.Add(newArrow);
        NavigationArrow arrowScript = newArrow.GetComponent<NavigationArrow>();
        if (arrowScript != null) arrowScript.Setup(connection.targetNode, this);
        newArrow.SetActive(!IsTransitioning);
    }

    private void SetActiveArrowsVisible(bool visible)
    {
        foreach (GameObject arrow in activeArrows)
        {
            if (arrow != null) arrow.SetActive(visible);
        }
    }

    private void ClearActiveArrows()
    {
        foreach (GameObject arrow in activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        activeArrows.Clear();
    }
}
