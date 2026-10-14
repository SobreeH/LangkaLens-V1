using UnityEngine;
using UnityEngine.UI;

/// <summary>Restores scrolling after inspection, including content resized by localization.</summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1100)]
public sealed class CanvasContentReset : MonoBehaviour
{
    private ScrollRect[] scrollRects;
    private Vector2[] initialContentPositions;
    private bool[] verticalSupported;
    private bool[] horizontalSupported;
    private bool resetPending = true;

    private void Awake() => CaptureInitialPositions();

    public void CaptureInitialPositions()
    {
        if (scrollRects != null) return;
        scrollRects = GetComponentsInChildren<ScrollRect>(true);
        initialContentPositions = new Vector2[scrollRects.Length];
        verticalSupported = new bool[scrollRects.Length];
        horizontalSupported = new bool[scrollRects.Length];
        for (int i = 0; i < scrollRects.Length; i++)
        {
            if (scrollRects[i] == null) continue;
            scrollRects[i].movementType = ScrollRect.MovementType.Clamped;
            verticalSupported[i] = scrollRects[i].vertical;
            horizontalSupported[i] = scrollRects[i].horizontal;
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
            scroll.movementType = ScrollRect.MovementType.Clamped;
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
            {
                scroll.movementType = ScrollRect.MovementType.Clamped;
                LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            }
        }
        Canvas.ForceUpdateCanvases();

        for (int i = 0; i < scrollRects.Length; i++)
        {
            var scroll = scrollRects[i];
            if (scroll == null || scroll.content == null) continue;
            scroll.StopMovement();
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.content.anchoredPosition = initialContentPositions[i];
            if (!scroll.isActiveAndEnabled) continue;

            var textComp = scroll.content.GetComponentInChildren<TMPro.TMP_Text>(true);
            bool hasMeaningfulText = textComp == null || !string.IsNullOrWhiteSpace(textComp.text);

            if (verticalSupported != null && i < verticalSupported.Length && verticalSupported[i])
            {
                float viewportHeight = scroll.viewport != null
                    ? scroll.viewport.rect.height
                    : ((RectTransform)scroll.transform).rect.height;
                float contentHeight = scroll.content.rect.height;

                bool canScrollVertical = hasMeaningfulText && (contentHeight > viewportHeight + 1f);
                scroll.vertical = canScrollVertical;

                if (scroll.verticalScrollbar != null)
                    scroll.verticalScrollbar.interactable = canScrollVertical;

                if (scroll.vertical) scroll.verticalNormalizedPosition = 1f;
            }

            if (horizontalSupported != null && i < horizontalSupported.Length && horizontalSupported[i])
            {
                float viewportWidth = scroll.viewport != null
                    ? scroll.viewport.rect.width
                    : ((RectTransform)scroll.transform).rect.width;
                float contentWidth = scroll.content.rect.width;

                bool canScrollHorizontal = hasMeaningfulText && (contentWidth > viewportWidth + 1f);
                scroll.horizontal = canScrollHorizontal;

                if (scroll.horizontalScrollbar != null)
                    scroll.horizontalScrollbar.interactable = canScrollHorizontal;

                if (scroll.horizontal) scroll.horizontalNormalizedPosition = 0f;
            }
        }
    }
}
