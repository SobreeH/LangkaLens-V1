using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles fake teleportation navigation in 360 photosphere tours using the VR controller thumbstick.
/// Pushing the left thumbstick forward projects a curved parabolic trajectory.
/// If the arc passes within snapRadius of a NavigationArrow, it snaps to it and highlights the arrow.
/// Releasing the thumbstick triggers the teleport transition to the destination node.
/// </summary>
public class VRTeleportNavigator : MonoBehaviour
{
    [Header("Core References")]
    [Tooltip("Transform from which the teleport arc emanates (defaults to LeftControllerAnchor)")]
    public Transform originTransform;

    [Tooltip("Reference to the TourManager (defaults to finding TourManager in scene)")]
    public TourManager tourManager;

    [Tooltip("LineRenderer used to visualize the curved arc")]
    public LineRenderer lineRenderer;

    [Header("Input Settings")]
    [Tooltip("Controller to read thumbstick from")]
    public OVRInput.Controller controller = OVRInput.Controller.LTouch;

    [Tooltip("Thumbstick axis to monitor")]
    public OVRInput.RawAxis2D thumbstickAxis = OVRInput.RawAxis2D.LThumbstick;

    [Tooltip("Forward stick tilt required to begin aiming")]
    [Range(0.1f, 0.9f)]
    public float activationThreshold = 0.45f;

    [Tooltip("Stick return threshold to trigger teleport on release")]
    [Range(0.05f, 0.5f)]
    public float releaseThreshold = 0.2f;

    [Tooltip("Pulling stick backwards past this value cancels aiming")]
    [Range(-0.9f, -0.1f)]
    public float cancelThreshold = -0.35f;

    [Header("Arc Physics Settings")]
    [Tooltip("Number of segments in the curved arc line")]
    [Range(10, 60)]
    public int arcResolution = 30;

    [Tooltip("Launch speed of the simulated arc")]
    public float arcVelocity = 8.5f;

    [Tooltip("Upward tilt bias in degrees added to controller orientation")]
    public float upwardAngleBias = 12.0f;

    [Tooltip("Downward gravity acceleration for the curve")]
    public float simulatedGravity = 9.8f;

    [Tooltip("Simulation time step between arc sample points")]
    public float timeStep = 0.05f;

    [Header("Snapping Settings")]
    [Tooltip("Maximum distance from arc to arrow to trigger snapping")]
    public float snapRadius = 2.0f;

    [Header("Visual Colors")]
    public Color searchColorStart = new Color(0.3f, 0.7f, 1f, 0.6f);
    public Color searchColorEnd = new Color(0.1f, 0.5f, 1f, 0.9f);
    public Color snapColorStart = new Color(0.2f, 1f, 0.5f, 0.85f);
    public Color snapColorEnd = new Color(0.1f, 1f, 0.3f, 1f);
    public float lineWidth = 0.025f;

    // Runtime state
    private bool isAiming = false;
    private NavigationArrow currentSnappedArrow = null;
    private Vector3[] arcPoints;
    private Coroutine hapticCoroutine;

    private void Awake()
    {
        EnsureArcPointsAllocated();
    }

    public void EnsureArcPointsAllocated()
    {
        if (arcPoints == null || arcPoints.Length != arcResolution)
        {
            arcPoints = new Vector3[arcResolution];
        }
    }

    private void Start()
    {
        EnsureArcPointsAllocated();

        if (tourManager == null)
        {
            tourManager = Object.FindFirstObjectByType<TourManager>();
        }

        if (originTransform == null)
        {
            GameObject leftAnchor = GameObject.Find("LeftControllerAnchor");
            if (leftAnchor != null) originTransform = leftAnchor.transform;
            else originTransform = transform;
        }

        SetupLineRenderer();
    }

