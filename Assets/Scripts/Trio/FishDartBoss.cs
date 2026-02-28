using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 2 — FishDart: 3 attack patterns, always invisible between attacks.
///
/// ATTACKS (cycle: CircleStrike → Ricochet → ShadowRush)
///   CircleStrike — teleports off-screen, orbits the player spiralling inward, then darts in
///   Ricochet     — enters from the side, slams a wall, bounces toward the player
///   ShadowRush   — enters from off-screen with speed, leaves fading afterimage clones
///
/// Phase 2 (< 50 % HP): faster, shorter pause, tighter orbit, quicker bounce.
/// </summary>
public class FishDartBoss : MonoBehaviour
{
    // ── HP ─────────────────────────────────────────────────────────────────────
    [Header("Health")]
    [SerializeField] private float maxHp = 40f;

    // ── Speeds & Timing ────────────────────────────────────────────────────────
    [Header("Speeds & Timing")]
    [SerializeField] private float lungeSpeed        = 18f;
    [SerializeField] private float pauseAfterAttack  = 1.2f;
    [SerializeField] private float offScreenDist     = 14f;

    [Header("Phase 2 Modifiers")]
    [SerializeField] private float p2SpeedMult       = 1.4f;
    [SerializeField] private float p2PauseMult       = 0.6f;

    [Header("Chain / Repeat Chances")]
    [Tooltip("Chance (0-1) to immediately do a bonus extra attack after the first one")]
    [SerializeField] private float chainChance       = 0.35f;
    [Tooltip("Chance (0-1) that the bonus attack is the same as the one just done (vs random)")]
    [SerializeField] private float repeatSameChance  = 0.5f;
    [Tooltip("Pause between chained attacks (shorter than the normal pause)")]
    [SerializeField] private float chainPause        = 0.35f;
    [Tooltip("In phase 2, chain chance is multiplied by this")]
    [SerializeField] private float p2ChainMult       = 1.6f;

    // ── Circle & Strike ────────────────────────────────────────────────────────
    [Header("Circle & Strike")]
    [SerializeField] private float orbitRadius       = 10f;
    [SerializeField] private float orbitSpeed        = 180f;   // deg/sec — rotation rate while circling
    [SerializeField] private float orbitShrinkRate   = 1.5f;   // radius lost per sec
    [SerializeField] private float strikeMinRadius   = 1.5f;
    [Tooltip("Speed of the inward dart after orbiting (independent of lungeSpeed)")]
    [SerializeField] private float strikeSpeed       = 22f;

    // ── Ricochet ───────────────────────────────────────────────────────────────
    [Header("Ricochet")]
    [SerializeField] private float bounceWallY       = 6f;

    // ── Shadow Rush ────────────────────────────────────────────────────────────
    [Header("Shadow Rush")]
    [SerializeField] private float shadowInterval    = 0.06f;
    [SerializeField] private float shadowFadeTime    = 0.4f;

    // ── Death ──────────────────────────────────────────────────────────────────
    [Header("Death Effect")]
    [SerializeField] private GameObject deathEffectPrefab;
    [SerializeField] private float deathEffectScale  = 15f;

    // ── Runtime ────────────────────────────────────────────────────────────────
    private float _currentHp;
    private bool  _phase2;
    private Transform _player;
    private SpriteRenderer   _sr;
    private SpriteRenderer[] _allRenderers;
    private int _attackIndex;

    private enum Attack { CircleStrike, Ricochet, ShadowRush }
    private static readonly Attack[] _order = { Attack.CircleStrike, Attack.Ricochet, Attack.ShadowRush };

    // ── Init ───────────────────────────────────────────────────────────────────
    // Awake runs in the same frame as Instantiate, before any rendering — guaranteed invisible.
    private void Awake()
    {
        _sr           = GetComponentInChildren<SpriteRenderer>();
        _allRenderers = GetComponentsInChildren<SpriteRenderer>();
        SetVisible(false);

        var enemy = GetComponent<Enemy>();
        if (enemy != null) enemy.enabled = false;
    }

