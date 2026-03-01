using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

// Handles main menu buttons such as Play, Options, and Quit.
public class MainMenuController : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName;
    [SerializeField] private GameObject mainMenuRoot;
    [SerializeField] private GameObject optionsMenuRoot;

    private void Awake()
    {
        if (optionsMenuRoot != null)
            optionsMenuRoot.SetActive(false);
    }

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

    public void ToggleOptions()
    {
        Debug.Log($"ToggleOptions called. mainMenuRoot={mainMenuRoot != null}, optionsMenuRoot={optionsMenuRoot != null}");

        bool showOptions = optionsMenuRoot != null && !optionsMenuRoot.activeSelf;

        if (mainMenuRoot != null)
            mainMenuRoot.SetActive(!showOptions);
        if (optionsMenuRoot != null)
            optionsMenuRoot.SetActive(showOptions);
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
