using UnityEngine;
using TMPro;
using System.Collections;

public class VRZoomController : MonoBehaviour
{
    [Header("References")]
    public Transform vrRigParent;
    public Transform vrCamera;
    public TextMeshProUGUI zoomText;
    public TourManager tourManager;

    [Header("Settings")]
    [Tooltip("How fast the camera zooms in/out.")]
    public float zoomSpeed = 2f;
    [Tooltip("Maximum distance the camera can move forward.")]
    public float maxZoom = 5f;

    private Vector3 currentZoomOffset = Vector3.zero;
    private Coroutine fadeCoroutine;

    void Start()
    {
        if (zoomText != null)
        {
            zoomText.color = new Color(zoomText.color.r, zoomText.color.g, zoomText.color.b, 0f);
            zoomText.text = "";
        }
    }

    void Update()
    {
        if (tourManager != null && (tourManager.IsNavigationBlocked ||
            (tourManager.reconstructionPresenter != null && !tourManager.reconstructionPresenter.CanReturnToTourMenu))) return;
        // OVRInput.Button.Two = B button on Right controller
        // OVRInput.Button.One = A button on Right controller
        bool zoomInPressed = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch); 
        bool zoomOutPressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch); 

        if (zoomInPressed || zoomOutPressed)
        {
            Vector3 direction = vrCamera.forward;
            direction.y = 0; // Keep movement on the horizontal plane
            if (direction.sqrMagnitude > 0.001f) direction.Normalize();
            else direction = Vector3.forward;
            
            // If pulling back (zooming out), pull back directly along the current offset 
            // to ensure we always return precisely to the origin.
            if (zoomOutPressed && currentZoomOffset.sqrMagnitude > 0.01f)
            {
                direction = currentZoomOffset.normalized;
            }

            float step = zoomSpeed * Time.deltaTime;
            if (zoomOutPressed) step = -step;

            Vector3 delta = direction * step;
            Vector3 newOffset = currentZoomOffset + delta;

            // Clamp max zoom
            if (newOffset.magnitude > maxZoom)
            {
                newOffset = newOffset.normalized * maxZoom;
            }

            // Clamp min zoom (prevent going past origin when zooming out)
            // If the dot product is negative or 0, we've crossed the origin
            if (zoomOutPressed && Vector3.Dot(newOffset, currentZoomOffset) <= 0)
            {
                newOffset = Vector3.zero;
            }

            // Apply displacement to Rig
            Vector3 actualDelta = newOffset - currentZoomOffset;
            vrRigParent.position += actualDelta;
            currentZoomOffset = newOffset;

            UpdateUI();
        }
    }

    private void UpdateUI()
    {
        if (zoomText == null) return;

        float percentage = 100f + ((currentZoomOffset.magnitude / maxZoom) * 100f);
        zoomText.text = Mathf.RoundToInt(percentage).ToString() + "%";

        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTextRoutine());
    }

    private IEnumerator FadeTextRoutine()
    {
        // Show immediately
        zoomText.color = new Color(zoomText.color.r, zoomText.color.g, zoomText.color.b, 1f);
        
        yield return new WaitForSeconds(0.5f); // Stay visible briefly
        
        // Fade out
        float t = 0;
        while (t < 1f)
        {
            t += Time.deltaTime;
            zoomText.color = new Color(zoomText.color.r, zoomText.color.g, zoomText.color.b, 1f - t);
            yield return null;
        }
    }
}
