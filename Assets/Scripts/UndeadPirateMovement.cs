using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class UndeadPirateMovement : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float aggroRange   = 10f;
    [SerializeField] private float deAggroRange = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 2.5f;
    [SerializeField] private float acceleration = 6f;

    [Header("Underwater Wobble")]
    [SerializeField] private float wobbleAmplitude = 0.15f;
    [SerializeField] private float wobbleFrequency = 1.8f;

    // ── Phase 1 ──────────────────────────────────────────────────────────────
    [Header("Phase 1 - Lunge")]
    [SerializeField] private float p1MeleeRange      = 1.8f;
    [SerializeField] private float p1MeleeDamage     = 4f;
    [SerializeField] private float p1AttackCooldown  = 2.0f;
    [SerializeField] private float p1WindUpDuration  = 0.4f;
    [SerializeField] private float p1LungeSpeed      = 9f;
    [SerializeField] private float p1LungeDuration   = 0.3f;
    [SerializeField] private float p1RecoverDuration = 0.6f;

    // ── Phase 2 ──────────────────────────────────────────────────────────────
    [Header("Phase 2 - Speed & Scale")]
    [SerializeField] private float p2ScaleMultiplier  = 1.3f;
    [SerializeField] private float p2SpeedMultiplier  = 1.5f;
    [SerializeField] private float p2LungeSpeedMult   = 1.4f;
    [SerializeField] private float p2WindUpDuration   = 0.25f;
    [SerializeField] private float p2RecoverDuration  = 0.25f;

    [Header("Phase 2 - Lunge Burst")]
    [SerializeField] private int p2LungeMinCount = 2;
    [SerializeField] private int p2LungeMaxCount = 5;

    [Header("Phase 2 - Formation Attack")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private int   p2FormMinRepeats      = 2;
    [SerializeField] private int   p2FormMaxRepeats      = 4;
    [SerializeField] private float p2FormEdgePadding     = 0.5f;
    [SerializeField] private float p2FormAimDuration     = 0.8f;
    [SerializeField] private float p2FormSwordSpeed      = 14f;
    [SerializeField] private int   p2FormSwordMinCount   = 7;
    [SerializeField] private int   p2FormSwordMaxCount   = 9;
    [SerializeField] private float p2FormSwordYNoise     = 0.4f;  // per-sword random Y nudge for dodgeability

    // ── Phase 3 ──────────────────────────────────────────────────────────────
    [Header("Phase 3 - Scale & Speed")]
    [SerializeField] private float p3AdditionalScaleMult = 1.4f;
    [SerializeField] private float p3SpeedMult           = 1.2f;

    [Header("Phase 3 - Orbiting Swords")]
    [SerializeField] private GameObject orbitingSwordPrefab;
    [SerializeField] private int p3OrbitCount = 4;

    // ── Internal state ────────────────────────────────────────────────────────
    private enum State { Idle, Chase, WindUp, Lunge, Recover, FormMove, FormAim }
    private State _state = State.Idle;
    private int   _phase = 1;

    private Rigidbody2D          _rb;
    private SpriteRenderer       _sr;
    private Animator             _animator;
    private Camera               _cam;
    private Transform            _player;
    private UndeadPirateManager  _manager; // phase callbacks

    private Vector2 _velocity;
    private Vector2 _lungeDir;
    private float   _stateTimer;
    private float   _attackCooldownTimer;
    private float   _wobbleOffset;
    private bool    _hitDealtThisLunge;

    // Phase 2+ burst/formation tracking
    private int  _lungeCount;
    private int  _lungeTarget;
    private int  _formCount;
    private int  _formTarget;
    private bool _formGoingRight;

    // Cached per-phase stats
    private float _curMoveSpeed;
    private float _curLungeSpeed;
    private float _curWindUp;
    private float _curRecover;

    // ── Lifecycle ─────────────────────────────────────────────────────────────
    private void Awake()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _sr           = GetComponentInChildren<SpriteRenderer>();
        _animator     = GetComponentInChildren<Animator>();
        _manager      = GetComponent<UndeadPirateManager>();
        _cam          = Camera.main;
        _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
        ApplyPhaseStats();
    }

    private void Start()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;
    }

    // ── Phase entry (called by Manager) ──────────────────────────────────────
    public void EnterPhase2()
    {
        if (_phase >= 2) return;
        _phase = 2;
        transform.localScale *= p2ScaleMultiplier;
        ApplyPhaseStats();
        NewLungeBurst();
        EnterChase();
    }

    public void EnterPhase3()
    {
        if (_phase >= 3) return;
        _phase = 3;
        transform.localScale *= p3AdditionalScaleMult;
        ApplyPhaseStats();
        SpawnOrbitingSwords();
    }

    private void ApplyPhaseStats()
    {
        float sm = 1f, lm = 1f;
        if (_phase >= 2) { sm *= p2SpeedMultiplier; lm *= p2LungeSpeedMult; }
        if (_phase >= 3) { sm *= p3SpeedMult; }
        _curMoveSpeed  = moveSpeed    * sm;
        _curLungeSpeed = p1LungeSpeed * lm;
        _curWindUp     = _phase >= 2 ? p2WindUpDuration  : p1WindUpDuration;
        _curRecover    = _phase >= 2 ? p2RecoverDuration : p1RecoverDuration;
    }

    // ── Update ────────────────────────────────────────────────────────────────
    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_player == null || !_player.gameObject.activeInHierarchy)
        {
            Decelerate(); return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:     UpdateIdle();     break;
            case State.Chase:    UpdateChase();    break;
            case State.WindUp:   UpdateWindUp();   break;
            case State.Lunge:    UpdateLunge();    break;
            case State.Recover:  UpdateRecover();  break;
            case State.FormMove: UpdateFormMove(); break;
            case State.FormAim:  UpdateFormAim();  break;
        }
    }

    private void FixedUpdate() => _rb.linearVelocity = _velocity;

    // ── State updates ─────────────────────────────────────────────────────────
    private void UpdateIdle()
    {
        Decelerate();
        if (_player == null) return;
        if (DistToPlayer() <= aggroRange) EnterChase();
        else PlayAnim("idle_1");
    }

    private void UpdateChase()
    {
        if (DistToPlayer() > deAggroRange) { _state = State.Idle; return; }

        Vector2 toPlayer = DirectionToPlayer();
        Vector2 wobble   = new Vector2(0f, Mathf.Sin(Time.time * wobbleFrequency + _wobbleOffset) * wobbleAmplitude);
        Vector2 desired  = (toPlayer + wobble).normalized * _curMoveSpeed;
        _velocity = Vector2.MoveTowards(_velocity, desired, acceleration * Time.deltaTime);

        FacePlayer();
        if (DistToPlayer() <= p1MeleeRange && _attackCooldownTimer <= 0f)
            EnterWindUp();
    }

    private void UpdateWindUp()
    {
        Decelerate();
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f) EnterLunge();
    }

    private void UpdateLunge()
    {
        _velocity = _lungeDir * _curLungeSpeed;
        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f) EnterRecover();
    }

    private void UpdateRecover()
    {
        Decelerate();
        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f) return;

        if (_phase >= 2)
        {
            _lungeCount++;
            if (_lungeCount >= _lungeTarget)
                EnterFormMove();
            else
                EnterChase();
        }
        else
        {
            EnterChase();
        }
    }

    private void UpdateFormMove()
    {
        float halfW   = _cam.orthographicSize * _cam.aspect;
        float camX    = _cam.transform.position.x;
        float targetX = _formGoingRight
            ? camX - halfW - p2FormEdgePadding
            : camX + halfW + p2FormEdgePadding;
        Vector2 target = new Vector2(targetX, _cam.transform.position.y);

        MoveToward(target, _curMoveSpeed * 1.5f);
        if (Mathf.Abs(_velocity.x) > 0.1f) SetFacing(_velocity.x > 0);

        if (Vector2.Distance(transform.position, target) < 0.3f)
        {
            transform.position = target;
            EnterFormAim();
        }
    }

    private void UpdateFormAim()
    {
        Decelerate();
        _stateTimer -= Time.deltaTime;
        if (_stateTimer > 0f) return;

        FireFormation();
        _formCount++;

        if (_formCount >= _formTarget)
        {
            _manager?.SetDamageMultiplier(1f);
            NewLungeBurst();
            EnterChase();
        }
        else
        {
            // 40% chance to flip side for next volley
            if (Random.value < 0.4f) _formGoingRight = !_formGoingRight;
            _stateTimer = p2FormAimDuration + Random.Range(-0.1f, 0.2f);
        }
    }

    // ── State transitions ─────────────────────────────────────────────────────
    private void EnterChase()
    {
        _state = State.Chase;
        PlayAnim("run");
    }

    private void EnterWindUp()
    {
        _state             = State.WindUp;
        _stateTimer        = _curWindUp + Random.Range(-0.05f, 0.1f);
        _lungeDir          = DirectionToPlayer();
        _hitDealtThisLunge = false;
        PlayAnim("skill_1");
    }

    private void EnterLunge()
    {
        _state      = State.Lunge;
        _stateTimer = p1LungeDuration + Random.Range(-0.05f, 0.05f);
        PlayAnim("run");
    }

    private void EnterRecover()
    {
        _state               = State.Recover;
        _stateTimer          = _curRecover + Random.Range(0f, 0.15f);
        _attackCooldownTimer = _phase >= 2 ? p1AttackCooldown * 0.5f : p1AttackCooldown;
        _velocity            = Vector2.zero;
        PlayAnim("hit_1");
    }

    private void EnterFormMove()
    {
        _formGoingRight = Random.value > 0.5f;
        _state = State.FormMove;
        PlayAnim("run");
    }

    private void EnterFormAim()
    {
        _stateTimer = p2FormAimDuration + Random.Range(-0.1f, 0.2f);
        _velocity   = Vector2.zero;
        _manager?.SetDamageMultiplier(0.5f);
        _state = State.FormAim;
        PlayAnim("skill_2");
    }

    // ── Formation helpers ─────────────────────────────────────────────────────
    private void NewLungeBurst()
    {
        _lungeCount  = 0;
        _lungeTarget = Random.Range(p2LungeMinCount, p2LungeMaxCount + 1);
        _formCount   = 0;
        _formTarget  = Random.Range(p2FormMinRepeats, p2FormMaxRepeats + 1);
    }

    private void FireFormation()
    {
        if (swordPrefab == null) return;

        int count     = Random.Range(p2FormSwordMinCount, p2FormSwordMaxCount + 1);
        float halfH   = _cam.orthographicSize;
        float camY    = _cam.transform.position.y;
        float camX    = _cam.transform.position.x;
        float halfW   = halfH * _cam.aspect;

        // Fire toward the far side of the screen
        float dir     = _formGoingRight ? 1f : -1f;
        float spawnX  = _formGoingRight
            ? camX - halfW - p2FormEdgePadding
            : camX + halfW + p2FormEdgePadding;

        // Spread Y positions evenly across screen height with per-sword noise
        float step = (halfH * 2f) / (count - 1);
        for (int i = 0; i < count; i++)
        {
            float y = (camY - halfH) + i * step + Random.Range(-p2FormSwordYNoise, p2FormSwordYNoise);
            var obj   = Instantiate(swordPrefab, new Vector3(spawnX, y, 0f), Quaternion.identity);
            var sword = obj.GetComponent<PirateSwordProjectile>();
            sword?.Launch(new Vector2(dir, 0f), p2FormSwordSpeed);
        }
    }

    // ── Phase 3 helpers ───────────────────────────────────────────────────────
    private void SpawnOrbitingSwords()
    {
        if (orbitingSwordPrefab == null) return;
        float step = 360f / p3OrbitCount;
        for (int i = 0; i < p3OrbitCount; i++)
        {
            var obj   = Instantiate(orbitingSwordPrefab, transform.position, Quaternion.identity);
            obj.transform.SetParent(transform);   // child → auto-destroys with boss
            var sword = obj.GetComponent<PirateOrbitingSword>();
            sword?.Init(i * step);
        }
    }

    // ── Trigger ───────────────────────────────────────────────────────────────
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != State.Lunge || _hitDealtThisLunge) return;
        PlayerManager pm = other.GetComponent<PlayerManager>();
        if (pm != null && pm.TakeMeleeDamage(p1MeleeDamage))
        {
            _hitDealtThisLunge = true;
            Debug.Log($"[Pirate] Melee hit! -{p1MeleeDamage} HP");
        }
    }

    // ── Utilities ─────────────────────────────────────────────────────────────
    private float   DistToPlayer()      => Vector2.Distance(transform.position, _player.position);
    private Vector2 DirectionToPlayer() => ((Vector2)(_player.position - transform.position)).normalized;

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        _velocity   = Vector2.MoveTowards(_velocity, dir * speed, acceleration * Time.deltaTime);
    }

    private void FacePlayer()  { if (_sr != null) _sr.flipX = _player.position.x < transform.position.x; }
    private void SetFacing(bool faceLeft) { if (_sr != null) _sr.flipX = faceLeft; }
    private void Decelerate()  { _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, acceleration * Time.deltaTime); }

    private static Vector2 RotateVector(Vector2 v, float radians)
    {
        float cos = Mathf.Cos(radians), sin = Mathf.Sin(radians);
        return new Vector2(cos * v.x - sin * v.y, sin * v.x + cos * v.y);
    }

    private void PlayAnim(string trigger)
    {
        if (_animator == null) return;
        _animator.ResetTrigger("idle_1");
        _animator.ResetTrigger("run");
        _animator.ResetTrigger("walk");
        _animator.ResetTrigger("skill_1");
        _animator.ResetTrigger("skill_2");
        _animator.ResetTrigger("hit_1");
        _animator.SetTrigger(trigger);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);
        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, deAggroRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, p1MeleeRange);
    }
}
