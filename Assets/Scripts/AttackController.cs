using System.Collections;
using UnityEngine;

/// <summary>
/// Poseidon Trident Boss – Attack Controller
/// ==========================================
/// Attach to the boss GameObject. Manages HP, phases, and randomly picks
/// attacks appropriate for each phase.
///
/// PHASES
///   Phase 1 (100 → 66 % HP) : MiddleSweep, WaveSurge
///   Phase 2 ( 66 → 33 % HP) : above + ForkThrow, TridentRain
///   Phase 3 ( 33 →  0 % HP) : above + SpinBarrage  (everything faster)
///
/// REQUIRED PREFABS  (assign in Inspector)
///   • Trident Prefab  – sprite + Rigidbody2D (Kinematic) + PolygonCollider2D
///                       (IsTrigger = true) + TridentScript component.
///     Place at: Assets/Prefabs/TridentProjectile.prefab
///
/// HOW TO SET UP THE BOSS GAMEOBJECT
///   1. Create a GameObject named "TridentBoss".
///   2. Add this AttackController component.
///   3. Drag TridentProjectile.prefab into the "Trident Prefab" slot.
///   4. Tune values in each Header section below.
///   5. Give the boss a Collider2D (IsTrigger = false) so player projectiles
///      tagged "Projectile" can hit it via OnTriggerEnter2D here.
/// </summary>
public class AttackController : MonoBehaviour
{
    // ── HP & Phases ───────────────────────────────────────────────────────────
    [Header("Boss HP")]
    [SerializeField] private float maxHp = 60f;

    [Header("Phase Thresholds")]
    [SerializeField] private float phase2Threshold = 0.66f; // enter phase 2 below this HP fraction
    [SerializeField] private float phase3Threshold = 0.33f; // enter phase 3 below this HP fraction

    // ── Shared Prefab ─────────────────────────────────────────────────────────
    [Header("Trident Prefab  (used by all attacks)")]
    [SerializeField] private GameObject tridentPrefab;

    // ── Attack Cooldown ───────────────────────────────────────────────────────
    [Header("Attack Timing")]
    [SerializeField] private float minCooldown = 1.8f;
    [SerializeField] private float maxCooldown = 3.2f;
    [Tooltip("In phase 3 all cooldowns are multiplied by this.")]
    [SerializeField] private float phase3CooldownMult = 0.6f;

    // ── Middle Sweep ──────────────────────────────────────────────────────────
    [Header("Middle Sweep  (flies side-to-side through centre)")]
    [SerializeField] private float sweepSpeed = 9f;
    [SerializeField] private float sweepYOffset = 0f;

    // ── Wave Surge ────────────────────────────────────────────────────────────
    [Header("Wave Surge  ('>'-shaped wedge flying right)")] 
    [SerializeField] private int   waveCount    = 5;
    [SerializeField] private float waveSpeed    = 8f;
    [SerializeField] private float waveSpread   = 4f;  // total vertical spread
    [SerializeField] private float waveXDepth   = 3f;  // how far back the wing tips are vs the centre tip

    // ── Fork Throw ────────────────────────────────────────────────────────────
    [Header("Fork Throw  (3-prong fan from boss position)")]
    [SerializeField] private float forkSpeed        = 10f;
    [SerializeField] private float forkSpreadDeg    = 30f; // half-angle of fan

    // ── Trident Rain ──────────────────────────────────────────────────────────
    [Header("Trident Rain  (tridents fall from top at random X positions)")]
    [SerializeField] private int   rainCount   = 4;
    [SerializeField] private float rainSpeed   = 11f;
    [SerializeField] private float rainDelay   = 0.22f; // seconds between each
    [SerializeField] private float rainAngleVariance = 10f; // random lean left/right

    // ── Spin Barrage (Phase 3 only) ───────────────────────────────────────────
    [Header("Spin Barrage  (Phase 3 – 8 tridents in all directions)")]
    [SerializeField] private float spinSpeed = 12f;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float _currentHp;
    private int   _phase = 1;
    private float _cooldownTimer;
    private bool  _busy;
    private Camera _cam;

