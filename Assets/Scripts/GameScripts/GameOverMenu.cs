using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverMenu : MonoBehaviour
{
    public void RestartGame()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("Respawn Until GameOver"))
        {
            SceneManager.LoadScene("Respawn Until GameOver");
        }
        else if (Application.CanStreamedLevelBeLoaded("Scene 1"))
        {
            SceneManager.LoadScene("Scene 1");
        }
        else
        {
            SceneManager.LoadScene(0);
        }
    }

    public void MainMenu()
    {
        Time.timeScale = 1f;
        if (Application.CanStreamedLevelBeLoaded("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }
}
