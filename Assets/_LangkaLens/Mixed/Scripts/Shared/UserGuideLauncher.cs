using UnityEngine;

/// <summary>Owns one reusable guide window for this tablet and its scene.</summary>
[DisallowMultipleComponent]
public sealed class UserGuideLauncher : MonoBehaviour
{
    [SerializeField] private UserGuideCanvas guidePrefab;
    private UserGuideCanvas instance;

    public void OpenGuide()
    {
        if (guidePrefab == null)
        {
            Debug.LogError("The tablet user guide prefab is missing.", this);
            return;
        }

        if (instance == null)
        {
            var rig = GetComponentInParent<OVRCameraRig>();
            Transform viewer = rig != null ? rig.centerEyeAnchor :
                Camera.main != null ? Camera.main.transform : null;
            if (viewer == null)
            {
                Debug.LogWarning("The user guide needs a viewer camera.", this);
                return;
            }

            // The prefab starts inactive. Parenting before activation lets
            // GentleCanvasFollow resolve the viewer before detaching from the eye.
            instance = Instantiate(guidePrefab, viewer, false);
            instance.gameObject.SetActive(false);
        }

        // Reopening selects this scene's page and recenters the reading panel.
        if (instance.gameObject.activeSelf) instance.Close();
        instance.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (instance != null) Destroy(instance.gameObject);
    }
}
