using UnityEngine;

namespace LangkaLens.MR
{
    [DefaultExecutionOrder(400)]
    public class MRModelSpawner : MonoBehaviour
    {
        [Header("Model Prefabs")]
        public GameObject[] modelPrefabs;

        [Header("Global Spawn Settings")]
        [Tooltip("If assigned, the model will spawn at this transform's position and rotation.")]
        public Transform modelPlacement;

        public Transform centerEyeCamera;

        [Header("On-site playable area (optional)")]
        [Tooltip("When assigned, models follow the aligned site and are hidden outside its playable interior.")]
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

        [Header("UI References")]
        [Tooltip("The canvas to hide when a model is spawned.")]
        public GameObject selectionCanvas;

        private GameObject _currentSpawnedModel;

        private void Update()
        {
            // Despawn the model if the 'B' or 'Y' button is pressed
            if (OVRInput.GetDown(OVRInput.RawButton.B) || OVRInput.GetDown(OVRInput.RawButton.Y))
            {
                DespawnModel();
            }
        }

        public void SpawnModel(int index)
        {
            if (playableArea != null && !playableArea.CanPlay)
            {
                Debug.Log("Align the entrance reference and enter the playable interior before selecting a model.");
                return;
            }

            if (modelPrefabs == null || index < 0 || index >= modelPrefabs.Length)
            {
                Debug.LogWarning("Invalid model index.");
                return;
            }

            if (modelPrefabs[index] == null)
            {
                Debug.LogWarning("Model prefab is null.");
                return;
            }

            // Destroy currently spawned model if it exists
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
            }

            // Hide the canvas
            if (selectionCanvas != null)
            {
                selectionCanvas.SetActive(false);
            }

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
            
            _currentSpawnedModel = Instantiate(modelPrefabs[index], finalSpawnPos, finalRotation);
            _currentSpawnedModel.name = modelPrefabs[index].name + "_Spawned";
            
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
                FitModelToOpening(_currentSpawnedModel.transform, playableArea, displayBaseHeight);

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
            Rect opening = area.ruinExclusion;
            if (opening.width <= 0 || opening.height <= 0 ||
                !TryGetBounds(model, area.transform, out Bounds bounds) || bounds.size.x < 0.0001f || bounds.size.z < 0.0001f)
                return false;

            // Preserve the model's proportions and orientation; fit its horizontal footprint.
            float factor = Mathf.Min(opening.width / bounds.size.x, opening.height / bounds.size.z);
            model.localScale *= factor;
            TryGetBounds(model, area.transform, out bounds);
            Vector3 offset = new Vector3(opening.center.x - bounds.center.x, baseHeight - bounds.min.y,
                opening.center.y - bounds.center.z);
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
            if (playableArea != null && _currentSpawnedModel != null && _currentSpawnedModel.activeSelf != playableArea.CanPlay)
                _currentSpawnedModel.SetActive(playableArea.CanPlay);
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

            if (selectionCanvas != null)
            {
                selectionCanvas.SetActive(true);
            }
        }

#if UNITY_EDITOR
        [ContextMenu("Spawn Demo Model (Editor Only)")]
        private void EditorSpawnDemoModel()
        {
            if (modelPrefabs != null && modelPrefabs.Length > 0)
            {
                SpawnModel(0);
            }
            else
            {
                Debug.LogWarning("No model prefabs assigned to spawn.");
            }
        }

        [ContextMenu("Despawn Demo Model (Editor Only)")]
        private void EditorDespawnDemoModel()
        {
            DespawnModel();
        }
#endif
    }
}
