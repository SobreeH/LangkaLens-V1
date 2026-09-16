using UnityEngine;
using UnityEngine.Localization.Settings;
using System.Collections;

public class LanguageToggler : MonoBehaviour
{
    public void ToggleLanguage()
    {
        StartCoroutine(ToggleLanguageRoutine());
    }

    private IEnumerator ToggleLanguageRoutine()
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        if (locales == null || locales.Count == 0) yield break;

        var currentLocale = LocalizationSettings.SelectedLocale;

        int currentIndex = locales.IndexOf(currentLocale);
        int nextIndex = (currentIndex + 1) % locales.Count;

        LocalizationSettings.SelectedLocale = locales[nextIndex];
    }
}
