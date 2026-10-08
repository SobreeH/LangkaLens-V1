using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Shared, temporary loading UI for scene and panorama transitions.</summary>
public sealed class SceneLoadingScreen : MonoBehaviour
{
    private static SceneLoadingScreen instance;
    public static bool IsLoading => instance != null;
    private Canvas canvas;
    private CanvasGroup group;
    private RectTransform fill;
    private TextMeshProUGUI label;
    private Camera view;
    private Material panelMaterial;
    private Material textMaterial;
    private bool thai;
    private int renderedFrames;
    private int lastRenderedFrame = -1;
    private MonoBehaviour contentOwner;
    private bool contentLoading;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => instance = null;

    public static void LoadScene(string sceneName)
    {
        if (IsLoading) return;
        if (string.IsNullOrWhiteSpace(sceneName) || !Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogError($"[SceneLoadingScreen] Scene is not enabled in Build Settings: {sceneName}");
            return;
        }

        CreateScreen();
        instance.StartCoroutine(instance.LoadRoutine(sceneName));
    }

    private static SceneLoadingScreen CreateScreen()
    {
        var root = new GameObject("Scene Loading Screen");
        DontDestroyOnLoad(root);
        instance = root.AddComponent<SceneLoadingScreen>();
        instance.BuildUI();
        return instance;
    }

    public static SceneLoadingScreen BeginContentLoading(MonoBehaviour owner)
    {
        if (IsLoading || owner == null || !owner.isActiveAndEnabled) return null;
        var screen = CreateScreen();
        screen.contentOwner = owner;
        screen.contentLoading = true;
        return screen;
    }

    public void ReportContentProgress(float progress) => SetProgress(Mathf.Clamp01(progress) * 0.9f);

    public void CancelContentLoading()
    {
        if (!contentLoading) return;
        gameObject.SetActive(false);
        if (instance == this) instance = null;
        Destroy(gameObject);
    }

    private void OnEnable()
    {
        Application.onBeforeRender += FollowCamera;
        RenderPipelineManager.endCameraRendering += OnCameraRendered;
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= FollowCamera;
        RenderPipelineManager.endCameraRendering -= OnCameraRendered;
    }

    private void LateUpdate()
    {
        if (contentLoading && (contentOwner == null || !contentOwner.isActiveAndEnabled))
        {
            CancelContentLoading();
            return;
        }
        FollowCamera();
    }

    private void FollowCamera()
    {
        if (canvas == null) return;
        if (view == null || !view.isActiveAndEnabled) view = Camera.main;
        if (view == null) return;
        canvas.worldCamera = view;
        canvas.transform.SetPositionAndRotation(view.transform.position + view.transform.forward * 1.5f,
            view.transform.rotation);
    }

    private void OnCameraRendered(ScriptableRenderContext context, Camera camera)
    {
        if (camera != view || lastRenderedFrame == Time.frameCount) return;
        lastRenderedFrame = Time.frameCount;
        renderedFrames++;
    }

