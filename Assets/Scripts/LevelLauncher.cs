using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelLauncher : MonoBehaviour
{
    [SerializeField] private string sceneName;

    public string SceneName => sceneName;

    public void Launch()
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogWarning($"{nameof(LevelLauncher)} on {name} has no scene assigned.");
            return;
        }

        SceneManager.LoadScene(sceneName);
    }
}
