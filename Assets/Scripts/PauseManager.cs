using UnityEngine;

// Handles global pause input and menu visibility.
public class PauseManager : MonoBehaviour
{
    public static bool IsPaused { get; private set; }

    [SerializeField] private GameObject pauseMenuRoot;
    [SerializeField] private GameObject optionsMenuRoot;

    void Awake()
    {
        InitializeMenus();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        SetPaused(!IsPaused);
    }

    public void SetPaused(bool paused)
    {
        if (IsPaused == paused)
        {
            return;
        }

        IsPaused = paused;
        Time.timeScale = paused ? 0f : 1f;
        AudioListener.pause = paused;

        if (!paused)
        {
            HideAllMenus();
        }
        else
        {
            ShowPauseMenuPanel();
        }
    }

    void OnDisable()
    {
        if (IsPaused)
        {
            SetPaused(false);
        }
    }

    public void ShowOptionsMenu()
    {
        if (!IsPaused)
        {
            SetPaused(true);
        }

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.SetActive(true);
        }
    }

    public void ShowPauseMenuPanel()
    {
        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.SetActive(false);
        }

        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(true);
        }
    }

    public void ResumeGame()
    {
        SetPaused(false);
    }

    void InitializeMenus()
    {
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.SetActive(false);
        }
    }

    void HideAllMenus()
    {
        if (pauseMenuRoot != null)
        {
            pauseMenuRoot.SetActive(false);
        }

        if (optionsMenuRoot != null)
        {
            optionsMenuRoot.SetActive(false);
        }
    }
}
