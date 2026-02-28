using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossFishMovement : MonoBehaviour
{
    [Header("Pattern 1 - Patrol")]
    [SerializeField] private float patrolSpeed       = 3f;
    [SerializeField] private float patrolEdgePadding = 3f;
    [SerializeField] private float patrolDuration    = 6f;

    [Header("Pattern 2 - Sweep")]
    [SerializeField] private float sweepMoveSpeed    = 5f;
    [SerializeField] private float sweepSpeed        = 14f;
    [SerializeField] private float sweepEdgePadding  = 0.5f;
    [SerializeField] private float sweepYOffset      = -0.5f;
    [SerializeField] private float aboveScreenOffset = 1.5f;
    [SerializeField] private float fishDropInterval  = 0.35f;
    [SerializeField] private GameObject droppedFishPrefab;
    [SerializeField] private bool spawnEnabled       = true;

    [Header("Wobble")]
    [SerializeField] private float wobbleAmplitude = 0.1f;
    [SerializeField] private float wobbleFrequency = 2f;

    private enum State { Patrol, MoveToTop, MoveToSweepStart, Sweep, ReturnToPatrol }
    private State _state = State.Patrol;

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Camera _cam;

    private Vector2 _velocity;
    private float   _patrolTimer;
    private float   _dropTimer;
    private float   _wobbleOffset;
    private int     _patrolDir = 1;
    private bool    _sweepGoingRight;

    private void Awake()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _sr           = GetComponentInChildren<SpriteRenderer>();
        _cam          = Camera.main;
        _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
        _patrolTimer  = patrolDuration;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Patrol:          UpdatePatrol();         break;
            case State.MoveToTop:       UpdateMoveToTop();      break;
            case State.MoveToSweepStart:UpdateMoveToSweepStart(); break;
            case State.Sweep:           UpdateSweep();          break;
            case State.ReturnToPatrol:  UpdateReturnToPatrol(); break;
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _velocity;
    }

    private void UpdatePatrol()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        float camX  = _cam.transform.position.x;
        float leftEdge  = camX - halfW + patrolEdgePadding;
        float rightEdge = camX + halfW - patrolEdgePadding;

        if (_patrolDir > 0 && transform.position.x >= rightEdge)  _patrolDir = -1;
        if (_patrolDir < 0 && transform.position.x <= leftEdge)   _patrolDir =  1;

        float wobble = Mathf.Sin(Time.time * wobbleFrequency + _wobbleOffset) * wobbleAmplitude;
        _velocity = new Vector2(_patrolDir * patrolSpeed, wobble);

        SetFacing(_patrolDir > 0);

        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
        {
            _patrolTimer = patrolDuration;
            _state = State.MoveToTop;
        }
    }

    private void UpdateMoveToTop()
    {
        float topY = _cam.transform.position.y + _cam.orthographicSize + aboveScreenOffset;
        MoveToward(new Vector2(transform.position.x, topY), sweepMoveSpeed);

        if (transform.position.y >= topY - 0.1f)
        {
            _sweepGoingRight = Random.value > 0.5f;
            _state = State.MoveToSweepStart;
        }
    }

    private void UpdateMoveToSweepStart()
    {
        float halfW    = _cam.orthographicSize * _cam.aspect;
        float camX     = _cam.transform.position.x;
        float startX   = _sweepGoingRight
            ? camX - halfW - sweepEdgePadding
            : camX + halfW + sweepEdgePadding;
        float sweepY   = _cam.transform.position.y - _cam.orthographicSize + sweepYOffset;
        Vector2 target = new Vector2(startX, sweepY);

        MoveToward(target, sweepMoveSpeed);

        if (Vector2.Distance(transform.position, target) < 0.2f)
        {
            _dropTimer = 0f;
            _state = State.Sweep;
        }
    }

    private void UpdateSweep()
    {
        float halfW     = _cam.orthographicSize * _cam.aspect;
        float camX      = _cam.transform.position.x;
        float endX      = _sweepGoingRight
            ? camX + halfW + sweepEdgePadding
            : camX - halfW - sweepEdgePadding;

        int dir = _sweepGoingRight ? 1 : -1;
        _velocity = new Vector2(dir * sweepSpeed, 0f);
        SetFacing(_sweepGoingRight);

        _dropTimer -= Time.deltaTime;
        if (_dropTimer <= 0f)
        {
            _dropTimer = fishDropInterval;
            DropFish();
        }

        bool pastEnd = _sweepGoingRight
            ? transform.position.x >= endX
            : transform.position.x <= endX;

        if (pastEnd)
            _state = State.ReturnToPatrol;
    }

    private void UpdateReturnToPatrol()
    {
        float camY      = _cam.transform.position.y;
        float patrolY   = camY;
        Vector2 target  = new Vector2(transform.position.x, patrolY);
        MoveToward(target, sweepMoveSpeed);

        if (Mathf.Abs(transform.position.y - patrolY) < 0.2f)
        {
            _patrolTimer = patrolDuration;
            _state = State.Patrol;
        }
    }

    private void DropFish()
    {
        if (!spawnEnabled || droppedFishPrefab == null) return;

        GameObject obj = Instantiate(droppedFishPrefab, transform.position, Quaternion.identity);
        BossFishDroppedFish fish = obj.GetComponent<BossFishDroppedFish>();
        if (fish != null)
            fish.Init();
    }

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = ((Vector2)((Vector3)target - transform.position)).normalized;
        _velocity   = Vector2.MoveTowards(_velocity, dir * speed, 20f * Time.deltaTime);
    }

    private void SetFacing(bool faceLeft)
    {
        if (_sr != null) _sr.flipX = faceLeft;
    }

    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        Camera c    = Camera.main;
        float halfW = c.orthographicSize * c.aspect;
        float sweepY = c.transform.position.y - c.orthographicSize + sweepYOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(
            new Vector3(c.transform.position.x - halfW, sweepY, 0f),
            new Vector3(c.transform.position.x + halfW, sweepY, 0f));
    }
}