    private IEnumerator LoadRoutine(string sceneName)
    {
        // Submit the loading UI before scene deserialization can stall the main thread.
        yield return WaitForRenderedFrames();
        AsyncOperation operation;
        try
        {
            operation = SceneManager.LoadSceneAsync(sceneName);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Destroy(gameObject);
            yield break;
        }
        if (operation == null)
        {
            Destroy(gameObject);
            yield break;
        }
        operation.allowSceneActivation = false;
        while (operation.progress < 0.9f)
        {
            SetProgress(operation.progress / 0.9f * 0.9f);
            yield return null;
        }
        SetProgress(0.9f);
        label.text = thai ? "กำลังเตรียมฉาก…" : "Preparing scene…";
        operation.allowSceneActivation = true;
        while (!operation.isDone) yield return null;

        // Start() may launch additional asset requests after scene activation.
        yield return null;
        view = null;
        var tour = FindFirstObjectByType<TourManager>();
        float deadline = Time.realtimeSinceStartup + 30f;
        while (tour != null && !tour.IsInitialContentReady && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (tour != null && !tour.IsInitialContentReady)
            Debug.LogWarning("[SceneLoadingScreen] Timed out waiting for the tour's initial panorama.");

        yield return FinishLoading();
    }

    public IEnumerator FinishLoading()
    {
        label.text = thai ? "กำลังเตรียมฉาก…" : "Preparing scene…";
        SetProgress(1f);
        yield return WaitForRenderedFrames();
        // Use real time so paused scenes cannot leave the cover stuck on screen.
        for (float elapsed = 0f; elapsed < 0.25f; elapsed += Time.unscaledDeltaTime)
        {
            group.alpha = 1f - elapsed / 0.25f;
            yield return null;
        }
        Destroy(gameObject);
    }

    public IEnumerator WaitForRenderedFrames()
    {
        int target = renderedFrames + 2;
        float deadline = Time.realtimeSinceStartup + 2f;
        // The timeout also permits headless/hidden Game View validation to complete.
        while (renderedFrames < target && Time.realtimeSinceStartup < deadline) yield return null;
    }

    private void SetProgress(float value)
    {
        fill.anchorMax = new Vector2(Mathf.Clamp01(value), 1f);
    }

    private void BuildUI()
    {
        thai = LocalizationSettings.SelectedLocale != null &&
            LocalizationSettings.SelectedLocale.Identifier.Code.StartsWith("th", StringComparison.OrdinalIgnoreCase);
        var panel = new GameObject("Loading Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        panel.transform.SetParent(transform, false);
        canvas = panel.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingOrder = 32767;
        group = panel.GetComponent<CanvasGroup>();
        var rect = (RectTransform)panel.transform;
        rect.sizeDelta = new Vector2(12000f, 12000f);
        rect.localScale = Vector3.one * 0.001f;
        panelMaterial = new Material(Resources.Load<Shader>("SceneLoadingOverlay"));
        CreateImage("Background", rect, new Vector2(12000f, 12000f), Vector2.zero,
            new Color(0.025f, 0.04f, 0.065f, 1f));

        var textObject = new GameObject("Loading Label", typeof(RectTransform));
        textObject.transform.SetParent(rect, false);
        label = textObject.AddComponent<TextMeshProUGUI>();
        label.font = Resources.Load<TMP_FontAsset>("Fonts & Materials/Sarabun-Medium SDF");
        label.fontSize = 48f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.text = thai ? "กำลังโหลด…" : "Loading…";
        label.rectTransform.sizeDelta = new Vector2(1000f, 150f);
        label.rectTransform.anchoredPosition = new Vector2(0f, 60f);
        textMaterial = new Material(label.fontSharedMaterial);
        textMaterial.renderQueue = panelMaterial.renderQueue;
        textMaterial.SetFloat("unity_GUIZTestMode", (float)CompareFunction.Always);
        label.fontSharedMaterial = textMaterial;

        var track = CreateImage("Progress Track", rect, new Vector2(650f, 12f), new Vector2(0f, -60f),
            new Color(0.15f, 0.19f, 0.23f));
        fill = CreateImage("Progress", track, Vector2.zero, Vector2.zero, new Color(0.35f, 0.85f, 0.75f));
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = new Vector2(0f, 1f);
        fill.sizeDelta = Vector2.zero;
        FollowCamera();
    }

    private RectTransform CreateImage(string objectName, Transform parent, Vector2 size, Vector2 position, Color color)
    {
        var go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var image = go.GetComponent<Image>();
        image.color = color;
        image.material = panelMaterial;
        image.raycastTarget = false;
        image.rectTransform.sizeDelta = size;
        image.rectTransform.anchoredPosition = position;
        return image.rectTransform;
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
        if (panelMaterial != null) Destroy(panelMaterial);
        if (textMaterial != null) Destroy(textMaterial);
    }
}
