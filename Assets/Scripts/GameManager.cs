using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string roadmapSceneName = "Roadmap";

    public void LoadRoadmap()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(roadmapSceneName);
    }
}
