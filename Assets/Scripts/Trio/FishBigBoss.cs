using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boss 3 — FishBig: slow tanky mine-layer with 6 attack patterns.
///
/// ATTACKS (rotated through):
///   1. Mine Wall       — line of mines across the screen (horizontal or vertical)
///   2. Mine Rain       — mines rain from the top in random columns
///   3. Spiral Mine     — mines spiral outward from FishBig
///   4. Chase Mines     — mines that slowly drift toward the player
///   5. Mine Ring       — circle of mines spawned around the player
///   6. Carpet Bomb Run — FishBig charges across the screen dropping mines
///
/// Between attacks the boss still patrols slowly.
/// Phase 2 (< 50 % HP): faster patrols, shorter cooldowns, extra mines per pattern.
/// </summary>
public class FishBigBoss : MonoBehaviour
{
    // ── HP ─────────────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float maxHp = 50f;

    // ── Movement ───────────────────────────────────────────────────────────────
    [Header("Patrol (between attacks)")]
    [SerializeField] private float patrolSpeed   = 1.8f;
    [SerializeField] private float patrolRangeX  = 6f;
    [SerializeField] private float patrolRangeY  = 3f;

    // ── Mine Core Settings ─────────────────────────────────────────────────────
    [Header("Mine Prefabs")]
    [SerializeField] private GameObject minePrefab;
    [SerializeField] private GameObject explosionPrefab;
    [SerializeField] private float mineLifetime      = 6f;
    [SerializeField] private int   mineContactDamage = 4;
    [SerializeField] private float mineScale         = 8f;
    [SerializeField] private float explosionScale    = 8f;

    // ── Attack Timing ──────────────────────────────────────────────────────────
    [Header("Attack Timing")]
    [SerializeField] private float patrolTimeBetween = 3f;
    [SerializeField] private float p2PatrolTimeMult  = 0.55f;

    // ── Phase 2 ────────────────────────────────────────────────────────────────
    [Header("Phase 2 Modifiers")]
    [SerializeField] private float p2SpeedMult    = 1.3f;
    [SerializeField] private float p2ExtraMines   = 1.4f;  // multiplier for mine counts

    // ── Mine Wall ──────────────────────────────────────────────────────────────
    [Header("Mine Wall")]
    [SerializeField] private int   wallMineCount  = 18;
    [SerializeField] private float wallSpanX      = 20f;
    [SerializeField] private float wallSpanY      = 12f;

    // ── Mine Rain ──────────────────────────────────────────────────────────────
    [Header("Mine Rain")]
    [SerializeField] private int   rainMineCount  = 22;
    [SerializeField] private float rainDuration   = 3f;
    [SerializeField] private float rainTopY       = 8f;
    [SerializeField] private float rainSpanX      = 18f;

    // ── Spiral ─────────────────────────────────────────────────────────────────
    [Header("Spiral Mine Field")]
    [SerializeField] private int   spiralMineCount = 18;
    [SerializeField] private float spiralMaxRadius = 9f;
    [SerializeField] private float spiralDropDelay = 0.09f;

    // ── Chase Mines ────────────────────────────────────────────────────────────
    [Header("Chase Mines")]
    [SerializeField] private int   chaseMineCount = 10;
    [SerializeField] private float chaseSpeed     = 1.5f;

    // ── Mine Ring ──────────────────────────────────────────────────────────────
    [Header("Mine Ring (around player)")]
    [SerializeField] private int   ringMineCount  = 14;
    [SerializeField] private float ringRadius     = 5.5f;

    // ── Carpet Bomb ────────────────────────────────────────────────────────────
    [Header("Carpet Bomb Run")]
    [SerializeField] private float carpetSpeed      = 8f;
    [SerializeField] private float carpetDropRate   = 0.10f;
    [SerializeField] private float carpetOffscreen  = 15f;

    // ── Death ──────────────────────────────────────────────────────────────────
    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private float _currentHp;
    private bool  _phase2;
    private Vector2 _patrolTarget;
    private SpriteRenderer _sr;
    private Transform _player;
    private List<GameObject> _mines = new List<GameObject>();
    private int _attackIndex;
    private bool _attacking;

    private enum Atk { MineWall, MineRain, SpiralField, ChaseMines, MineRing, CarpetBomb }
    private Atk[] _phase1Pool = { Atk.MineWall, Atk.MineRain, Atk.SpiralField };
    private Atk[] _allPool    = { Atk.MineWall, Atk.MineRain, Atk.SpiralField,
                                  Atk.ChaseMines, Atk.MineRing, Atk.CarpetBomb };

