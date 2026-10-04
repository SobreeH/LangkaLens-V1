using Oculus.Interaction;
using UnityEngine;

/// <summary>ISDK owns selection; this transformer keeps architectural models upright.</summary>
public class TourModelTransformer : MonoBehaviour, ITransformer
{
    private IGrabbable grabbable;
    private Pose previousGrab;
    private bool inspecting;
    private bool allowTranslation = true;
    private bool grabbed;

    public void Initialize(IGrabbable value) => grabbable = value;

    public void SetInspecting(bool value, bool allowTranslation = true)
    {
        inspecting = value;
        this.allowTranslation = allowTranslation;
        Rebase();
    }

    public void BeginTransform()
    {
        grabbed = true;
        Rebase();
    }

    public void Rebase()
    {
        if (grabbable != null && grabbable.GrabPoints.Count > 0)
            previousGrab = grabbable.GrabPoints[0];
    }

    public void UpdateTransform()
    {
        if (!grabbed || grabbable == null || grabbable.GrabPoints.Count == 0) return;
        Pose current = grabbable.GrabPoints[0];
        if (inspecting)
        {
            // Museum exhibits stay on their display while grip rotation remains available.
            if (allowTranslation)
                grabbable.Transform.position += current.position - previousGrab.position;
            Vector3 before = Vector3.ProjectOnPlane(previousGrab.rotation * Vector3.forward, Vector3.up);
            Vector3 after = Vector3.ProjectOnPlane(current.rotation * Vector3.forward, Vector3.up);
            // Ignore yaw when pointing almost vertically, where heading is ambiguous.
            if (before.sqrMagnitude > 0.04f && after.sqrMagnitude > 0.04f)
                Rotate(Vector3.SignedAngle(before, after, Vector3.up));
        }
        previousGrab = current;
    }

    public void Rotate(float degrees)
    {
        if (inspecting && grabbable != null)
            grabbable.Transform.rotation = Quaternion.AngleAxis(degrees, Vector3.up) * grabbable.Transform.rotation;
    }

    public void EndTransform() => grabbed = false;
}
