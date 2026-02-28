using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 1 — FishMid: follows the player and spawns fish minion formations.
///
/// BEHAVIOURS
///   • Follows the player at a leisurely speed.
///   • Periodically spawns a wave of small fish in a formation (V or circle).
///   • When below 50 % HP the formations get larger and spawn faster.
///
/// SETUP
///   1. Use the FishMid prefab from Underwater Diving.
///   2. Add: Rigidbody2D (Kinematic), Collider2D (IsTrigger), Animator (FishMid),
///      SpriteRenderer, this script.
///   3. Assign fishMinionPrefab (a small FishMid sprite with FishMinionScript).
///   4. Tag the collider so player projectiles ("Projectile") can hit it.
/// </summary>
public class FishMidBoss : MonoBehaviour
{
    // ── HP ─────────────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float maxHp = 30f;

    // ── Movement ───────────────────────────────────────────────────────────────
    [Header("Follow Player")]
    [SerializeField] private float followSpeed = 2.5f;
    [SerializeField] private float keepDistance = 3f;

    // ── Spawn Waves ────────────────────────────────────────────────────────────
    [Header("Fish Minion Waves")]
    [SerializeField] private GameObject fishMinionPrefab;
    [SerializeField] private float waveCooldown = 4f;
    [SerializeField] private int   waveSize     = 5;
    [Tooltip("Phase 2: wave cooldown multiplier")]
    [SerializeField] private float p2CooldownMult = 0.6f;
    [Tooltip("Phase 2: extra fish per wave")]
    [SerializeField] private int   p2ExtraFish    = 3;

    [Header("Minion Scale")]
    [SerializeField] private float minionScale = 10f;

    [Header("Formation")]
    [SerializeField] private float vSpreadAngle  = 30f;
    [SerializeField] private float vSpacing       = 1f;
    [SerializeField] private float circleRadius   = 6f;

    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectScale = 10f;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private float  _currentHp;
    private float  _waveTimer;
    private bool   _phase2;
    private Transform _player;
    private SpriteRenderer _sr;
    private List<GameObject> _minions = new List<GameObject>();

    private enum Formation { V, Circle, Spiral, Pincer }
    private Formation _nextFormation = Formation.V;
    private int _formationIndex = 0;

    private void Start()
    {
        // HP multiplier from GameSettings
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            maxHp *= GameSettings.Instance.mouseAimingHpMultiplier;

        _currentHp = maxHp;
        _waveTimer = waveCooldown * 0.5f; // first wave comes a bit sooner

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void Update()
    {
        if (_player == null) return;

        // ── Follow ────────────────────────────────────────────────────────────
        Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position);
        float dist = toPlayer.magnitude;

        if (dist > keepDistance)
        {
            Vector2 step = toPlayer.normalized * followSpeed * Time.deltaTime;
            transform.position += (Vector3)step;
        }

        // Face the player
        if (_sr != null)
        {
            bool facingRight = toPlayer.x > 0;
            _sr.flipX = !facingRight;
        }

        // ── Wave spawning ─────────────────────────────────────────────────────
        float cd = _phase2 ? waveCooldown * p2CooldownMult : waveCooldown;
        _waveTimer -= Time.deltaTime;
        if (_waveTimer <= 0f)
        {
            _waveTimer = cd;
            SpawnWave();
        }
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
        Debug.Log($"[FishMid] HP: {_currentHp}/{maxHp}");

        if (!_phase2 && _currentHp <= maxHp * 0.5f)
        {
            _phase2 = true;
            followSpeed *= 1.3f;
            Debug.Log("[FishMid] Phase 2!");
        }

        if (_currentHp <= 0f)
            Die();
    }

    private void Die()
    {
        // Clean up minions
        foreach (var m in _minions)
            if (m != null) Destroy(m);

        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = new Vector3(deathEffectScale, deathEffectScale, 1f);
        }

