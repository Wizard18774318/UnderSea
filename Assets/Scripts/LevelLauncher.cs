using System;
using System.IO;
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

        if (!TryGetScenePath(sceneName, out string path))
        {
            Debug.LogError($"{nameof(LevelLauncher)}: Scene '{sceneName}' is not listed in Build Settings. Add it via File > Build Settings (or Build Profiles).");
            return;
        }

        SceneManager.LoadScene(path);
    }

    private static bool TryGetScenePath(string identifier, out string path)
    {
        int sceneCount = SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < sceneCount; i++)
        {
            string candidatePath = SceneUtility.GetScenePathByBuildIndex(i);
            string candidateName = Path.GetFileNameWithoutExtension(candidatePath);

            if (string.Equals(candidateName, identifier, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(candidatePath, identifier, StringComparison.OrdinalIgnoreCase))
            {
                path = candidatePath;
                return true;
            }
        }

        path = null;
        return false;
    }
}
