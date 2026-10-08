using UnityEngine;

public class SceneChanger : MonoBehaviour
{
    public void GoToMainMenu()
    {
        // Ensure this exactly matches the name of your main menu scene
        SceneLoadingScreen.LoadScene("Main_Menu_Scene");
    }
}