        Debug.Log("[FishMid] Defeated!");
        Destroy(gameObject);
    }

    // ── Formation Spawning ─────────────────────────────────────────────────────
    private void SpawnWave()
    {
        if (fishMinionPrefab == null || _player == null) return;

        int count = _phase2 ? waveSize + p2ExtraFish : waveSize;

        // Rotate through all 5 formations
        Formation[] pool = _phase2
            ? new[] { Formation.V, Formation.Circle, Formation.Spiral, Formation.Pincer }
            : new[] { Formation.V, Formation.Circle, Formation.Spiral };

        Formation f = pool[_formationIndex % pool.Length];
        _formationIndex++;

        switch (f)
        {
            case Formation.V:        SpawnVFormation(count);      break;
            case Formation.Circle:   SpawnCircleFormation(count); break;
            case Formation.Spiral:   StartCoroutine(SpawnSpiralFormation(count)); break;
            case Formation.Pincer:   SpawnPincerFormation(count);   break;
        }
    }

    private void SpawnVFormation(int count)
    {
        Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        for (int i = 0; i < count; i++)
        {
            int side = (i % 2 == 0) ? 1 : -1;
            int row  = (i + 1) / 2;

            float angle = baseAngle + side * vSpreadAngle * (row / (float)Mathf.Max(1, (count - 1) / 2));
            Vector2 offset = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            ) * (row * vSpacing);

            SpawnMinion(transform.position + (Vector3)offset, dir, followPlayer: false);
        }
    }

    private void SpawnCircleFormation(int count)
    {
        float step = 360f / count;
        for (int i = 0; i < count; i++)
        {
            float angle = i * step * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * circleRadius;
            Vector3 spawnPos = transform.position + (Vector3)offset;
            Vector2 dir = ((Vector2)_player.position - (Vector2)spawnPos).normalized;
            // Circle minions always home
            SpawnMinion(spawnPos, dir, followPlayer: true);
        }
    }

    /// <summary>Minions pop out one at a time in an expanding spiral — 50/50 homing.</summary>
    private IEnumerator SpawnSpiralFormation(int count)
    {
        float angleStep = 137.5f;
        float radiusStep = circleRadius / count;

        for (int i = 0; i < count; i++)
        {
            float angle = i * angleStep * Mathf.Deg2Rad;
            float r     = radiusStep * (i + 1);
            Vector2 offset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
            Vector3 spawnPos = transform.position + (Vector3)offset;
            Vector2 dir = ((Vector2)_player.position - (Vector2)spawnPos).normalized;
            bool homing = (Random.value > 0.5f);
            SpawnMinion(spawnPos, dir, followPlayer: homing);
            yield return new WaitForSeconds(0.12f);
        }
    }

    /// <summary>Fish stream in from two sides — always fly straight (crossing pattern).</summary>
    private void SpawnPincerFormation(int count)
    {
        int half = count / 2;
        float screenEdge = 12f;
        float playerY    = _player != null ? _player.position.y : 0f;
        float spread     = circleRadius * 0.5f;

        for (int i = 0; i < half; i++)
        {
            float y = playerY + (i - half / 2f) * spread;
            SpawnMinion(new Vector3(-screenEdge, y, 0f), Vector2.right, followPlayer: false);
        }

        for (int i = 0; i < count - half; i++)
        {
            float y = playerY + (i - (count - half) / 2f) * spread;
            SpawnMinion(new Vector3(screenEdge, y, 0f), Vector2.left, followPlayer: false);
        }
    }

    private void SpawnMinion(Vector3 pos, Vector2 dir, bool followPlayer = false)
    {
        GameObject m = Instantiate(fishMinionPrefab, pos, Quaternion.identity);
        m.transform.localScale = new Vector3(minionScale, minionScale, 1f);

        var minion = m.GetComponent<FishMinionScript>();
        if (minion != null) minion.Init(dir, followPlayer);
        _minions.Add(m);
    }
}