    private void Start()
    {
        if (GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
            maxHp *= GameSettings.Instance.mouseAimingHpMultiplier;

        _currentHp = maxHp;
        _player    = GameObject.FindGameObjectWithTag("Player")?.transform;

        StartCoroutine(AttackLoop());
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  MAIN LOOP
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator AttackLoop()
    {
        yield return new WaitForSeconds(0.5f);

        while (_currentHp > 0f)
        {
            // Pick next attack from the fixed rotation
            Attack atk = _order[_attackIndex % _order.Length];
            _attackIndex++;

            yield return ExecuteAttack(atk);
            SetVisible(false);

            // Roll for chain: keep attacking with a shorter pause each time
            float currentChainChance = chainChance * (_phase2 ? p2ChainMult : 1f);
            while (_currentHp > 0f && Random.value < currentChainChance)
            {
                yield return new WaitForSeconds(chainPause);
                if (_currentHp <= 0f) break;

                // Repeat same attack OR pick a random different one
                Attack bonus;
                if (Random.value < repeatSameChance)
                {
                    bonus = atk;  // same attack again
                }
                else
                {
                    // pick any attack except the last one played
                    Attack[] others = System.Array.FindAll(_order, a => a != atk);
                    bonus = others[Random.Range(0, others.Length)];
                }

                atk = bonus;   // update so repeat-same keeps chaining the new one
                yield return ExecuteAttack(bonus);
                SetVisible(false);

                // Reduce chain chance slightly each link so it doesn't go forever
                currentChainChance *= 0.6f;
            }

            float pause = _phase2 ? pauseAfterAttack * p2PauseMult : pauseAfterAttack;
            yield return new WaitForSeconds(pause);
        }
    }

    private IEnumerator ExecuteAttack(Attack atk)
    {
        switch (atk)
        {
            case Attack.CircleStrike: yield return DoCircleStrike(); break;
            case Attack.Ricochet:     yield return DoRicochet();     break;
            case Attack.ShadowRush:   yield return DoShadowRush();   break;
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  CIRCLE & STRIKE
    //  Teleports to orbit radius off-screen, circles spiralling inward, then darts
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoCircleStrike()
    {
        if (_player == null) { yield return DoShadowRush(); yield break; }

        float radius = orbitRadius;
        // Start slightly off the edge so it slides into view
        float angle = Random.Range(0f, 360f);
        float startRad = angle * Mathf.Deg2Rad;
        transform.position = (Vector3)((Vector2)_player.position +
                              new Vector2(Mathf.Cos(startRad), Mathf.Sin(startRad)) * radius);

        SetVisible(true);

        // Orbit spiralling inward
        while (radius > strikeMinRadius && _currentHp > 0f)
        {
            if (_player == null) break;

            angle += orbitSpeed * (_phase2 ? 1.3f : 1f) * Time.deltaTime;
            radius -= orbitShrinkRate * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            Vector2 offset = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * radius;
            transform.position = (Vector3)((Vector2)_player.position + offset);

            FaceToward(_player.position);
            yield return null;
        }

        // Strike inward past the player
        if (_player != null)
        {
            Vector2 dir = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            FaceDir(dir);
            float spd = (_phase2 ? strikeSpeed * p2SpeedMult : strikeSpeed);
            yield return DashTo((Vector2)transform.position + dir * offScreenDist, spd);
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  RICOCHET
    //  Enters from off-screen side, angles to a wall, bounces toward the player
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoRicochet()
    {
        bool fromLeft = Random.value > 0.5f;
        bool goUp     = Random.value > 0.5f;

        // Start fully off-screen horizontally
        float startX = fromLeft ? -offScreenDist : offScreenDist;
        float startY = PlayerY() + Random.Range(-2f, 2f);
        float wallY  = goUp ? bounceWallY : -bounceWallY;

        transform.position = new Vector3(startX, startY, 0f);
        SetVisible(false);  // Remain hidden until we reach the wall edge

        // Move to the wall — diagonal so it enters the screen then hits
        Vector2 toWall = (new Vector2(0f, wallY) - new Vector2(startX, startY)).normalized;
        FaceDir(toWall);
        float speed = Speed();

        bool hitWall = false;
        while (!hitWall)
        {
            transform.position += (Vector3)(toWall * speed * Time.deltaTime);

            // Become visible once inside screen bounds
            float px = Mathf.Abs(transform.position.x);
            if (!(_sr != null && _sr.enabled) && px < offScreenDist * 0.6f)
                SetVisible(true);

            if ((goUp  && transform.position.y >= wallY) ||
                (!goUp && transform.position.y <= -bounceWallY))
                hitWall = true;

            yield return null;
        }

        // Bounce in a random direction that points away from the wall we just hit
        {
            // Random angle biased away from the wall (±70° from horizontal)
            float spread = 70f * Mathf.Deg2Rad;
            float baseAngle = goUp ? -Mathf.PI * 0.5f : Mathf.PI * 0.5f; // away from wall
            float randomAngle = baseAngle + Random.Range(-spread, spread);
            Vector2 bounceDir = new Vector2(Mathf.Cos(randomAngle), Mathf.Sin(randomAngle)).normalized;
            FaceDir(bounceDir);
            yield return DashTo((Vector2)transform.position + bounceDir * offScreenDist * 1.8f,
                                 speed * (_phase2 ? 1.4f : 1.2f));
        }
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  SHADOW RUSH
    //  Off-screen → very fast horizontal dash with fading afterimage clones
    // ══════════════════════════════════════════════════════════════════════════
    private IEnumerator DoShadowRush()
    {
        bool fromLeft = Random.value > 0.5f;
        float startX  = fromLeft ? -offScreenDist : offScreenDist;
        float endX    = fromLeft ?  offScreenDist : -offScreenDist;
        float targetY = PlayerY();

        // Teleport off-screen while invisible
        transform.position = new Vector3(startX, targetY, 0f);
        FaceDir(fromLeft ? Vector2.right : Vector2.left);
        SetVisible(true);

        Vector2 dir   = fromLeft ? Vector2.right : Vector2.left;
        float speed   = Speed() * 1.3f;
        float shadowT = 0f;
        Vector3 end   = new Vector3(endX, targetY, 0f);

        while (Vector2.Distance(transform.position, end) > 0.5f && _currentHp > 0f)
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);

            shadowT -= Time.deltaTime;
            if (shadowT <= 0f) { shadowT = shadowInterval; SpawnAfterimage(); }
            yield return null;
        }
    }

    private void SpawnAfterimage()
    {
        if (_sr == null || !_sr.enabled) return;

        GameObject shadow = new GameObject("Afterimage");
        shadow.transform.position   = transform.position;
        shadow.transform.localScale = transform.localScale;

        SpriteRenderer sr = shadow.AddComponent<SpriteRenderer>();
        sr.sprite          = _sr.sprite;
        sr.color           = new Color(0.3f, 0.5f, 1f, 0.55f);
        sr.sortingLayerID  = _sr.sortingLayerID;
        sr.sortingOrder    = _sr.sortingOrder - 1;
        // Mirror scale-based flip so afterimage faces the same direction
        sr.flipX = transform.localScale.x < 0;

        shadow.AddComponent<AfterimageHasFader>().Init(shadowFadeTime);
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  HELPERS
    // ══════════════════════════════════════════════════════════════════════════
    private float Speed()  => _phase2 ? lungeSpeed * p2SpeedMult : lungeSpeed;
    private float PlayerY() => _player != null ? _player.position.y : 0f;

    /// Move along a straight line until within 0.4 units of target.
    private IEnumerator DashTo(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        while (Vector2.Distance(transform.position, target) > 0.4f && _currentHp > 0f)
        {
            transform.position += (Vector3)(dir * speed * Time.deltaTime);
            yield return null;
        }
    }

    /// Face toward a world-space point.
    /// Uses localScale.x sign — matches the Underwater Diving prefab convention.
    private void FaceToward(Vector2 target)
    {
        Vector3 s = transform.localScale;
        bool faceLeft = target.x < transform.position.x;
        float desiredSign = faceLeft ? -1f : 1f;
        if (Mathf.Sign(s.x) != desiredSign)
            transform.localScale = new Vector3(-s.x, s.y, s.z);
    }

    /// Face along a movement direction vector.
    private void FaceDir(Vector2 dir)
    {
        if (dir.x == 0f) return;
        Vector3 s = transform.localScale;
        float desiredSign = dir.x < 0f ? -1f : 1f;
        if (Mathf.Sign(s.x) != desiredSign)
            transform.localScale = new Vector3(-s.x, s.y, s.z);
    }

    private void SetVisible(bool v)
    {
        if (_allRenderers != null)
            foreach (var r in _allRenderers) if (r != null) r.enabled = v;
        else if (_sr != null)
            _sr.enabled = v;
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
            Debug.Log("[FishDart] Phase 2!");
        }
        if (_currentHp <= 0f) Die();
    }

    private void Die()
    {
        StopAllCoroutines();
        if (deathEffectPrefab != null)
        {
            var fx = Instantiate(deathEffectPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = new Vector3(deathEffectScale, deathEffectScale, 1f);
        }
        Debug.Log("[FishDart] Defeated!");
        Destroy(gameObject);
    }
}

// ─── Fade-out helper for afterimage ghosts ────────────────────────────────────
public class AfterimageHasFader : MonoBehaviour
{
    private float _duration;
    private float _timer;
    private SpriteRenderer _sr;

    public void Init(float duration)
    {
        _duration = _timer = duration;
        _sr = GetComponent<SpriteRenderer>();
    }

    private void Update()
    {
        _timer -= Time.deltaTime;
        if (_timer <= 0f) { Destroy(gameObject); return; }
        if (_sr != null) { Color c = _sr.color; c.a = _timer / _duration; _sr.color = c; }
    }
}
