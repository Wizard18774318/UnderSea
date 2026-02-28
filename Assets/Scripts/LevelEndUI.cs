using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Shows death / victory overlays and routes button actions. Attach this to a UI
/// prefab that contains both panels, then hook it up to PlayerStatsManager and a
/// level objective (e.g. BossFishManager).
/// </summary>
public class LevelEndUI : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private GameObject victoryPanel;

    [Header("Navigation")]
    [SerializeField] private string mainMenuSceneName;
    [SerializeField] private LevelLauncher nextLevelLauncher;

    [Header("Victory Source")]
    [SerializeField] private BossFishManager bossObjective;
    [SerializeField] private bool autoFindBossInScene = true;

    [Header("Auto Panels (optional)")]
    [SerializeField] private bool autoCreatePanelsIfMissing = true;
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.78f);
    [SerializeField] private string deathHeadline = "YOU DIED";
    [SerializeField] private string deathBody = "Press Restart to try again.";
    [SerializeField] private string victoryHeadline = "VICTORY";
    [SerializeField] private string victoryBody = "Boss defeated!";

    private PlayerStatsManager _playerStats;
    private bool _hasEnded;
    private Transform _runtimeCanvasRoot;

    private void Awake()
    {
        BuildRuntimePanelsIfNeeded();
        HidePanels();
    }

    private void OnEnable()
    {
        AttachPlayerCallbacks();
        AttachBossCallbacks();
    }

    private IEnumerator Start()
    {
        if (bossObjective == null)
        {
            yield return null;
            AttachBossCallbacks();
        }
    }

    private void OnDisable()
    {
        DetachPlayerCallbacks();
        DetachBossCallbacks();
    }

    private void AttachPlayerCallbacks()
    {
        _playerStats = PlayerStatsManager.Instance;
        if (_playerStats != null)
            _playerStats.OnPlayerDied += HandlePlayerDeath;
    }

    private void AttachBossCallbacks()
    {
        bossObjective = ResolveBossObjective(bossObjective);

        if (bossObjective != null)
            bossObjective.OnBossKilled += HandleVictory;
    }

    private void DetachPlayerCallbacks()
    {
        if (_playerStats != null)
            _playerStats.OnPlayerDied -= HandlePlayerDeath;
    }

    private void DetachBossCallbacks()
    {
        if (bossObjective != null)
            bossObjective.OnBossKilled -= HandleVictory;
    }

    private void HandlePlayerDeath()
    {
        ShowEndState(deathPanel);
    }

    private void HandleVictory()
    {
        ShowEndState(victoryPanel);
    }

    /// <summary>Allow triggering victory from Timeline or UnityEvent.</summary>
    public void TriggerVictoryManually()
    {
        HandleVictory();
    }

    public void RestartLevel()
    {
        ResumeGameplayState();
        Scene current = SceneManager.GetActiveScene();
        SceneManager.LoadScene(current.path);
    }

    public void LoadMainMenu()
    {
        if (string.IsNullOrEmpty(mainMenuSceneName))
        {
            Debug.LogWarning("LevelEndUI: No main menu scene name assigned.");
            return;
        }

        ResumeGameplayState();
        SceneManager.LoadScene(mainMenuSceneName);
    }

    public void LoadNextLevel()
    {
        if (nextLevelLauncher == null)
        {
            Debug.LogWarning("LevelEndUI: No next level launcher assigned.");
            return;
        }

        ResumeGameplayState();
        nextLevelLauncher.Launch();
    }

    private void ShowEndState(GameObject panelToEnable)
    {
        if (_hasEnded)
            return;

        BuildRuntimePanelsIfNeeded();

        _hasEnded = true;
        ForceUnpauseIfNeeded();
        Time.timeScale = 0f;
        AudioListener.pause = true;

        if (deathPanel != null)
            deathPanel.SetActive(panelToEnable == deathPanel);

        if (victoryPanel != null)
            victoryPanel.SetActive(panelToEnable == victoryPanel);

        DetachPlayerCallbacks();
        DetachBossCallbacks();
    }

    private void HidePanels()
    {
        BuildRuntimePanelsIfNeeded();

        if (deathPanel != null)
            deathPanel.SetActive(false);
        if (victoryPanel != null)
            victoryPanel.SetActive(false);
    }

    private static void ResumeGameplayState()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
    }

    private static void ForceUnpauseIfNeeded()
    {
        if (!PauseManager.IsPaused)
            return;

        PauseManager pauseManager = FindFirstObjectByType<PauseManager>();
        if (pauseManager != null)
            pauseManager.SetPaused(false);
    }

    private void BuildRuntimePanelsIfNeeded()
    {
        if (!autoCreatePanelsIfMissing)
            return;

        bool needDeathPanel = deathPanel == null;
        bool needVictoryPanel = victoryPanel == null;
        if (!needDeathPanel && !needVictoryPanel)
            return;

        Transform parent = ResolvePanelParent();

        if (needDeathPanel)
            deathPanel = CreateOverlayPanel(parent, "DeathOverlay", deathHeadline, deathBody);

        if (needVictoryPanel)
            victoryPanel = CreateOverlayPanel(parent, "VictoryOverlay", victoryHeadline, victoryBody);
    }

    private Transform ResolvePanelParent()
    {
        if (_runtimeCanvasRoot != null)
            return _runtimeCanvasRoot;

        Canvas existingCanvas = GetComponentInParent<Canvas>();
        if (existingCanvas != null)
        {
            _runtimeCanvasRoot = existingCanvas.transform;
            return _runtimeCanvasRoot;
        }

        GameObject canvasGO = new GameObject("LevelEndUI_AutoCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        _runtimeCanvasRoot = canvasGO.transform;
        return _runtimeCanvasRoot;
    }

    private GameObject CreateOverlayPanel(Transform parent, string name, string headline, string body)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.color = overlayColor;

        CreateText(panel.transform, name + "_Headline", headline, TextAnchor.MiddleCenter, 60, 0.22f);
        CreateText(panel.transform, name + "_Body", body, TextAnchor.MiddleCenter, 30, -0.12f);

        panel.SetActive(false);
        return panel;
    }

    private void CreateText(Transform parent, string name, string text, TextAnchor anchor, int fontSize, float normalizedYOffset)
    {
        GameObject textGO = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        textGO.transform.SetParent(parent, false);

        RectTransform rect = textGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0.5f);
        rect.anchorMax = new Vector2(0.85f, 0.5f);
        rect.sizeDelta = new Vector2(0f, 200f);
        rect.anchoredPosition = new Vector2(0f, normalizedYOffset * Screen.height * 0.5f);

        Text uiText = textGO.GetComponent<Text>();
        uiText.text = text;
        uiText.alignment = anchor;
        uiText.fontSize = fontSize;
        uiText.color = Color.white;
        uiText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        uiText.horizontalOverflow = HorizontalWrapMode.Wrap;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
    }

    private BossFishManager ResolveBossObjective(BossFishManager candidate)
    {
        if (candidate != null && !candidate.CountsAsLevelBoss)
        {
            Debug.Log($"LevelEndUI: Ignoring assigned boss '{candidate.name}' because it is flagged as a non-level boss.", candidate);
            candidate = null;
        }

        if (candidate == null && autoFindBossInScene)
        {
            BossFishManager[] bosses = FindObjectsByType<BossFishManager>(FindObjectsSortMode.None);
            foreach (BossFishManager boss in bosses)
            {
                if (boss != null && boss.CountsAsLevelBoss)
                {
                    candidate = boss;
                    break;
                }
            }

            if (candidate == null && bosses.Length > 0)
                Debug.Log("LevelEndUI: No level boss in this scene is flagged as a victory objective. Assign one manually or enable Counts As Level Boss on the correct enemy.");
        }

        return candidate;
    }
}
