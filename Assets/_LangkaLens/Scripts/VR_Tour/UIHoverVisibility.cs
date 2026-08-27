using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class UIHoverVisibility : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private CanvasGroup canvasGroup;
    
    [Tooltip("How fast the UI fades in/out")]
    public float fadeDuration = 0.2f;
    
    [Tooltip("The alpha (transparency) when not hovered. Set to 0 to completely hide it.")]
    public float idleAlpha = 0f;
    
    [Tooltip("The alpha (transparency) when hovered. Set to 1 for fully visible.")]
    public float hoverAlpha = 1f;

    private Coroutine fadeCoroutine;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        // Ensure it starts at the idle state
        canvasGroup.alpha = idleAlpha;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(hoverAlpha));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeTo(idleAlpha));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = canvasGroup.alpha;
        float time = 0;
        
        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            yield return null;
        }
        
        canvasGroup.alpha = targetAlpha;
    }
}
