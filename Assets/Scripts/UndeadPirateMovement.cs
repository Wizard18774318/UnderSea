using UnityEngine;
[RequireComponent(typeof(Rigidbody2D))]
public class UndeadPirateMovement : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private float aggroRange   = 10f;
    [SerializeField] private float deAggroRange = 15f;

    [Header("Movement")]
    [SerializeField] private float moveSpeed       = 2.5f;
    [SerializeField] private float acceleration    = 6f;
    [SerializeField] private bool rotateToFacePlayer = true;

    [Header("Melee Attack")]
    [SerializeField] private float meleeRange        = 1.6f;
    [SerializeField] private float meleeDamage       = 4f;
    [SerializeField] private float windUpDuration    = 0.4f;
    [SerializeField] private float lungeSpeed        = 9f;
    [SerializeField] private float lungeDuration     = 0.2f;
    [SerializeField] private float recoverDuration   = 0.6f;
    [SerializeField] private float attackCooldown    = 1.5f;

    [Header("Underwater Wobble")]
    [SerializeField] private float wobbleAmplitude = 0.15f;
    [SerializeField] private float wobbleFrequency = 1.8f;

    private enum State { Idle, Chase, WindUp, Lunge, Recover }
    private State _state = State.Idle;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Transform _player;

    private Vector2 _velocity;
    private Vector2 _lungeDir;
    private float   _stateTimer;
    private float   _attackCooldownTimer;
    private float   _wobbleOffset;

    private bool _hitDealtThisLunge;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
        _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    private void Update()
    {
        if (_player == null || !_player.gameObject.activeInHierarchy)
        {
            Decelerate();
            return;
        }

        _attackCooldownTimer -= Time.deltaTime;

        switch (_state)
        {
            case State.Idle:   UpdateIdle();    break;
            case State.Chase:  UpdateChase();   break;
            case State.WindUp: UpdateWindUp();  break;
            case State.Lunge:  UpdateLunge();   break;
            case State.Recover:UpdateRecover(); break;
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _velocity;
    }
    private void UpdateIdle()
    {
        Decelerate();

        if (_player == null) return;
        if (DistToPlayer() <= aggroRange)
            EnterChase();
    }

    private void UpdateChase()
    {
        if (DistToPlayer() > deAggroRange)
        {
            _state = State.Idle;
            return;
        }
        Vector2 toPlayer = DirectionToPlayer();
        Vector2 wobble   = new Vector2(0f, Mathf.Sin(Time.time * wobbleFrequency + _wobbleOffset) * wobbleAmplitude);
        Vector2 desired  = (toPlayer + wobble).normalized * moveSpeed;
        _velocity = Vector2.MoveTowards(_velocity, desired, acceleration * Time.deltaTime);

        FacePlayer();
        if (DistToPlayer() <= meleeRange && _attackCooldownTimer <= 0f)
            EnterWindUp();
    }

    private void UpdateWindUp()
    {
        Decelerate();

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            EnterLunge();
    }

    private void UpdateLunge()
    {
        _velocity = _lungeDir * lungeSpeed;

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            EnterRecover();
    }

    private void UpdateRecover()
    {
        Decelerate();

        _stateTimer -= Time.deltaTime;
        if (_stateTimer <= 0f)
            EnterChase();
    }
    private void EnterChase()
    {
        _state = State.Chase;
    }

    private void EnterWindUp()
    {
        _state      = State.WindUp;
        _stateTimer = windUpDuration;
        _lungeDir   = DirectionToPlayer();
        _hitDealtThisLunge = false;
    }

    private void EnterLunge()
    {
        _state      = State.Lunge;
        _stateTimer = lungeDuration;
    }

    private void EnterRecover()
    {
        _state               = State.Recover;
        _stateTimer          = recoverDuration;
        _attackCooldownTimer = attackCooldown;
        _velocity            = Vector2.zero;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_state != State.Lunge || _hitDealtThisLunge) return;

        PlayerManager pm = other.GetComponent<PlayerManager>();
        if (pm != null)
        {
            _hitDealtThisLunge = true;
            PlayerStatsManager.Instance?.TakeDamage(meleeDamage);
            Debug.Log($"Boss melee hit! -{meleeDamage} hp");
        }
    }
    private float   DistToPlayer()      => Vector2.Distance(transform.position, _player.position);
    private Vector2 DirectionToPlayer() => ((Vector2)(_player.position - transform.position)).normalized;

    private void FacePlayer()
    {
        if (_sr == null) return;
        _sr.flipX = _player.position.x < transform.position.x;
    }

    private void Decelerate()
    {
        _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, acceleration * Time.deltaTime);
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, aggroRange);

        Gizmos.color = Color.grey;
        Gizmos.DrawWireSphere(transform.position, deAggroRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, meleeRange);
    }
}
