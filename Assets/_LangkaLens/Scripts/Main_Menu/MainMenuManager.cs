using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    [Header("UI Panels")]
    [SerializeField] private GameObject mainCanvas; // Reference to your root canvas
    [SerializeField] private GameObject languagePanel;
    [SerializeField] private GameObject mainMenuPanel;

    [Header("Interaction")]
    [SerializeField] private Button okButton;
    [SerializeField] private CanvasGroup[] languageButtons;
    [SerializeField] private float unselectedAlpha = 0.5f;

    [Header("Dynamic Language Icon")]
    [SerializeField] private RawImage btnLangIcon;
    [Tooltip("Assign the textures for each language in the same order as the locale list (e.g. 0=EN, 1=TH)")]
    [SerializeField] private Texture[] languageIcons;

    [Header("Scene Names")]
    [Tooltip("Ensure these match your Build Settings names exactly")]
    [SerializeField] private string mrSceneName = "Scene_1_MR_Phase3A";
    [SerializeField] private string vrSceneName = "Scene_2_VR_Phase3B";
    [SerializeField] private string museumSceneName = "Scene_3_Museum_Phase3C";

    // Define a key to save our state
    private const string LanguageSelectedKey = "HasSelectedLanguage";

    private void Start()
    {
        // Setup initial language state based on localization
        StartCoroutine(InitializeLanguageState());

        // Check if the user has previously selected a language (0 = false, 1 = true)
        if (PlayerPrefs.GetInt(LanguageSelectedKey, 0) == 1)
        {
            // Skip the language panel and go straight to the main menu
            languagePanel.SetActive(false);
            mainMenuPanel.SetActive(true);
        }
        else
        {
            // Show language panel for first-time setup
            languagePanel.SetActive(true);
            mainMenuPanel.SetActive(false);

            // Keep OK button disabled until a language is picked
            if (okButton != null) okButton.interactable = false;
        }
    }

    private IEnumerator InitializeLanguageState()
    {
        yield return LocalizationSettings.InitializationOperation;
        
        var locale = LocalizationSettings.SelectedLocale;
        if (locale != null)
        {
            int index = LocalizationSettings.AvailableLocales.Locales.IndexOf(locale);
            if (index >= 0)
            {
                UpdateLanguageVisuals(index);
            }
        }
    }

    public void SelectLanguage(int index)
    {
        // Stop any pending locale changes and start a new one
        StopAllCoroutines();
        StartCoroutine(SetLocaleRoutine(index));

        // Enable the OK button now that a selection has been made
        if (okButton != null) okButton.interactable = true;

        UpdateLanguageVisuals(index);
    }

    private void UpdateLanguageVisuals(int index)
    {
        // Update btn_Lang icon to match the selected language
        if (btnLangIcon != null && languageIcons != null && index >= 0 && index < languageIcons.Length)
        {
            if (languageIcons[index] != null)
            {
                btnLangIcon.texture = languageIcons[index];
            }
        }

        // Dim unselected language buttons
        if (languageButtons != null)
        {
            for (int i = 0; i < languageButtons.Length; i++)
            {
                if (languageButtons[i] != null)
                {
                    languageButtons[i].alpha = (i == index) ? 1f : unselectedAlpha;
                }
            }
        }
    }

    public void ConfirmAndContinue()
    {
        // Save state so the language panel doesn't show up again next time
        PlayerPrefs.SetInt(LanguageSelectedKey, 1);
        PlayerPrefs.Save(); // Forces the save to disk immediately

        languagePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Allows user to go back to language selection from Main Menu
    /// </summary>
    public void OpenLanguageSelector()
    {
        mainMenuPanel.SetActive(false);
        languagePanel.SetActive(true);
    }

    private IEnumerator SetLocaleRoutine(int localeID)
    {
        // Safely wait for the localization system to initialize
        yield return LocalizationSettings.InitializationOperation;
        
        if (localeID >= 0 && localeID < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[localeID];
        }
    }

    // --- Scene Management ---
    
    public void LoadMRMode()
    {
        Debug.Log("<color=cyan>[LangkaLens]</color> <color=green>Transitioning to MR Mode...</color>");
        
        // Hide canvas to act as a pseudo-loading screen
        if (mainCanvas != null) mainCanvas.SetActive(false);
        
        SceneManager.LoadSceneAsync(mrSceneName);
    }

    public void LoadVRMode()
    {
        Debug.Log("<color=cyan>[LangkaLens]</color> <color=green>Transitioning to VR Mode...</color>");
        
        if (mainCanvas != null) mainCanvas.SetActive(false);
        
        SceneManager.LoadSceneAsync(vrSceneName);
    }
    
    public void LoadMuseumMode()
    {
        Debug.Log("<color=cyan>[LangkaLens]</color> Transitioning to <color=green>Museum Mode...</color>");
        
        if (mainCanvas != null) mainCanvas.SetActive(false);
        
        SceneManager.LoadSceneAsync(museumSceneName);
    }

    public void CloseApp()
    {
        Debug.Log("<color=cyan>[LangkaLens]</color> Quitting App...");
        Application.Quit();
    }
}