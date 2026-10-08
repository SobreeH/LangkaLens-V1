using UnityEngine;

/// <summary>Owns a reusable guide window, optionally replacing an existing menu.</summary>
[DisallowMultipleComponent]
public sealed class UserGuideLauncher : MonoBehaviour
{
    [SerializeField] private UserGuideCanvas guidePrefab;
    [Tooltip("Optional menu canvas to hide and replace while the guide is open.")]
    [SerializeField] private RectTransform replacementCanvas;
    private UserGuideCanvas instance;
    private bool menuHidden;

    public void OpenGuide()
    {
        if (guidePrefab == null)
        {
            Debug.LogError("The tablet user guide prefab is missing.", this);
            return;
        }

        if (replacementCanvas != null)
        {
            OpenInPlace();
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

    private void OpenInPlace()
    {
        if (instance == null)
        {
            // Keep the guide outside the menu hierarchy so hiding the menu cannot hide it.
            // The prefab is inactive, allowing follow to be disabled before Awake runs.
            instance = Instantiate(guidePrefab, replacementCanvas.parent, false);
            if (instance.TryGetComponent<GentleCanvasFollow>(out var follow)) follow.enabled = false;
            instance.Closed += RestoreMenu;
        }

        if (menuHidden) return;

        var guideRect = (RectTransform)instance.transform;
        float fit = Mathf.Min(replacementCanvas.rect.width / guideRect.rect.width,
            replacementCanvas.rect.height / guideRect.rect.height);
        guideRect.localScale = replacementCanvas.localScale * fit;
        guideRect.SetPositionAndRotation(replacementCanvas.TransformPoint(replacementCanvas.rect.center),
            replacementCanvas.rotation);

        menuHidden = true;
        replacementCanvas.gameObject.SetActive(false);
        instance.gameObject.SetActive(true);
    }

    private void RestoreMenu()
    {
        if (!menuHidden) return;
        menuHidden = false;
        if (replacementCanvas != null) replacementCanvas.gameObject.SetActive(true);
    }

    private void OnDestroy()
    {
        if (instance != null)
        {
            instance.Closed -= RestoreMenu;
            Destroy(instance.gameObject);
        }
    }
}
