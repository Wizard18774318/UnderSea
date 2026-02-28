using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Handles main menu buttons such as Play and Quit.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName;

    public void PlayGame()
    {
        if (string.IsNullOrEmpty(gameplaySceneName))
        {
            Debug.LogWarning("MainMenuController: No gameplay scene name assigned.");
            return;
        }

        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplaySceneName);
    }

    public void QuitGame()
    {
    #if UNITY_EDITOR
        EditorApplication.isPlaying = false;
    #else
        Application.Quit();
    #endif
    }
}
