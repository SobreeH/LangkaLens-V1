using UnityEngine;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(400)]
    public class MRModelSpawner : MonoBehaviour
    {
        [Header("Site restoration")]
        public GameObject siteModelPrefab;

        [Header("Global Spawn Settings")]
        [Tooltip("If assigned, the model will spawn at this transform's position and rotation.")]
        public Transform modelPlacement;

        public Transform centerEyeCamera;

        [Header("On-site playable area (optional)")]
        [Tooltip("When assigned, models follow the aligned site. Alignment, tracking and the selected footprint mode control playback.")]
        public MRMeshPlayableArea playableArea;
        
        [Tooltip("If true, spawn position is relative to the camera. If false, it uses exact world coordinates.")]
        public bool spawnRelativeToCamera = true;
        
        [Tooltip("The position to spawn the models (either offset from camera, or exact world position).")]
        public Vector3 globalSpawnPosition = new Vector3(0, -0.2f, 1.0f);
        
        [Tooltip("The global scale applied to all spawned models.")]
        public Vector3 globalSpawnScale = Vector3.one;

        [Header("Central model display")]
        [Tooltip("Fit each model proportionally into the site's central rectangle. This is a footprint-based estimate, not a measured historical scale.")]
        public bool fitToCentralOpening;
        [Tooltip("Height of the model base in site-local metres.")]
        public float displayBaseHeight;

        [Min(0.01f)]
        [Tooltip("Uniform multiplier after automatic fitting: 1 = fitted size, 1.05 = 5% larger, 0.95 = 5% smaller. Respawn to apply.")]
        public float fittedScaleMultiplier = 1f;
        [Tooltip("Offset from the fitted position in site-local metres (X sideways, Y up, Z forward). Respawn to apply.")]
        public Vector3 fittedPositionOffset = Vector3.zero;

        [Tooltip("The entrance alignment that must be confirmed before the restoration appears.")]
        public MRMeshReferenceAlignment alignment;

        public bool IsAlignmentDone
        {
            get
            {
                if (playableArea != null && playableArea.developmentMode)
                    return true;
                if (alignment != null)
                    return alignment.IsConfirmed;
                if (playableArea != null && playableArea.alignment != null)
                    return playableArea.alignment.IsConfirmed;
                return true;
            }
        }

        private GameObject _currentSpawnedModel;
        private bool _modelRequestedVisible = true;
        private bool _alignmentWasDone;

        public bool IsModelVisible => isActiveAndEnabled && _currentSpawnedModel != null &&
                                      _currentSpawnedModel.activeInHierarchy;

        private void Start()
        {
            if (alignment == null && playableArea != null)
            {
                alignment = playableArea.alignment;
            }
            if (alignment == null)
            {
                alignment = FindFirstObjectByType<MRMeshReferenceAlignment>();
            }
        }

        private void Update()
        {
            bool alignmentDone = IsAlignmentDone;
            // The A press that confirms alignment must not also hide the model.
            if (_alignmentWasDone && alignmentDone &&
                (alignment == null || !alignment.IsBusy) &&
                OVRInput.GetDown(OVRInput.RawButton.A, OVRInput.Controller.RTouch))
                ToggleModelVisibility();
            _alignmentWasDone = alignmentDone;
        }

        public void ToggleModelVisibility()
        {
            if (_currentSpawnedModel == null || !IsAlignmentDone)
                return;
            _modelRequestedVisible = !_modelRequestedVisible;
        }

        private void OnDisable()
        {
            _alignmentWasDone = false;
        }

        public void SpawnModel()
        {
            if (_currentSpawnedModel != null || !IsAlignmentDone ||
                (playableArea != null && !playableArea.CanPlay) || siteModelPrefab == null)
                return;
            Vector3 finalSpawnPos;
            Quaternion finalRotation;

            if (modelPlacement == null)
            {
                if (playableArea != null)
                {
                    // Try to find Model_Placement inside the playable area first (including inactive ones)
                    Transform[] allTransforms = playableArea.GetComponentsInChildren<Transform>(true);
                    foreach (var t in allTransforms)
                    {
                        if (t.name == "Model_Placement" || t.name == "Model_placement")
                        {
                            modelPlacement = t;
                            break;
                        }
                    }
                }

                if (modelPlacement == null)
                {
                    GameObject mp = GameObject.Find("Model_Placement");
                    if (mp != null) modelPlacement = mp.transform;
                }
            }

            if (modelPlacement != null)
            {
                finalSpawnPos = modelPlacement.position;
                finalRotation = modelPlacement.rotation;
            }
            else if (spawnRelativeToCamera && centerEyeCamera != null)
            {
                // Calculate position relative to the camera
                finalSpawnPos = centerEyeCamera.position 
                              + centerEyeCamera.right * globalSpawnPosition.x 
                              + centerEyeCamera.up * globalSpawnPosition.y 
                              + centerEyeCamera.forward * globalSpawnPosition.z;
                
                // Face the user
                Vector3 lookPos = centerEyeCamera.position;
                lookPos.y = finalSpawnPos.y; // Keep rotation level
                finalRotation = Quaternion.LookRotation(finalSpawnPos - lookPos);
            }
            else
            {
                // Exact world position pinpointing
                finalSpawnPos = globalSpawnPosition;
                finalRotation = Quaternion.identity; 
            }
            
            _currentSpawnedModel = Instantiate(siteModelPrefab, finalSpawnPos, finalRotation);
            _currentSpawnedModel.name = siteModelPrefab.name + "_Spawned";
            
            Transform targetParent = playableArea != null ? playableArea.transform : null;
            if (modelPlacement != null)
            {
                targetParent = modelPlacement;
            }

            if (targetParent != null)
            {
                _currentSpawnedModel.transform.SetParent(targetParent, true);
            }
            
            _currentSpawnedModel.transform.localScale = globalSpawnScale;

            if (fitToCentralOpening && playableArea != null)
                FitModelToOpening(_currentSpawnedModel.transform, playableArea, displayBaseHeight,
                    fittedScaleMultiplier, fittedPositionOffset);

            // Dynamically add physics and interaction components for raw GLB models
            if (_currentSpawnedModel.GetComponent<Collider>() == null)
            {
                BoxCollider bc = _currentSpawnedModel.AddComponent<BoxCollider>();
                if (TryGetBounds(_currentSpawnedModel.transform, _currentSpawnedModel.transform, out Bounds bounds))
                {
                    bc.center = bounds.center;
                    bc.size = bounds.size;
                }
            }

            if (_currentSpawnedModel.GetComponent<Rigidbody>() == null)
            {
                Rigidbody rb = _currentSpawnedModel.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            // If the prefab doesn't have MRSpatialObject, add it (which also adds Grabbable)
            if (playableArea == null && _currentSpawnedModel.GetComponent<MRSpatialObject>() == null)
            {
                _currentSpawnedModel.AddComponent<MRSpatialObject>();
            }
            else if (playableArea != null)
            {
                // Site content follows the common entrance alignment instead of an independent object anchor.
                foreach (var spatialObject in _currentSpawnedModel.GetComponentsInChildren<MRSpatialObject>(true))
                    spatialObject.enabled = false;
            }
        }

        public static bool FitModelToOpening(Transform model, MRMeshPlayableArea area, float baseHeight)
        {
            return FitModelToOpening(model, area, baseHeight, 1f, Vector3.zero);
        }

        public static bool FitModelToOpening(Transform model, MRMeshPlayableArea area, float baseHeight,
            float scaleMultiplier, Vector3 positionOffset)
        {
            Rect opening = area.ruinExclusion;
            if (opening.width <= 0 || opening.height <= 0 ||
                !TryGetBounds(model, area.transform, out Bounds bounds) || bounds.size.x < 0.0001f || bounds.size.z < 0.0001f)
                return false;

            // Preserve the model's proportions and orientation; fit its horizontal footprint.
            float factor = Mathf.Min(opening.width / bounds.size.x, opening.height / bounds.size.z);
            // Scale before centering/grounding so an offset mesh pivot does not shift the fitted base.
            model.localScale *= factor * Mathf.Max(0.01f, scaleMultiplier);
            TryGetBounds(model, area.transform, out bounds);
            Vector3 offset = new Vector3(opening.center.x - bounds.center.x, baseHeight - bounds.min.y,
                opening.center.y - bounds.center.z) + positionOffset;
            model.position += area.transform.TransformVector(offset);
            return true;
        }

        public static bool TryGetBounds(Transform model, Transform space, out Bounds bounds)
        {
            bounds = new Bounds();
            bool found = false;
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>(true))
            {
                Bounds local = renderer.localBounds;
                Matrix4x4 matrix = space.worldToLocalMatrix * renderer.transform.localToWorldMatrix;
                for (int i = 0; i < 8; i++)
                {
                    Vector3 corner = local.center + Vector3.Scale(local.extents,
                        new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                    Vector3 point = matrix.MultiplyPoint3x4(corner);
                    if (!found) { bounds = new Bounds(point, Vector3.zero); found = true; }
                    else bounds.Encapsulate(point);
                }
            }
            return found;
        }

        private void LateUpdate()
        {
            bool canDisplay = IsAlignmentDone && (playableArea == null || playableArea.CanPlay);
            if (canDisplay && _currentSpawnedModel == null)
                SpawnModel();
            bool visible = canDisplay && _modelRequestedVisible;
            if (_currentSpawnedModel != null && _currentSpawnedModel.activeSelf != visible)
                _currentSpawnedModel.SetActive(visible);
        }

        public void DespawnModel()
        {
            if (_currentSpawnedModel != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                {
                    DestroyImmediate(_currentSpawnedModel);
                }
                else
                {
                    Destroy(_currentSpawnedModel);
                }
#else
                Destroy(_currentSpawnedModel);
#endif
                _currentSpawnedModel = null;
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Spawn Demo Model (Editor Only)")]
        private void EditorSpawnDemoModel()
        {
            SpawnModel();
        }

        [ContextMenu("Despawn Demo Model (Editor Only)")]
        private void EditorDespawnDemoModel()
        {
            DespawnModel();
        }
#endif
    }
}
