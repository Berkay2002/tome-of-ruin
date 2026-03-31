using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuUI : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene("ZoneA");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
