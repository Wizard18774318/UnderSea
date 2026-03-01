using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string roadmapSceneName = "Roadmap";
    [SerializeField] private Toggle mouseAimingToggle;

    public void LoadRoadmap()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(roadmapSceneName);
    }

    public void SetMouseAiming()
    {
        if (GameSettings.Instance == null)
        {
            Debug.LogWarning("GameManager: GameSettings.Instance is null — make sure GameSettings exists in the Menu scene.");
            return;
        }

        if (mouseAimingToggle == null)
        {
            Debug.LogWarning("GameManager: mouseAimingToggle is not assigned.");
            return;
        }

        GameSettings.Instance.mouseAiming = mouseAimingToggle.isOn;
        Debug.Log($"Mouse aiming set to: {mouseAimingToggle.isOn}");
    }
}