    // public so other scripts (e.g. health bar) can read it
    public float HpFraction => _currentHp / maxHp;
    public int   Phase      => _phase;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _cam       = Camera.main;
        _currentHp = maxHp;
        _cooldownTimer = maxCooldown; // small grace period at start
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_busy) return;

        _cooldownTimer -= Time.deltaTime;
        if (_cooldownTimer <= 0f)
            PickAndLaunchAttack();
    }

    // ── Damage (called externally or via trigger) ─────────────────────────────
    public void TakeDamage(float amount)
    {
        _currentHp = Mathf.Max(0f, _currentHp - amount);
        CheckPhaseTransition();
        if (_currentHp <= 0f) Die();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(other.GetComponent<ProjectileMovement>()?.getDamageAmount() ?? 0f);
            Destroy(other.gameObject);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack selector
    // ─────────────────────────────────────────────────────────────────────────
    private void PickAndLaunchAttack()
    {
        // Build pool based on phase
        // Phase 1: 0,1   Phase 2: 0,1,2,3   Phase 3: 0,1,2,3,4
        int maxAttack = _phase == 1 ? 1 : _phase == 2 ? 3 : 4;
        int choice    = Random.Range(0, maxAttack + 1);

        _busy = true;
        switch (choice)
        {
            case 0: StartCoroutine(DoMiddleSweep());  break;
            case 1: StartCoroutine(DoWaveSurge());    break;
            case 2: StartCoroutine(DoForkThrow());    break;
            case 3: StartCoroutine(DoTridentRain());  break;
            case 4: StartCoroutine(DoSpinBarrage());  break;
        }
    }

    private void AttackDone()
    {
        float mult = _phase == 3 ? phase3CooldownMult : 1f;
        _cooldownTimer = Random.Range(minCooldown, maxCooldown) * mult;
        _busy = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 1 – Middle Sweep
    // Single trident flies from left → right through screen centre.
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoMiddleSweep()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float spawnX = _cam.transform.position.x - halfW - 1f;
        float midY   = _cam.transform.position.y + sweepYOffset;

        SpawnTrident(new Vector3(spawnX, midY, 0f), Vector2.right, sweepSpeed);
        yield return null;
        AttackDone();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 2 – Wave Surge
    // All tridents fly straight right but spawn at staggered X positions
    // so they form a ">" shape on screen.
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoWaveSurge()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float tipX   = _cam.transform.position.x - halfW - 1f; // centre (tip) spawn X
        float camY   = _cam.transform.position.y;

        float step      = waveSpread / Mathf.Max(1, waveCount - 1);
        float startY    = camY - waveSpread * 0.5f;
        float halfCount = (waveCount - 1) * 0.5f;

        for (int i = 0; i < waveCount; i++)
        {
            float y = startY + i * step;
            // t = 0 at centre row, 1 at wing tips → push wings further left
            float t = halfCount > 0 ? Mathf.Abs(i - halfCount) / halfCount : 0f;
            float x = tipX - t * waveXDepth;
            SpawnTrident(new Vector3(x, y, 0f), Vector2.right, waveSpeed);
        }
        yield return null;
        AttackDone();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 3 – Fork Throw
    // Three tridents burst out from boss position in a spread (like trident prongs).
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoForkThrow()
    {
        // Aim toward player if present, otherwise aim right
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 baseDir = player != null
            ? ((Vector2)(player.transform.position - transform.position)).normalized
            : Vector2.right;

        float[] angles = { -forkSpreadDeg, 0f, forkSpreadDeg };
        foreach (float deg in angles)
        {
            Vector2 dir = RotateVector(baseDir, deg * Mathf.Deg2Rad);
            SpawnTrident(transform.position, dir, forkSpeed);
        }
        yield return null;
        AttackDone();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 4 – Trident Rain
    // Tridents drop from the top of the screen at random X positions.
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoTridentRain()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float halfH  = _cam.orthographicSize;
        float spawnY = _cam.transform.position.y + halfH + 1f;
        float camX   = _cam.transform.position.x;

        for (int i = 0; i < rainCount; i++)
        {
            float x   = camX + Random.Range(-halfW * 0.8f, halfW * 0.8f);
            float lean = Random.Range(-rainAngleVariance, rainAngleVariance) * Mathf.Deg2Rad;
            Vector2 dir = RotateVector(Vector2.down, lean);
            SpawnTrident(new Vector3(x, spawnY, 0f), dir, rainSpeed);
            yield return new WaitForSeconds(rainDelay);
        }
        AttackDone();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 5 – Spin Barrage (Phase 3 only)
    // 8 tridents launched in all 8 compass directions from boss position.
    // ─────────────────────────────────────────────────────────────────────────
    private IEnumerator DoSpinBarrage()
    {
        int count = 8;
        for (int i = 0; i < count; i++)
        {
            float ang = i * (360f / count) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang));
            SpawnTrident(transform.position, dir, spinSpeed);
        }
        yield return null;
        AttackDone();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────
    private void SpawnTrident(Vector3 pos, Vector2 dir, float speed)
    {
        if (tridentPrefab == null) return;
        var obj = Instantiate(tridentPrefab, pos, Quaternion.identity);
        obj.GetComponent<TridentScript>()?.Init(dir, speed);
    }

    private static Vector2 RotateVector(Vector2 v, float radians)
    {
        float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void CheckPhaseTransition()
    {
        float f = HpFraction;
        if (_phase < 2 && f <= phase2Threshold) { _phase = 2; OnPhase2(); }
        else if (_phase < 3 && f <= phase3Threshold) { _phase = 3; OnPhase3(); }
    }

    private void OnPhase2()
    {
        Debug.Log("[TridentBoss] *** PHASE 2 ***");
        // Hook: add visual/audio feedback here (tint, shake, sound)
    }

    private void OnPhase3()
    {
        Debug.Log("[TridentBoss] *** PHASE 3 ***");
        // Hook: add visual/audio feedback here
    }

    private void Die()
    {
        Debug.Log("[TridentBoss] Defeated!");
        // Hook: play death animation, load next scene, etc.
        Destroy(gameObject);
    }
}