    private void SetupLineRenderer()
    {
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                GameObject lineObj = new GameObject("TeleportArcLine");
                lineObj.transform.SetParent(transform, false);
                lineRenderer = lineObj.AddComponent<LineRenderer>();
            }
        }

        lineRenderer.positionCount = arcResolution;
        lineRenderer.useWorldSpace = true;
        lineRenderer.startWidth = lineWidth;
        lineRenderer.endWidth = lineWidth * 0.75f;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Ensure a visible unlit material
        if (lineRenderer.sharedMaterial == null)
        {
            Shader unlitShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlitShader == null) unlitShader = Shader.Find("Sprites/Default");
            
            Material mat = new Material(unlitShader);
            mat.color = Color.white;
            lineRenderer.material = mat;
        }

        lineRenderer.enabled = false;
    }

    private void Update()
    {
        HandleInput();
    }

    private void HandleInput()
    {
        // Don't allow new aiming or teleporting during an active scene/tour transition
        if (tourManager != null && tourManager.IsTransitioning)
        {
            if (isAiming) CancelAiming();
            return;
        }

        Vector2 stick = OVRInput.Get(thumbstickAxis, controller);

        #if UNITY_EDITOR
        // Fallback for testing in Unity Editor with keyboard/mouse
        if (stick.magnitude < 0.05f)
        {
            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
                stick = new Vector2(0f, 1f);
        }
        #endif

        if (!isAiming)
        {
            // Start aiming when thumbstick is pushed forward past activationThreshold
            if (stick.y >= activationThreshold)
            {
                isAiming = true;
                if (lineRenderer != null) lineRenderer.enabled = true;
            }
        }
        else
        {
            // Check for cancel (pulling thumbstick backward)
            if (stick.y <= cancelThreshold)
            {
                CancelAiming();
                return;
            }

            // Check for release (stick returned near center)
            if (stick.magnitude <= releaseThreshold)
            {
                ExecuteTeleportOrCancel();
                return;
            }

            // Continue aiming: simulate trajectory and check for arrow snaps
            UpdateArcTrajectory();
        }
    }

    public void UpdateArcTrajectory()
    {
        EnsureArcPointsAllocated();
        if (originTransform == null) return;

        // Calculate initial launch direction with upward bias
        Vector3 origin = originTransform.position;
        Vector3 forward = originTransform.forward;
        Vector3 right = originTransform.right;

        Quaternion biasRotation = Quaternion.AngleAxis(-upwardAngleBias, right);
        Vector3 launchVelocity = (biasRotation * forward) * arcVelocity;
        Vector3 gravityVector = Vector3.down * simulatedGravity;

        // Generate ballistic trajectory points
        for (int i = 0; i < arcResolution; i++)
        {
            float t = i * timeStep;
            arcPoints[i] = origin + (launchVelocity * t) + (0.5f * gravityVector * t * t);
        }

        // Check for snapping against active navigation arrows
        NavigationArrow closestArrow = FindClosestSnapArrow(arcPoints, out int closestArcIndex);

        if (closestArrow != null)
        {
            // Arrow is within snap radius
            if (currentSnappedArrow != closestArrow)
            {
                if (currentSnappedArrow != null) currentSnappedArrow.OnUnhover();
                currentSnappedArrow = closestArrow;
                currentSnappedArrow.OnHover();
                TriggerHaptic(0.2f, 0.07f);
            }

            // Bend the arc points from the closest segment to land cleanly on the arrow
            Vector3 targetPos = closestArrow.transform.position;
            int blendStartIndex = Mathf.Max(1, closestArcIndex);
            for (int i = blendStartIndex; i < arcResolution; i++)
            {
                float blendFactor = (float)(i - blendStartIndex) / (arcResolution - 1 - blendStartIndex + 0.001f);
                arcPoints[i] = Vector3.Lerp(arcPoints[i], targetPos, blendFactor);
            }
            arcPoints[arcResolution - 1] = targetPos;

            SetLineGradient(snapColorStart, snapColorEnd);
        }
        else
        {
            // No arrow within snap radius
            if (currentSnappedArrow != null)
            {
                currentSnappedArrow.OnUnhover();
                currentSnappedArrow = null;
            }

            SetLineGradient(searchColorStart, searchColorEnd);
        }

        // Apply points to line renderer
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = arcResolution;
            lineRenderer.SetPositions(arcPoints);
        }
    }

    private NavigationArrow FindClosestSnapArrow(Vector3[] points, out int closestPointIndex)
    {
        closestPointIndex = -1;
        if (tourManager == null || tourManager.ActiveArrows == null || tourManager.ActiveArrows.Count == 0)
            return null;

        NavigationArrow bestArrow = null;
        float bestDistance = snapRadius;

        foreach (GameObject arrowObj in tourManager.ActiveArrows)
        {
            if (arrowObj == null || !arrowObj.activeInHierarchy) continue;

            NavigationArrow arrow = arrowObj.GetComponent<NavigationArrow>();
            if (arrow == null) continue;

            Vector3 arrowPos = arrowObj.transform.position;

            // Find closest arc point to this arrow
            for (int i = 0; i < points.Length; i++)
            {
                float dist = Vector3.Distance(points[i], arrowPos);
                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    bestArrow = arrow;
                    closestPointIndex = i;
                }
            }
        }

        return bestArrow;
    }

    private void ExecuteTeleportOrCancel()
    {
        if (currentSnappedArrow != null)
        {
            // Teleport action confirmed!
            TriggerHaptic(0.45f, 0.12f);
            NavigationArrow target = currentSnappedArrow;
            
            // Clean up hover state and aiming arc
            target.OnUnhover();
            currentSnappedArrow = null;
            isAiming = false;
            if (lineRenderer != null) lineRenderer.enabled = false;

            // Trigger the transition
            target.OnArrowSelected();
        }
        else
        {
            // Released without valid snap: cancel cleanly
            CancelAiming();
        }
    }

    private void CancelAiming()
    {
        isAiming = false;
        if (currentSnappedArrow != null)
        {
            currentSnappedArrow.OnUnhover();
            currentSnappedArrow = null;
        }

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
        }
    }

    private void SetLineGradient(Color start, Color end)
    {
        if (lineRenderer == null) return;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(start, 0.0f), new GradientColorKey(end, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(start.a, 0.0f), new GradientAlphaKey(end.a, 1.0f) }
        );
        lineRenderer.colorGradient = gradient;
    }

    private void TriggerHaptic(float amplitude, float duration)
    {
        if (hapticCoroutine != null) StopCoroutine(hapticCoroutine);
        hapticCoroutine = StartCoroutine(HapticRoutine(amplitude, duration));
    }

    private IEnumerator HapticRoutine(float amplitude, float duration)
    {
        OVRInput.SetControllerVibration(amplitude, amplitude, controller);
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0f, 0f, controller);
        hapticCoroutine = null;
    }

    private void OnDisable()
    {
        CancelAiming();
        OVRInput.SetControllerVibration(0f, 0f, controller);
    }
}