    // ── Init ───────────────────────────────────────────────────────────────────
    private void Start()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            maxHp *= GameSettings.Instance.mouseAimingHpMultiplier;

        _currentHp = maxHp;
        _sr = GetComponentInChildren<SpriteRenderer>();
        _player = GameObject.FindGameObjectWithTag("Player")?.transform;

        var enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.enabled = false;

        PickNewPatrolTarget();
        StartCoroutine(AttackLoop());
    }

    // ── Update: patrol between attacks ─────────────────────────────────────────
    private void Update()
    {
        if (_attacking) return; // movement handled by attack coroutine

        float speed = _phase2 ? patrolSpeed * p2SpeedMult : patrolSpeed;
        Vector2 toTarget = _patrolTarget - (Vector2)transform.position;

        if (toTarget.magnitude < 0.5f) PickNewPatrolTarget();
        else transform.position += (Vector3)(toTarget.normalized * speed * Time.deltaTime);

        if (_sr != null && Mathf.Abs(toTarget.x) > 0.01f)
            FaceDir(toTarget.x);

        _mines.RemoveAll(m => m == null);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  ATTACK LOOP
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator AttackLoop()
    {
        // Short initial patrol
        yield return new WaitForSeconds(patrolTimeBetween * 0.5f);

        while (_currentHp > 0f)
        {
            Atk[] pool = _phase2 ? _allPool : _phase1Pool;
            Atk atk = pool[_attackIndex % pool.Length];
            _attackIndex++;

            _attacking = true;
            switch (atk)
            {
                case Atk.MineWall:    yield return DoMineWall();    break;
                case Atk.MineRain:    yield return DoMineRain();    break;
                case Atk.SpiralField: yield return DoSpiralField(); break;
                case Atk.ChaseMines:  yield return DoChaseMines();  break;
                case Atk.MineRing:    yield return DoMineRing();    break;
                case Atk.CarpetBomb:  yield return DoCarpetBomb();  break;
            }
            _attacking = false;

            float pause = _phase2 ? patrolTimeBetween * p2PatrolTimeMult : patrolTimeBetween;
            yield return new WaitForSeconds(pause);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  1. MINE WALL — line of mines across the screen
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoMineWall()
    {
        int count = MineCount(wallMineCount);
        bool horizontal = Random.value > 0.5f;

        if (horizontal)
        {
            // Horizontal wall at a random Y
            float y = Random.Range(-wallSpanY * 0.4f, wallSpanY * 0.4f);
            float startX = -wallSpanX * 0.5f;
            float step = wallSpanX / (count - 1);
            for (int i = 0; i < count; i++)
            {
                SpawnMine(new Vector2(startX + step * i, y));
                yield return new WaitForSeconds(0.06f);
            }
        }
        else
        {
            // Vertical wall at a random X
            float x = Random.Range(-wallSpanX * 0.35f, wallSpanX * 0.35f);
            float startY = -wallSpanY * 0.5f;
            float step = wallSpanY / (count - 1);
            for (int i = 0; i < count; i++)
            {
                SpawnMine(new Vector2(x, startY + step * i));
                yield return new WaitForSeconds(0.06f);
            }
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  2. MINE RAIN — mines drop from the top
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoMineRain()
    {
        int count = MineCount(rainMineCount);
        float interval = rainDuration / count;

        for (int i = 0; i < count; i++)
        {
            float x = Random.Range(-rainSpanX * 0.5f, rainSpanX * 0.5f);
            float y = rainTopY;
            // Mine falls to a random resting Y
            float restY = Random.Range(-3f, 2f);
            StartCoroutine(DropMineDown(new Vector2(x, y), restY));
            yield return new WaitForSeconds(interval);
        }
    }

    private IEnumerator DropMineDown(Vector2 startPos, float targetY)
    {
        GameObject mine = SpawnMine(startPos);
        if (mine == null) yield break;

        float fallSpeed = 6f;
        while (mine != null && mine.transform.position.y > targetY)
        {
            mine.transform.position += Vector3.down * fallSpeed * Time.deltaTime;
            yield return null;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  3. SPIRAL MINE FIELD — expanding spiral around FishBig
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoSpiralField()
    {
        int count = MineCount(spiralMineCount);
        float angleStep = 360f / count * 2.5f; // >360 so it spirals
        float radiusStep = spiralMaxRadius / count;

        Vector2 center = transform.position;

        for (int i = 0; i < count; i++)
        {
            float angle = angleStep * i * Mathf.Deg2Rad;
            float radius = radiusStep * (i + 1);
            Vector2 pos = center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            SpawnMine(pos);
            yield return new WaitForSeconds(spiralDropDelay);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  4. CHASE MINES — mines that slowly drift toward the player
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoChaseMines()
    {
        int count = MineCount(chaseMineCount);

        for (int i = 0; i < count; i++)
        {
            // Spawn scattered around FishBig
            Vector2 offset = Random.insideUnitCircle * 2f;
            GameObject mine = SpawnMine((Vector2)transform.position + offset);
            if (mine != null && _player != null)
            {
                var script = mine.GetComponent<TrioMineScript>();
                if (script != null)
                    script.SetChase(_player, chaseSpeed);
            }
            yield return new WaitForSeconds(0.2f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  5. MINE RING — circle of mines around the player
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoMineRing()
    {
        if (_player == null) { yield return DoMineWall(); yield break; }

        int count = MineCount(ringMineCount);
        Vector2 center = _player.position;
        float angleStep = 360f / count;

        for (int i = 0; i < count; i++)
        {
            float rad = angleStep * i * Mathf.Deg2Rad;
            Vector2 pos = center + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ringRadius;
            SpawnMine(pos);
            yield return new WaitForSeconds(0.05f);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  6. CARPET BOMB RUN — charges across screen dropping mines
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoCarpetBomb()
    {
        // Pick direction
        bool fromLeft = Random.value > 0.5f;
        float startX = fromLeft ? -carpetOffscreen : carpetOffscreen;
        float endX   = fromLeft ?  carpetOffscreen : -carpetOffscreen;
        float y = transform.position.y;

        // Teleport to starting side
        transform.position = new Vector3(startX, y, 0f);
        FaceDir(fromLeft ? 1f : -1f);

        float speed = _phase2 ? carpetSpeed * p2SpeedMult : carpetSpeed;
        Vector2 dir = fromLeft ? Vector2.right : Vector2.left;
        float dropTimer = 0f;

        // Charge across, dropping mines
        while ((fromLeft && transform.position.x < endX) ||
               (!fromLeft && transform.position.x > endX))
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);

            dropTimer -= Time.deltaTime;
            if (dropTimer <= 0f)
            {
                dropTimer = carpetDropRate;
                SpawnMine(transform.position + Vector3.down * 0.5f);
            }
            yield return null;
        }

        // Return to a patrol position
        PickNewPatrolTarget();
        transform.position = new Vector3(_patrolTarget.x, _patrolTarget.y, 0f);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  MINE SPAWNING
    // ══════════════════════════════════════════════════════════════════════════
    private GameObject SpawnMine(Vector2 pos)
    {
        if (minePrefab == null) return null;

        GameObject mine = Instantiate(minePrefab, pos, Quaternion.identity);
        mine.transform.localScale = new Vector3(mineScale, mineScale, 1f);
        var script = mine.GetComponent<TrioMineScript>();
        if (script != null)
            script.Init(mineLifetime, mineContactDamage, explosionPrefab, explosionScale);
        _mines.Add(mine);
        return mine;
    }

    /// Phase 2 drops more mines per pattern.
    private int MineCount(int baseCount)
    {
        return _phase2 ? Mathf.CeilToInt(baseCount * p2ExtraMines) : baseCount;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════════
    private void FaceDir(float xDir)
    {
        Vector3 s = transform.localScale;
        float desired = xDir < 0f ? -1f : 1f;
        if (Mathf.Sign(s.x) != desired)
            transform.localScale = new Vector3(-s.x, s.y, s.z);
    }

    private void PickNewPatrolTarget()
    {
        float x = Random.Range(-patrolRangeX, patrolRangeX);
        float y = Random.Range(-patrolRangeY, patrolRangeY);
        _patrolTarget = new Vector2(x, y);
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
        if (!_phase2 && _currentHp <= maxHp * 0.5f)
        {
            _phase2 = true;
            Debug.Log("[FishBig] Phase 2!");
        }
        if (_currentHp <= 0f) Die();
    }

    private void Die()
    {
        StopAllCoroutines();

        // Explode all active mines
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
}
