using UnityEngine;
using UnityEngine.SceneManagement;
using Oculus.Interaction;

namespace LangkaLens.MR
{
    public class MRModelSpawner : MonoBehaviour
    {
        [Header("Model Prefabs")]
        public GameObject[] modelPrefabs;

        [Header("Global Spawn Settings")]
        public Transform centerEyeCamera;
        
        [Tooltip("If true, spawn position is relative to the camera. If false, it uses exact world coordinates.")]
        public bool spawnRelativeToCamera = true;
        
        [Tooltip("The position to spawn the models (either offset from camera, or exact world position).")]
        public Vector3 globalSpawnPosition = new Vector3(0, -0.2f, 1.0f);
        
        [Tooltip("The global scale applied to all spawned models.")]
        public Vector3 globalSpawnScale = Vector3.one;

        [Header("UI References")]
        [Tooltip("The canvas to hide when a model is spawned.")]
        public GameObject selectionCanvas;

        private GameObject _currentSpawnedModel;

        private void Update()
        {
            // Reset the scene when the 'X' button on the left controller is pressed
            if (OVRInput.GetDown(OVRInput.RawButton.X))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
        }

        public void SpawnModel(int index)
        {
            if (index < 0 || index >= modelPrefabs.Length)
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
                Destroy(_currentSpawnedModel);
            }

            // Hide the canvas
            if (selectionCanvas != null)
            {
                selectionCanvas.SetActive(false);
            }

            Vector3 finalSpawnPos;
            Quaternion finalRotation;

            if (spawnRelativeToCamera && centerEyeCamera != null)
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
            _currentSpawnedModel.transform.localScale = globalSpawnScale;

            // Dynamically add physics and interaction components for raw GLB models
            if (_currentSpawnedModel.GetComponent<Collider>() == null)
            {
                BoxCollider bc = _currentSpawnedModel.AddComponent<BoxCollider>();
                Renderer[] renderers = _currentSpawnedModel.GetComponentsInChildren<Renderer>();
                if (renderers.Length > 0)
                {
                    Bounds bounds = renderers[0].bounds;
                    for (int j = 1; j < renderers.Length; j++)
                    {
                        bounds.Encapsulate(renderers[j].bounds);
                    }
                    bc.center = _currentSpawnedModel.transform.InverseTransformPoint(bounds.center);
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
            if (_currentSpawnedModel.GetComponent<MRSpatialObject>() == null)
            {
                _currentSpawnedModel.AddComponent<MRSpatialObject>();
            }
        }
    }
}
