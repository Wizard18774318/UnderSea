using UnityEngine;

/// <summary>
/// Persistent singleton for global game settings.
/// Place on a GameObject in your first/main scene — it will survive all scene loads.
/// Access anywhere via GameSettings.Instance.
/// </summary>
public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("Controls")]
    [Tooltip("True = player rotates toward mouse | False = snaps to 45° movement direction")]
    public bool mouseAiming = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
}
