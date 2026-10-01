using System.Collections;
using UnityEngine;

// Positions the menu when opened, then leaves it stationary as the user looks around.
public class WorldSpaceMenuPlacement : MonoBehaviour
{
    [SerializeField] private Transform head;
    [SerializeField] private Transform menuRoot;
    [SerializeField] private TourManager tourManager;
    [SerializeField, Min(0.5f)] private float distance = 2f;

    private Coroutine pendingPlacement;

    private void OnEnable()
    {
        Recenter();
        pendingPlacement = StartCoroutine(PlaceAfterTrackingAndTransition());
    }

    private IEnumerator PlaceAfterTrackingAndTransition()
    {
        // Allow the initial tracked pose to arrive and any tour rig movement to settle.
        yield return null;
        while (tourManager != null && tourManager.IsTransitioning)
            yield return null;

        Recenter();
        pendingPlacement = null;
    }

    private void OnDisable()
    {
        if (pendingPlacement != null)
        {
            StopCoroutine(pendingPlacement);
            pendingPlacement = null;
        }
    }

    [ContextMenu("Recenter Menu")]
    public void Recenter()
    {
        if (head == null || menuRoot == null) return;

        Vector3 forward = Vector3.ProjectOnPlane(head.forward, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.Cross(head.right, Vector3.up);
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector3.forward;
        forward.Normalize();

        menuRoot.SetPositionAndRotation(
            head.position + forward * distance,
            Quaternion.LookRotation(forward, Vector3.up));
    }
}
