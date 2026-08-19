using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneChanger : MonoBehaviour
{
    public void GoToMainMenu()
    {
        // Ensure this exactly matches the name of your main menu scene
        SceneManager.LoadScene("Main_Menu_Scene"); 
    }
}