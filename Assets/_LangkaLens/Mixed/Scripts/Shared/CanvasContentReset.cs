using UnityEngine;
using UnityEngine.UI;

/// <summary>Restores scrolling after inspection, including content resized by localization.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1100)]
public sealed class CanvasContentReset : MonoBehaviour
{
    private ScrollRect[] scrollRects;
    private Vector2[] initialContentPositions;
    private bool resetPending = true;

    private void Awake() => CaptureInitialPositions();

    public void CaptureInitialPositions()
    {
        if (scrollRects != null) return;
        scrollRects = GetComponentsInChildren<ScrollRect>(true);
        initialContentPositions = new Vector2[scrollRects.Length];
        for (int i = 0; i < scrollRects.Length; i++)
        {
            if (scrollRects[i].content != null)
                initialContentPositions[i] = scrollRects[i].content.anchoredPosition;
        }
    }

    public void ResetContents()
    {
        CaptureInitialPositions();
        resetPending = true;
        // Hidden canvases must also forget their old scroll offset and inertia.
        for (int i = 0; i < scrollRects.Length; i++)
        {
            var scroll = scrollRects[i];
            if (scroll == null || scroll.content == null) continue;
            scroll.StopMovement();
            scroll.content.anchoredPosition = initialContentPositions[i];
        }
        if (isActiveAndEnabled) ApplyReset();
    }

    private void OnEnable()
    {
        if (resetPending) ResetContents();
    }

    private void LateUpdate()
    {
        if (!resetPending) return;
        // Run after ScrollRect and chat updates so new text/layout cannot undo the reset.
        ApplyReset();
        resetPending = false;
    }

    private void ApplyReset()
    {
        Canvas.ForceUpdateCanvases();
        foreach (var scroll in scrollRects)
        {
            if (scroll != null && scroll.isActiveAndEnabled && scroll.content != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
        }
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < scrollRects.Length; i++)
        {
            var scroll = scrollRects[i];
            if (scroll == null || scroll.content == null) continue;
            scroll.StopMovement();
            scroll.content.anchoredPosition = initialContentPositions[i];
            if (!scroll.isActiveAndEnabled) continue;
            // The content's height can differ for each artifact and language.
            if (scroll.horizontal) scroll.horizontalNormalizedPosition = 0f;
            if (scroll.vertical) scroll.verticalNormalizedPosition = 1f;
        }
    }
}
