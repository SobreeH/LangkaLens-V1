using UnityEngine;
using UnityEngine.XR;

/// <summary>Explains the headset's system recenter action without intercepting the Meta button.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(600)]
public class HeadsetRecenterGuidance : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private Transform head;
    [SerializeField] private Transform trackingSpace;
    [SerializeField] private GameObject reminderView;
    [SerializeField] private UnityEngine.UI.Image metaHighlight;

    [Header("Reminder")]
    [SerializeField] private bool remindersEnabled = true;
    [Tooltip("Horizontal distance from tracking-space zero. Height is intentionally ignored.")]
    [SerializeField, Min(0.01f)] private float originToleranceMetres = 0.25f;
    [SerializeField, Min(1f)] private float awaySecondsBeforeReminder = 180f;
    [SerializeField, Min(1f)] private float reminderDurationSeconds = 12f;
    [SerializeField, Min(0.5f)] private float reminderDistance = 1.4f;
    [SerializeField] private float reminderVerticalOffset = -0.18f;

    private OVRDisplay display;
    private float awaySeconds;
    private float reminderSeconds;
    private float vibrationStartedAt;
    private bool vibrating;
    private bool focused = true;
    private bool paused;
    private Color restingHighlight;
    private bool HasReminder => reminderView != null && reminderView.activeSelf;

    private void Awake()
    {
        if (metaHighlight != null) restingHighlight = metaHighlight.color;
        if (reminderView != null) reminderView.SetActive(false);
    }

    private void OnEnable()
    {
        focused = Application.isFocused;
        SubscribeToDisplay();
    }

    private void SubscribeToDisplay()
    {
        // OVRManager creates its display during initialization; retry until it is available.
        if (display == OVRManager.display) return;
        if (display != null) display.RecenteredPose -= OnRecentered;
        display = OVRManager.display;
        if (display != null) display.RecenteredPose += OnRecentered;
    }

    private void LateUpdate()
    {
        // The highlight remains visible in MR, but no automatic popup or haptics may run there.
        if (!remindersEnabled || gameObject.scene.name == "MR_Scene")
        {
            ResetReminder();
            return;
        }

        SubscribeToDisplay();
        if (head == null || trackingSpace == null || reminderView == null ||
            !focused || paused || !OVRManager.hasInputFocus || !HasTrackedHead())
        {
            ResetReminder();
            return;
        }

        // Locomotion moves the rig in the world. Only physical displacement inside tracking space
        // matters here, otherwise museum movement and tour zoom would cause false reminders.
        Vector3 position = trackingSpace.InverseTransformPoint(head.position);
        position.y = 0f;
        if (position.sqrMagnitude <= originToleranceMetres * originToleranceMetres)
        {
            ResetReminder();
            return;
        }

        awaySeconds += Time.unscaledDeltaTime;
        if (awaySeconds >= awaySecondsBeforeReminder && !HasReminder)
        {
            awaySeconds = 0f;
            reminderSeconds = 0f;
            PositionReminder();
            reminderView.SetActive(true);
            StartVibration();
        }

        if (HasReminder)
        {
            reminderSeconds += Time.unscaledDeltaTime;
            if (reminderSeconds >= reminderDurationSeconds)
                HideReminder();
            else
            {
                PositionReminder();
                if (metaHighlight != null)
                {
                    float pulse = 0.5f + 0.5f * Mathf.Sin(reminderSeconds * Mathf.PI * 2f);
                    metaHighlight.color = Color.Lerp(restingHighlight, Color.white, pulse * 0.65f);
                }
            }
        }

        UpdateVibration();
    }

    private static bool HasTrackedHead()
    {
        var device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        return device.isValid &&
               device.TryGetFeatureValue(CommonUsages.isTracked, out bool tracked) && tracked &&
               device.TryGetFeatureValue(CommonUsages.trackingState, out InputTrackingState state) &&
               (state & (InputTrackingState.Position | InputTrackingState.Rotation)) ==
               (InputTrackingState.Position | InputTrackingState.Rotation);
    }

    private void PositionReminder()
    {
        reminderView.transform.SetPositionAndRotation(
            head.position + head.forward * reminderDistance + head.up * reminderVerticalOffset,
            head.rotation);
    }

    private void StartVibration()
    {
        if (!OVRInput.IsControllerConnected(OVRInput.Controller.RTouch)) return;
        vibrating = true;
        vibrationStartedAt = Time.unscaledTime;
        OVRInput.SetControllerVibration(0.7f, 0.35f, OVRInput.Controller.RTouch);
    }

    private void UpdateVibration()
    {
        if (!vibrating) return;
        float elapsed = Time.unscaledTime - vibrationStartedAt;
        if (elapsed >= 0.5f || !OVRInput.IsControllerConnected(OVRInput.Controller.RTouch))
        {
            StopVibration();
            return;
        }
        bool pulse = elapsed < 0.15f || (elapsed >= 0.3f && elapsed < 0.45f);
        OVRInput.SetControllerVibration(pulse ? 0.7f : 0f, pulse ? 0.35f : 0f, OVRInput.Controller.RTouch);
    }

    private void StopVibration()
    {
        if (!vibrating) return;
        OVRInput.SetControllerVibration(0f, 0f, OVRInput.Controller.RTouch);
        vibrating = false;
    }

    private void HideReminder()
    {
        if (reminderView != null && reminderView.activeSelf) reminderView.SetActive(false);
        if (metaHighlight != null) metaHighlight.color = restingHighlight;
        reminderSeconds = 0f;
        StopVibration();
    }

    private void ResetReminder()
    {
        awaySeconds = 0f;
        HideReminder();
    }

    private void OnRecentered() => ResetReminder();

    private void OnApplicationFocus(bool hasFocus)
    {
        focused = hasFocus;
        if (!hasFocus) ResetReminder();
    }

    private void OnApplicationPause(bool isPaused)
    {
        paused = isPaused;
        if (isPaused) ResetReminder();
    }

    private void OnDisable()
    {
        if (display != null) display.RecenteredPose -= OnRecentered;
        display = null;
        ResetReminder();
    }
}
