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

    [Header("Scene Names")]
    [Tooltip("Ensure these match your Build Settings names exactly")]
    [SerializeField] private string mrSceneName = "Scene_1_MR_Phase3A";
    [SerializeField] private string vrSceneName = "Scene_2_VR_Phase3B";
    [SerializeField] private string museumSceneName = "Scene_3_Museum_Phase3C";

    // Define a key to save our state
    private const string LanguageSelectedKey = "HasSelectedLanguage";

    private void Start()
    {
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

    public void SelectLanguage(int index)
    {
        // Stop any pending locale changes and start a new one
        StopAllCoroutines();
        StartCoroutine(SetLocaleRoutine(index));

        // Enable the OK button now that a selection has been made
        if (okButton != null) okButton.interactable = true;
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