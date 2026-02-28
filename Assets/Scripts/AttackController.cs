using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Poseidon Trident Boss – Attack Controller
/// ==========================================
/// Attach to the boss GameObject. Manages HP, phases, and randomly picks
/// attacks appropriate for each phase.
///
/// PHASES
///   Phase 1 (100 → 66 % HP) : Random fish swarm only (Level 1 style)
///   Phase 2 ( 66 → 33 % HP) : WaveSurge, ForkThrow, TridentRain
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

    [Header("Phase Overrides")]
    [SerializeField] private bool phase2Only = false;

    // ── Shared Prefab ─────────────────────────────────────────────────────────
    [Header("Trident Prefab  (used by all attacks)")]
    [SerializeField] private GameObject tridentPrefab;

    // ── Attack Cooldown ───────────────────────────────────────────────────────
    [Header("Attack Timing")]
    [SerializeField] private float minCooldown = 1.8f;
    [SerializeField] private float maxCooldown = 3.2f;
    [Tooltip("In phase 3 all cooldowns are multiplied by this.")]
    [SerializeField] private float phase3CooldownMult = 0.6f;

    // ── Phase 1 Replacement – Random Fish (Level 1 style) ─────────────────────
    [Header("Phase 1 – Fish Swarm")]
    [SerializeField] private bool enablePhase1Fish = true;
    [SerializeField] private GameObject phase1FishPrefab;
    [SerializeField] private float phase1SpawnInterval = 2f;
    [SerializeField] private float phase1FishSpeed = 5f;
    [SerializeField] private float phase1ScreenPadding = 1f;
    [SerializeField] private int phase1MaxFish = 3;
    [SerializeField] private float phase1FishScale = 0.5f;

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

    [Header("BossFish Minions")]
    [SerializeField] private GameObject bossFishMinionPrefab;
    [SerializeField, Range(1, 3)] private int bossFishSpawnPhase = 2;
    [SerializeField] private Vector2 bossFishSpawnOffset = new Vector2(-3f, 1.5f);
    [SerializeField] private float bossFishSpawnInterval = 18f;
    [SerializeField] private int bossFishMaxAlive = 1;

    // ── Runtime ───────────────────────────────────────────────────────────────
    private float _currentHp;
    private int   _phase = 1;
    private float _cooldownTimer;
    private bool  _busy;
    private Camera _cam;
    private float _phase1SpawnTimer;
    private readonly List<Phase1FishTracker> _phase1Fish = new List<Phase1FishTracker>();
    private float _bossFishSpawnTimer;
    private readonly List<GameObject> _activeBossFish = new List<GameObject>();

    // public so other scripts (e.g. health bar) can read it
    public float HpFraction => _currentHp / maxHp;
    public int   Phase      => _phase;

    // ─────────────────────────────────────────────────────────────────────────
    private void Awake()
    {
        _cam = Camera.main;

        // Apply mouse-aiming HP multiplier from global settings
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            maxHp *= GameSettings.Instance.mouseAimingHpMultiplier;

        _currentHp     = maxHp;
        _cooldownTimer = maxCooldown;
        _phase1SpawnTimer = Mathf.Max(0.1f, phase1SpawnInterval);
        phase1MaxFish = Mathf.Max(1, phase1MaxFish);
        phase1FishScale = Mathf.Max(0.01f, phase1FishScale);
        bossFishSpawnInterval = Mathf.Max(0.1f, bossFishSpawnInterval);
        bossFishMaxAlive = Mathf.Max(0, bossFishMaxAlive);
        bossFishSpawnPhase = Mathf.Clamp(bossFishSpawnPhase, 1, 3);
        _bossFishSpawnTimer = bossFishSpawnInterval;

        if (phase2Only)
        {
            _phase = 2;
            OnPhase2();
        }
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        HandleBossFishMinions();
        if (TryHandlePhase1Fish()) return;
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
        int maxAttack = _phase >= 3 ? 3 : (_phase >= 2 ? 2 : -1);
        if (maxAttack < 0)
        {
            _cooldownTimer = maxCooldown;
            return;
        }

        int choice = Random.Range(0, maxAttack + 1);

        _busy = true;
        switch (choice)
        {
            case 0: StartCoroutine(DoWaveSurge());    break;
            case 1: StartCoroutine(DoForkThrow());    break;
            case 2: StartCoroutine(DoTridentRain());  break;
            case 3: StartCoroutine(DoSpinBarrage());  break;
        }
    }

    private void AttackDone()
    {
        float mult = _phase == 3 ? phase3CooldownMult : 1f;
        _cooldownTimer = Random.Range(minCooldown, maxCooldown) * mult;
        _busy = false;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Attack 1 – Wave Surge
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
    // Attack 2 – Fork Throw
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
    // Attack 3 – Trident Rain
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
    // Attack 4 – Spin Barrage (Phase 3 only)
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

    private bool TryHandlePhase1Fish()
    {
        if (_phase != 1) return false;
        if (!enablePhase1Fish) return false;
        if (phase1FishPrefab == null) return false;
        if (_cam == null) return false;

        if (_phase1Fish.Count >= phase1MaxFish)
            return true;

        _phase1SpawnTimer -= Time.deltaTime;
        if (_phase1SpawnTimer <= 0f)
        {
            SpawnPhase1FishInternal();
            _phase1SpawnTimer = Mathf.Max(0.1f, phase1SpawnInterval);
        }
        return true;
    }

    private void HandleBossFishMinions()
    {
        if (bossFishMinionPrefab == null) return;
        if (bossFishMaxAlive <= 0) return;
        if (_phase < bossFishSpawnPhase) return;

        for (int i = _activeBossFish.Count - 1; i >= 0; i--)
        {
            if (_activeBossFish[i] == null)
                _activeBossFish.RemoveAt(i);
        }

        if (_activeBossFish.Count >= bossFishMaxAlive)
            return;

        _bossFishSpawnTimer -= Time.deltaTime;
        if (_bossFishSpawnTimer > 0f) return;

        Vector3 spawnPos = transform.position + (Vector3)bossFishSpawnOffset;
        GameObject minion = Instantiate(bossFishMinionPrefab, spawnPos, Quaternion.identity);
        _activeBossFish.Add(minion);
        _bossFishSpawnTimer = bossFishSpawnInterval;
    }

    private bool SpawnPhase1FishInternal()
    {
        if (_phase1Fish.Count >= phase1MaxFish) return false;
        float halfH = _cam.orthographicSize;
        float halfW = halfH * _cam.aspect;
        Vector3 camPos = _cam.transform.position;

        int side = Random.Range(0, 4);
        Vector2 spawnPos = Vector2.zero;
        Vector2 targetPos = Vector2.zero;

        switch (side)
        {
            case 0:
                spawnPos  = new Vector2(camPos.x - halfW - phase1ScreenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                targetPos = new Vector2(camPos.x + halfW + phase1ScreenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 1:
                spawnPos  = new Vector2(camPos.x + halfW + phase1ScreenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                targetPos = new Vector2(camPos.x - halfW - phase1ScreenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 2:
                spawnPos  = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH - phase1ScreenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH + phase1ScreenPadding);
                break;
            default:
                spawnPos  = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH + phase1ScreenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH - phase1ScreenPadding);
                break;
        }

        Vector2 dir = (targetPos - spawnPos).normalized;
        GameObject fish = Instantiate(phase1FishPrefab, new Vector3(spawnPos.x, spawnPos.y, 0f), Quaternion.identity);

        Vector3 baseScale = fish.transform.localScale;
        float scaleFactor = Mathf.Max(0.01f, phase1FishScale);
        Vector3 scaled = new Vector3(
            Mathf.Sign(Mathf.Approximately(baseScale.x, 0f) ? 1f : baseScale.x) * Mathf.Abs(baseScale.x) * scaleFactor,
            baseScale.y * scaleFactor,
            baseScale.z == 0f ? 1f : baseScale.z);
        fish.transform.localScale = scaled;
        FishMover mover = fish.GetComponent<FishMover>();
        if (mover != null)
            mover.Init(dir, phase1FishSpeed);

        Phase1FishTracker tracker = fish.GetComponent<Phase1FishTracker>();
        if (tracker == null)
            tracker = fish.AddComponent<Phase1FishTracker>();
        tracker.Init(this, baseScale, scaleFactor);
        _phase1Fish.Add(tracker);
        return true;
    }

    internal void NotifyPhase1FishDestroyed(Phase1FishTracker tracker)
    {
        if (tracker == null) return;
        if (_phase1Fish.Remove(tracker) && _phase == 1 && enablePhase1Fish)
            _phase1SpawnTimer = 0f;
    }

    private static Vector2 RotateVector(Vector2 v, float radians)
    {
        float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void CheckPhaseTransition()
    {
        if (phase2Only) return;
        float f = HpFraction;
        if (_phase < 2 && f <= phase2Threshold) { _phase = 2; OnPhase2(); }
        else if (_phase < 3 && f <= phase3Threshold) { _phase = 3; OnPhase3(); }
    }

    private void OnPhase2()
    {
        Debug.Log("[TridentBoss] *** PHASE 2 ***");
        // Hook: add visual/audio feedback here (tint, shake, sound)
        _bossFishSpawnTimer = 0f;
    }

    private void OnPhase3()
    {
        Debug.Log("[TridentBoss] *** PHASE 3 ***");
        // Hook: add visual/audio feedback here
        if (bossFishSpawnPhase >= 3)
            _bossFishSpawnTimer = 0f;
    }

    private void Die()
    {
        Debug.Log("[TridentBoss] Defeated!");
        // Hook: play death animation, load next scene, etc.
        foreach (GameObject fish in _activeBossFish)
        {
            if (fish != null)
                Destroy(fish);
        }
        _activeBossFish.Clear();
        Destroy(gameObject);
    }
}

[DisallowMultipleComponent]
public class Phase1FishTracker : MonoBehaviour
{
    private AttackController _owner;
    private Vector3 _baseScale = Vector3.one;
    private float _scaleFactor = 1f;

    public void Init(AttackController owner, Vector3 baseScale, float scaleFactor)
    {
        _owner       = owner;
        _baseScale   = new Vector3(
            Mathf.Abs(baseScale.x) < 0.0001f ? 1f : Mathf.Abs(baseScale.x),
            Mathf.Abs(baseScale.y) < 0.0001f ? 1f : Mathf.Abs(baseScale.y),
            baseScale.z == 0f ? 1f : baseScale.z);
        _scaleFactor = Mathf.Max(0.01f, scaleFactor);
        ApplyScale();
    }

    private void LateUpdate()
    {
        ApplyScale();
    }

    private void ApplyScale()
    {
        float sign = Mathf.Sign(transform.localScale.x);
        if (Mathf.Approximately(sign, 0f)) sign = 1f;

        Vector3 target = new Vector3(
            sign * _baseScale.x * _scaleFactor,
            _baseScale.y * _scaleFactor,
            _baseScale.z);

        if (transform.localScale != target)
            transform.localScale = target;
    }

    private void OnDestroy()
    {
        if (_owner == null || !_owner.isActiveAndEnabled) return;
        _owner.NotifyPhase1FishDestroyed(this);
    }
}
