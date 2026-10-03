using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Dynamically updates a RawImage texture based on the active Unity Localization locale (English / Thai).
/// Subscribes to LocalizationSettings.SelectedLocaleChanged to react dynamically at runtime.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RawImage))]
public class LocalizedRawImage : MonoBehaviour
{
    [Header("Target Component")]
    [SerializeField] private RawImage targetImage;

    [Header("Localized Textures")]
    [Tooltip("Texture to display when locale is English ('en') or default fallback")]
    [SerializeField] private Texture englishTexture;

    [Tooltip("Texture to display when locale is Thai ('th')")]
    [SerializeField] private Texture thaiTexture;

    public Texture EnglishTexture
    {
        get => englishTexture;
        set
        {
            englishTexture = value;
            UpdateTexture();
        }
    }

    public Texture ThaiTexture
    {
        get => thaiTexture;
        set
        {
            thaiTexture = value;
            UpdateTexture();
        }
    }

    public RawImage TargetImage
    {
        get => targetImage;
        set => targetImage = value;
    }

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }
    }

    private void OnEnable()
    {
        LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
        StartCoroutine(InitializeAndApplyRoutine());
    }

    private void OnDisable()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
    }

    private IEnumerator InitializeAndApplyRoutine()
    {
        if (!LocalizationSettings.InitializationOperation.IsDone)
        {
            yield return LocalizationSettings.InitializationOperation;
        }

        UpdateTexture(LocalizationSettings.SelectedLocale);
    }

    private void OnSelectedLocaleChanged(Locale locale)
    {
        UpdateTexture(locale);
    }

    /// <summary>
    /// Updates the RawImage texture based on the given or current locale.
    /// </summary>
    public void UpdateTexture(Locale locale = null)
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }

        if (targetImage == null) return;

        if (locale == null)
        {
            locale = LocalizationSettings.SelectedLocale;
        }

        string code = locale != null ? locale.Identifier.Code : "en";
        bool isThai = !string.IsNullOrEmpty(code) && code.StartsWith("th", System.StringComparison.OrdinalIgnoreCase);

        Texture chosenTexture = isThai ? thaiTexture : englishTexture;
        if (chosenTexture != null)
        {
            targetImage.texture = chosenTexture;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<RawImage>();
        }
    }

    [ContextMenu("Apply English Texture")]
    private void ContextApplyEnglish()
    {
        if (targetImage == null) targetImage = GetComponent<RawImage>();
        if (targetImage != null && englishTexture != null)
        {
            UnityEditor.Undo.RecordObject(targetImage, "Apply English Texture");
            targetImage.texture = englishTexture;
            UnityEditor.EditorUtility.SetDirty(targetImage);
        }
    }

    [ContextMenu("Apply Thai Texture")]
    private void ContextApplyThai()
    {
        if (targetImage == null) targetImage = GetComponent<RawImage>();
        if (targetImage != null && thaiTexture != null)
        {
            UnityEditor.Undo.RecordObject(targetImage, "Apply Thai Texture");
            targetImage.texture = thaiTexture;
            UnityEditor.EditorUtility.SetDirty(targetImage);
        }
    }
#endif
}
