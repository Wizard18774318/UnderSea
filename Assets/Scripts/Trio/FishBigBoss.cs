using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 3 — FishBig: slow but tanky, patrols and lays mines.
///
/// BEHAVIOURS
///   • Slowly patrols back and forth across the arena.
///   • Periodically drops a mine behind itself.
///   • Mines explode on player contact OR after a timer (TrioMineScript).
///   • Phase 2 (< 50 % HP): drops mines faster, occasionally drops a cluster of 3.
///
/// SETUP
///   1. Use the FishBig prefab from Underwater Diving.
///   2. Add: Rigidbody2D (Kinematic), Collider2D (IsTrigger), Animator (FishBig),
///      SpriteRenderer, this script.
///   3. Assign minePrefab = the Mine, MineBig or MineSmall prefab from Underwater Diving
///      (make sure it has TrioMineScript + Collider2D IsTrigger).
///   4. Assign explosionPrefab = the Explosion prefab from Underwater Diving.
/// </summary>
public class FishBigBoss : MonoBehaviour
{
    // ── HP ─────────────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float maxHp = 50f;

    // ── Movement ───────────────────────────────────────────────────────────────
    [Header("Patrol")]
    [SerializeField] private float patrolSpeed  = 1.8f;
    [SerializeField] private float patrolRangeX = 6f;
    [SerializeField] private float patrolRangeY = 3f;

    // ── Mine Settings ──────────────────────────────────────────────────────────
    [Header("Mines")]
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float mineCooldown      = 3f;
    [SerializeField] private float mineLifetime      = 6f;
    [SerializeField] private int   mineContactDamage = 4;

    [Header("Phase 2 Modifiers")]
    [SerializeField] private float p2CooldownMult     = 0.5f;
    [SerializeField] private float p2SpeedMult        = 1.3f;
    [Tooltip("Chance to drop a 3-mine cluster in phase 2")]
    [SerializeField] private float p2ClusterChance    = 0.3f;
    [SerializeField] private float clusterSpread      = 1.2f;

    [Header("Mine & Explosion Scale")]
    [SerializeField] private float mineScale      = 8f;
    [SerializeField] private float explosionScale = 8f;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private float _currentHp;
    private bool  _phase2;
    private float _mineTimer;
    private Vector2 _patrolTarget;
    private SpriteRenderer _sr;
    private Transform _player;
    private List<GameObject> _mines = new List<GameObject>();

    private void Start()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            maxHp *= GameSettings.Instance.mouseAimingHpMultiplier;

        _currentHp = maxHp;
        _mineTimer = mineCooldown * 0.4f; // first mine comes fairly soon
        _sr = GetComponentInChildren<SpriteRenderer>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        PickNewPatrolTarget();
    }

    private void Update()
    {
        // ── Patrol ────────────────────────────────────────────────────────────
        float speed = _phase2 ? patrolSpeed * p2SpeedMult : patrolSpeed;
        Vector2 toTarget = _patrolTarget - (Vector2)transform.position;

        if (toTarget.magnitude < 0.5f)
            PickNewPatrolTarget();
        else
            transform.position += (Vector3)(toTarget.normalized * speed * Time.deltaTime);

        // Face movement direction
        if (_sr != null && Mathf.Abs(toTarget.x) > 0.01f)
            _sr.flipX = toTarget.x < 0;

        // ── Mine dropping ─────────────────────────────────────────────────────
        float cd = _phase2 ? mineCooldown * p2CooldownMult : mineCooldown;
        _mineTimer -= Time.deltaTime;
        if (_mineTimer <= 0f)
        {
            _mineTimer = cd;
            DropMine();
        }

        // Clean up destroyed mines from list
        _mines.RemoveAll(m => m == null);
    }

    // ── Damage ─────────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Projectile")) return;

        float dmg = 1f;
        var pm = other.GetComponent<ProjectileMovement>();
        if (pm != null) dmg = pm.getDamageAmount();

        Destroy(other.gameObject);
        TakeDamage(dmg);
    }

    private void TakeDamage(float amount)
    {
        _currentHp -= amount;
        Debug.Log($"[FishBig] HP: {_currentHp}/{maxHp}");

        if (!_phase2 && _currentHp <= maxHp * 0.5f)
        {
            _phase2 = true;
            Debug.Log("[FishBig] Phase 2 — mines everywhere!");
        }

        if (_currentHp <= 0f)
            Die();
    }

    private void Die()
    {
        // Explode all remaining mines
        foreach (var m in _mines)
        {
            if (m == null) continue;
            if (explosionPrefab != null)
            {
                var ex = Instantiate(explosionPrefab, m.transform.position, Quaternion.identity);
                ex.transform.localScale = new Vector3(explosionScale, explosionScale, 1f);
            }
            Destroy(m);
        }

        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = new Vector3(explosionScale, explosionScale, 1f);
        }

        Debug.Log("[FishBig] Defeated!");
        Destroy(gameObject);
    }

    // ── Mine Dropping ──────────────────────────────────────────────────────────
    private void DropMine()
    {
        if (minePrefab == null) return;

        // Phase 2 cluster?
        if (_phase2 && Random.value < p2ClusterChance)
        {
            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = Random.insideUnitCircle * clusterSpread;
                SpawnOneMine((Vector2)transform.position + offset);
            }
        }
        else
        {
            SpawnOneMine(transform.position);
        }
    }

    private void SpawnOneMine(Vector2 pos)
    {
        GameObject mine = Instantiate(minePrefab, pos, Quaternion.identity);
        mine.transform.localScale = new Vector3(mineScale, mineScale, 1f);
        var script = mine.GetComponent<TrioMineScript>();
        if (script != null)
            script.Init(mineLifetime, mineContactDamage, explosionPrefab, explosionScale);
        _mines.Add(mine);
    }

    // ── Patrol ─────────────────────────────────────────────────────────────────
    private void PickNewPatrolTarget()
    {
        float x = Random.Range(-patrolRangeX, patrolRangeX);
        float y = Random.Range(-patrolRangeY, patrolRangeY);
        _patrolTarget = new Vector2(x, y);
    }
}
