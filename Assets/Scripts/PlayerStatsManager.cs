using System;
using UnityEngine;

/// <summary>
/// Global singleton that owns the player's HP (in hearts) and oxygen.
/// All other scripts should read/write through this instead of keeping their own copies.
///
/// HP is stored as float hearts (1.0 = 1 full heart) to stay compatible with the
/// existing HealthBarController / PlayerStats asset. Changes are automatically
/// forwarded to PlayerStats.Instance so the heart HUD updates without any extra work.
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    // ── Singleton ─────────────────────────────────────────────────────────────
    private static PlayerStatsManager _instance;
    public static PlayerStatsManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = FindObjectOfType<PlayerStatsManager>();
            return _instance;
        }
    }

    // ── Inspector ──────────────────────────────────────────────────────────────
    [Header("Health (1 unit = 1/4 heart  |  3 hearts = 12 units)")]
    [SerializeField] private int maxHp = 12;         // 3 hearts × 4 quarters
    [SerializeField] private int startingHp = 12;

    [Header("Oxygen")]
    [SerializeField] private float maxOxygen = 100f;
    [SerializeField] private float startingOxygen = 100f;

    // ── State ──────────────────────────────────────────────────────────────────
    private int _currentHp;
    private float _currentOxygen;

    // ── Public read-only ────────────────────────────────────────────────────────
    public int CurrentHp      => _currentHp;
    public int MaxHp          => maxHp;
    public float CurrentOxygen  => _currentOxygen;
    public float MaxOxygen      => maxOxygen;
    public bool  IsAlive        => _currentHp > 0f;

    // ── Events ─────────────────────────────────────────────────────────────────
    /// <summary>Fired whenever HP changes (damage, heal).</summary>
    public event Action OnHealthChanged;

    /// <summary>Fired whenever oxygen changes.</summary>
    public event Action OnOxygenChanged;

    /// <summary>Fired once when the player's HP reaches 0.</summary>
    public event Action OnPlayerDied;

    // ──────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
    }

    private void Start()
    {
        _currentHp     = Mathf.Clamp(startingHp, 0, maxHp);
        _currentOxygen = Mathf.Clamp(startingOxygen, 0f, maxOxygen);

        SyncToPlayerStats();
        OnHealthChanged?.Invoke();
        OnOxygenChanged?.Invoke();
    }

    // ── Health API ─────────────────────────────────────────────────────────────
    /// <summary>Deal damage to the player (in hearts).</summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive) return;

        _currentHp = Mathf.Clamp(_currentHp - (int)amount, 0, maxHp);
        SyncToPlayerStats();
        OnHealthChanged?.Invoke();

        if (_currentHp <= 0)
            OnPlayerDied?.Invoke();
        Debug.Log($"Player HP: {_currentHp}/{maxHp}");
    }

    /// <summary>Restore HP (in hearts, clamped to max).</summary>
    public void Heal(int amount)
    {
        _currentHp = Mathf.Clamp(_currentHp + amount, 0, maxHp);
        SyncToPlayerStats();
        OnHealthChanged?.Invoke();
    }

    /// <summary>Change the current max HP and optionally cap current HP.</summary>
    public void SetMaxHp(int newMax, bool refillToMax = false)
    {
        maxHp = Mathf.Max(1, newMax);
        if (refillToMax) _currentHp = maxHp;
        else             _currentHp = Mathf.Min(_currentHp, maxHp);

        SyncToPlayerStats();
        OnHealthChanged?.Invoke();
    }

    // ── Oxygen API ─────────────────────────────────────────────────────────────
    /// <summary>Add oxygen (called by OxygenBubble while player is inside).</summary>
    public void GainOxygen(float amount)
    {
        SetOxygen(_currentOxygen + amount);
    }

    /// <summary>Remove oxygen (called by PlayerManager's passive drain).</summary>
    public void DrainOxygen(float amount)
    {
        SetOxygen(_currentOxygen - amount);
    }

    private void SetOxygen(float value)
    {
        float prev = _currentOxygen;
        _currentOxygen = Mathf.Clamp(value, 0f, maxOxygen);
        if (!Mathf.Approximately(prev, _currentOxygen))
            OnOxygenChanged?.Invoke();
    }

    // ── PlayerStats bridge ─────────────────────────────────────────────────────
    /// <summary>
    /// Keeps the third-party PlayerStats singleton in sync so HealthBarController
    /// updates the heart HUD automatically.
    /// </summary>
    private void SyncToPlayerStats()
    {
        if (PlayerStats.Instance == null) return;

        // PlayerStats works in whole/fractional hearts (max 3).
        // Our internal scale is quarter-hearts, so divide by 4.
        float heartsValue = _currentHp / 4f;
        float delta = heartsValue - PlayerStats.Instance.Health;
        if (delta > 0f)
            PlayerStats.Instance.Heal(delta);
        else if (delta < 0f)
            PlayerStats.Instance.TakeDamage(-delta);
    }
}
