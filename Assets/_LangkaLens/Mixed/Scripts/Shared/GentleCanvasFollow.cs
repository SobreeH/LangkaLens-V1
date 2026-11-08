using UnityEngine;

/// <summary>Keeps a reading panel nearby without locking it to every headset movement.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public sealed class GentleCanvasFollow : MonoBehaviour
{
    [Tooltip("Viewer to follow. When unset, use the camera above this canvas in the hierarchy.")]
    [SerializeField] private Transform head;
    [Tooltip("Metres to the right, above and ahead of the viewer, relative to their horizontal heading.")]
    [SerializeField] private Vector3 viewerOffset = new Vector3(0f, 0f, 3f);
    [Tooltip("Additional offset applied while the child virtual keyboard is open.")]
    [SerializeField] private Vector3 keyboardOpenOffset = new Vector3(0f, 0.85f, 0f);
    [Tooltip("Minimum clearance in metres kept between the bottom of the open keyboard and the rig floor.")]
    [SerializeField, Min(0f)] private float minFloorClearance = 0.1f;
    [Tooltip("Small head turns leave the panel stationary so the user can scan its contents.")]
    [SerializeField, Range(1f, 45f)] private float angularDeadZone = 12f;
    [Tooltip("Small physical head movements leave the panel stationary.")]
    [SerializeField, Min(0.01f)] private float positionDeadZone = 0.1f;
    [Tooltip("Time in seconds used to ease toward a new viewing position.")]
    [SerializeField, Min(0.05f)] private float smoothTime = 0.35f;

    private Transform followSpace;
    private BilingualVirtualKeyboard childKeyboard;
    private Vector3 currentExtraOffset;
    private Vector3 extraOffsetVelocity;
    private Vector3 anchorHeadPosition;
    private Vector3 smoothedHeadPosition;
    private Quaternion targetRotation = Quaternion.identity;
    private Vector3 velocity;
    private bool initialized;

    private void Awake()
    {
        // A fixed menu replacement disables following before its first activation.
        if (!enabled) return;

        if (head == null)
        {
            Camera parentCamera = GetComponentInParent<Camera>(true);
            if (parentCamera != null) head = parentCamera.transform;
        }

        if (head == null || head == transform || head.IsChildOf(transform))
        {
            Debug.LogError("Gentle canvas follow needs a viewer camera above the canvas, or an assigned head reference.", this);
            enabled = false;
            return;
        }

        childKeyboard = GetComponentInChildren<BilingualVirtualKeyboard>(true);

        // Leave the eye anchor before rendering: otherwise late XR pose updates still head-lock the UI.
        // Keeping the tracking-space parent also lets locomotion/recentering move the panel with the rig.
        if (transform.IsChildOf(head)) transform.SetParent(head.parent, true);
        followSpace = transform.parent;
        initialized = true;
    }

    private void OnEnable()
    {
        if (!initialized || head == null) return;
        SetTarget(GetHeading());
        smoothedHeadPosition = anchorHeadPosition;
        currentExtraOffset = GetTargetExtraOffset();
        extraOffsetVelocity = Vector3.zero;
        ApplyPose(targetRotation);
        velocity = Vector3.zero;
    }

    private void LateUpdate() => UpdateFollow(Time.unscaledDeltaTime);

    private void UpdateFollow(float deltaTime)
    {
        if (!initialized || head == null || deltaTime <= 0f) return;

        Quaternion heading = GetHeading();
        Quaternion worldTargetRotation = followSpace != null
            ? followSpace.rotation * targetRotation : targetRotation;
        Vector3 localHeadPosition = ToLocalPosition(head.position);
        if (Quaternion.Angle(worldTargetRotation, heading) > angularDeadZone ||
            (localHeadPosition - anchorHeadPosition).sqrMagnitude > positionDeadZone * positionDeadZone)
        {
            SetTarget(heading);
        }

        smoothedHeadPosition = Vector3.SmoothDamp(smoothedHeadPosition, anchorHeadPosition,
            ref velocity, smoothTime, Mathf.Infinity, deltaTime);
        currentExtraOffset = Vector3.SmoothDamp(currentExtraOffset, GetTargetExtraOffset(),
            ref extraOffsetVelocity, smoothTime, Mathf.Infinity, deltaTime);
        float blend = 1f - Mathf.Exp(-deltaTime / smoothTime);
        Quaternion rotation = Quaternion.Slerp(transform.localRotation, targetRotation, blend);
        ApplyPose(rotation);
    }

    private Vector3 GetTargetExtraOffset()
        => childKeyboard != null && childKeyboard.IsOpen ? keyboardOpenOffset : Vector3.zero;

    private Quaternion GetHeading()
    {
        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
        {
            // Looking straight up/down has no reliable yaw. Retain the panel's last heading.
            Quaternion previous = followSpace != null
                ? followSpace.rotation * targetRotation : targetRotation;
            forward = Vector3.ProjectOnPlane(previous * Vector3.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        }
        return Quaternion.LookRotation(forward, Vector3.up);
    }

    private void SetTarget(Quaternion heading)
    {
        anchorHeadPosition = ToLocalPosition(head.position);
        targetRotation = followSpace != null
            ? Quaternion.Inverse(followSpace.rotation) * heading : heading;
    }

    private void ApplyPose(Quaternion localRotation)
    {
        Quaternion rotation = followSpace != null ? followSpace.rotation * localRotation : localRotation;
        Vector3 origin = followSpace != null
            ? followSpace.TransformPoint(smoothedHeadPosition) : smoothedHeadPosition;
        // Ease around the viewer on an arc, rather than cutting through them during a large turn.
        Vector3 position = origin + rotation * (viewerOffset + currentExtraOffset);
        if (childKeyboard != null && childKeyboard.IsOpen && followSpace != null)
        {
            var kbRect = (RectTransform)childKeyboard.transform;
            float keyboardBottomFromCanvas = (kbRect.localPosition.y + kbRect.rect.yMin) * transform.lossyScale.y;
            float minCanvasY = followSpace.position.y + minFloorClearance - keyboardBottomFromCanvas;
            if (position.y < minCanvasY) position.y = minCanvasY;
        }
        transform.SetPositionAndRotation(position, rotation);
    }

    private Vector3 ToLocalPosition(Vector3 worldPosition)
        => followSpace != null ? followSpace.InverseTransformPoint(worldPosition) : worldPosition;
}
