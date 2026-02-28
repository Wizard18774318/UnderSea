using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Drives a UI Image (set to Filled / Horizontal) to display the player's oxygen.
/// Attach to a Canvas GameObject alongside an Image component, or assign the Image
/// in the Inspector. Also optionally tints the bar based on oxygen level.
///
/// Setup in Unity:
///   1. Create a UI Image (fill method = Filled, Fill Method = Horizontal).
///   2. Attach this script to the same GameObject (or any HUD object).
///   3. Drag the Image into the "Oxygen Fill Image" field.
/// </summary>
public class OxygenBarController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image oxygenFillImage;

    [Header("Colour Tint")]
    [SerializeField] private bool useTint = true;
    [SerializeField] private Color fullColor   = new Color(0.2f, 0.8f, 1f);   // cyan-blue
    [SerializeField] private Color lowColor    = new Color(1f,   0.3f, 0.1f); // orange-red
    [SerializeField] [Range(0f, 1f)] private float lowThreshold = 0.25f;

    private void Start()
    {
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnOxygenChanged += Refresh;
            Refresh();
        }
        else
        {
            Debug.LogWarning("OxygenBarController: PlayerStatsManager not found in scene.");
        }
    }

    private void OnDestroy()
    {
        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnOxygenChanged -= Refresh;
    }

    private void Refresh()
    {
        if (oxygenFillImage == null || PlayerStatsManager.Instance == null) return;

        float t = PlayerStatsManager.Instance.CurrentOxygen / PlayerStatsManager.Instance.MaxOxygen;
        oxygenFillImage.fillAmount = t;

        if (useTint)
        {
            float tintT = Mathf.Clamp01(t / Mathf.Max(lowThreshold, 0.001f));
            oxygenFillImage.color = Color.Lerp(lowColor, fullColor, tintT);
        }
    }
}
