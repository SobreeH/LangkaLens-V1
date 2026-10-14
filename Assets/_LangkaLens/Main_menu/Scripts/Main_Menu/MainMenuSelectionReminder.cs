using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>Periodically demonstrates menu selection without requiring any interaction.</summary>
[DisallowMultipleComponent]
public sealed class MainMenuSelectionReminder : MonoBehaviour
{
    [SerializeField] private Transform viewer;
    [SerializeField] private GameObject card;
    [SerializeField] private UserGuideLauncher guideLauncher;
    [SerializeField, Min(1f)] private float repeatInterval = 30f;
    [SerializeField, Min(0.1f)] private float visibleDuration = 10f;
    [SerializeField] private Vector3 viewerOffset = new Vector3(-1f, 0f, 2f);

    private bool hasShown;
    private bool wasGuideOpen;
    private float nextAppearance;
    private float hideAt;
    private Vector3 lastHeading = Vector3.forward;

    private void OnEnable()
    {
        hasShown = false;
        wasGuideOpen = false;
        nextAppearance = 0f;
        Hide();
    }

    private void LateUpdate()
    {
        if (card == null || viewer == null || !viewer.gameObject.activeInHierarchy)
        {
            Hide();
            return;
        }

        float now = Time.unscaledTime;
        bool guideOpen = guideLauncher != null && guideLauncher.IsGuideOpen;
        if (guideOpen)
        {
            wasGuideOpen = true;
            Hide();
            return;
        }
        if (wasGuideOpen)
        {
            wasGuideOpen = false;
            nextAppearance = now + repeatInterval;
        }

        // An incoming scene can still be covered by the persistent loading screen.
        // Wait for it before showing the first card; never show over a transition.
        if (SceneLoadingScreen.IsLoading)
        {
            Hide();
            if (hasShown) nextAppearance = now + repeatInterval;
            return;
        }
        if (!LocalizationSettings.InitializationOperation.IsDone ||
            LocalizationSettings.SelectedLocale == null) return;

        if (card.activeSelf && now >= hideAt) Hide();
        if (now < nextAppearance) return;

        Vector3 heading = Vector3.ProjectOnPlane(viewer.forward, Vector3.up);
        if (heading.sqrMagnitude > 0.001f) lastHeading = heading.normalized;
        Quaternion yaw = Quaternion.LookRotation(lastHeading, Vector3.up);
        Vector3 position = viewer.position + yaw * viewerOffset;
        // Canvas faces back along its forward axis. Do not follow the head while reading.
        card.transform.SetPositionAndRotation(position,
            Quaternion.LookRotation(position - viewer.position, Vector3.up));
        card.SetActive(true);
        hasShown = true;
        hideAt = now + visibleDuration;
        nextAppearance = now + repeatInterval;
    }

    private void OnDisable() => Hide();

    private void Hide()
    {
        if (card != null && card.activeSelf) card.SetActive(false);
    }

    private void OnValidate()
    {
        repeatInterval = Mathf.Max(1f, repeatInterval);
        visibleDuration = Mathf.Clamp(visibleDuration, 0.1f, repeatInterval);
    }
}
