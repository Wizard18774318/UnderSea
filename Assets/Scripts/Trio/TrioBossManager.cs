using System.Collections;
using UnityEngine;

/// <summary>
/// Orchestrates the Trio boss fight.
/// Spawns 3 fish bosses one after another — the next spawns when the current one dies.
/// 
/// SETUP
/// 1. Create an empty GameObject called "TrioBossManager" in the scene.
/// 2. Attach this script.
/// 3. Assign the three boss prefabs (FishMid, FishDart, FishBig) in the Inspector.
/// 4. Each boss prefab needs its own boss script + SpriteRenderer + Animator +
///    Collider2D (IsTrigger) + Rigidbody2D (Kinematic).
/// 5. Set spawnPoint to where the first boss should appear (defaults to this object's position).
/// </summary>
public class TrioBossManager : MonoBehaviour
{
    [Header("Boss Prefabs  (spawn order: 1 → 2 → 3)")]
    [SerializeField] private GameObject fishMidPrefab;
    [SerializeField] private GameObject fishDartPrefab;
    [SerializeField] private GameObject fishBigPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private Vector3 spawnPosition = new Vector3(5f, 0f, 0f);
    [SerializeField] private float delayBetweenBosses = 2f;
    [SerializeField] private float fishMidScale  = 10f;
    [SerializeField] private float fishDartScale = 15f;
    [SerializeField] private float fishBigScale  = 10f;

    [Header("HP Multiplier (mouseAiming)")]
    [SerializeField] private float mouseAimHpMult = 20f;

    private int _currentBossIndex;
    private GameObject _activeBoss;
    private GameObject[] _bossPrefabs;
    private bool _fightActive;

    /// <summary>Other scripts can read which boss is currently active (0, 1, 2) or -1 if none.</summary>
    public int CurrentBossIndex => _fightActive ? _currentBossIndex : -1;

    /// <summary>True while any boss is alive.</summary>
    public bool FightActive => _fightActive;

    private void Start()
    {
        _bossPrefabs = new[] { fishMidPrefab, fishDartPrefab, fishBigPrefab };
        _currentBossIndex = 0;
        _fightActive = true;
        SpawnBoss(_currentBossIndex);
    }

    private void Update()
    {
        if (!_fightActive) return;

        // Check if current boss was destroyed
        if (_activeBoss == null)
        {
            _currentBossIndex++;
            if (_currentBossIndex >= _bossPrefabs.Length)
            {
                _fightActive = false;
                Debug.Log("[TrioBoss] All three bosses defeated!");
                return;
            }
            StartCoroutine(SpawnAfterDelay(_currentBossIndex, delayBetweenBosses));
            // Temporarily mark boss as "pending" to avoid re-triggering
            _activeBoss = gameObject; // placeholder until coroutine spawns real boss
        }
    }

    private IEnumerator SpawnAfterDelay(int index, float delay)
    {
        yield return new WaitForSeconds(delay);
        SpawnBoss(index);
    }

    private void SpawnBoss(int index)
    {
        if (_bossPrefabs[index] == null)
        {
            Debug.LogWarning($"[TrioBoss] Boss prefab at index {index} is null, skipping.");
            _activeBoss = null; // will trigger next boss check in Update
            return;
        }

        Vector3 pos = spawnPosition;
        _activeBoss = Instantiate(_bossPrefabs[index], pos, Quaternion.identity);
        float s = index == 0 ? fishMidScale : index == 1 ? fishDartScale : fishBigScale;
        _activeBoss.transform.localScale = new Vector3(s, s, 1f);

        Debug.Log($"[TrioBoss] Spawned boss #{index + 1}: {_activeBoss.name}");
    }

    /// <summary>Returns the mouse-aim HP multiplier for child bosses to query.</summary>
    public float GetHpMultiplier()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            return mouseAimHpMult;
        return 1f;
    }
}
