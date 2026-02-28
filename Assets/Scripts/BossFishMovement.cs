using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossFishMovement : MonoBehaviour
{
    [Header("Pattern 1 - Patrol")]
    [SerializeField] private float patrolSpeed       = 3f;
    [SerializeField] private float patrolEdgePadding = 3f;
    [SerializeField] private float patrolDuration    = 6f;

    [Header("Pattern 2 - Sweep")]
    [SerializeField] private float sweepMoveSpeed   = 5f;
    [SerializeField] private float sweepSpeed       = 14f;
    [SerializeField] private float sweepEdgePadding = 0.5f;
    [SerializeField] private float sweepYOffset     = -0.5f;
    [SerializeField] private float aboveScreenOffset = 1.5f;
    [SerializeField] private float fishDropInterval  = 0.35f;
    [SerializeField] private GameObject droppedFishPrefab;
    [SerializeField] private bool spawnEnabled = true;

    [Header("Wobble")]
    [SerializeField] private float wobbleAmplitude = 0.1f;
    [SerializeField] private float wobbleFrequency = 2f;

    [Header("Phase 2 - Multi Dash")]
    [SerializeField] private float dashSpeed       = 28f;
    [SerializeField] private float dashMoveSpeed   = 10f;
    [SerializeField] private float dashEdgePadding = 0.5f;
    [SerializeField] private int   dashMinCount    = 2;
    [SerializeField] private int   dashMaxCount    = 5;

    [Header("Phase 2 - Middle Pass")]
    [SerializeField] private float middlePassSpeed        = 13f;
    [SerializeField] private float middlePassDropInterval = 0.35f;
    [SerializeField] private int   middlePassMinRepeats   = 1;
    [SerializeField] private int   middlePassMaxRepeats   = 3;
    [SerializeField] private GameObject rowFishPrefab;
    [SerializeField] private float rowFishSpeed = 9f;

    [Header("Phase 2 - Center Fire")]
    [SerializeField] private GameObject redBubblePrefab;
    [SerializeField] private GameObject shieldPrefab;
    [SerializeField] private int   bubbleCount        = 12;
    [SerializeField] private float bubbleOutSpeed     = 6f;
    [SerializeField] private float bubbleReturnDelay  = 2.5f;
    [SerializeField] private float bubbleCurlSpeed    = 80f;
    [SerializeField] private int   bubbleWaveCount    = 3;
    [SerializeField] private float bubbleWaveInterval = 0.7f;
    [SerializeField] private float centerFireHold     = 0.5f;

    [Header("Phase 2 - Ambush Sweep")]
    [SerializeField] private GameObject ambushFishPrefab;
    [SerializeField] private float ambushSweepSpeed   = 10f;
    [SerializeField] private float ambushDropInterval = 0.45f;
    [SerializeField] private float ambushEdgeOffset   = 0.5f;
    [SerializeField] private int   ambushMinRepeats   = 1;
    [SerializeField] private int   ambushMaxRepeats   = 3;

    [Header("Phase 2 - Patrol")]
    [SerializeField] private float phase2PatrolSpeed       = 7f;
    [SerializeField] private float phase2PatrolMinDuration = 3f;
    [SerializeField] private float phase2PatrolMaxDuration = 7f;
    [SerializeField] private float phase2PatrolYRange      = 0.6f;  // fraction of half-height to wander

    private enum State
    {
        Patrol, MoveToTop, MoveToSweepStart, Sweep, ReturnToPatrol,
        Phase2Enter,
        TripleDashPosition, TripleDash,
        MiddlePassPosition, MiddlePass,
        CenterFirePosition, CenterFire,
        AmbushPosition, AmbushSweep,
        Phase2Patrol
    }

    private State _state = State.Patrol;
    private bool  _isPhase2;

    private Rigidbody2D    _rb;
    private SpriteRenderer _sr;
    private Camera         _cam;

    private Vector2 _velocity;
    private float   _patrolTimer;
    private float   _dropTimer;
    private float   _wobbleOffset;
    private int     _patrolDir = 1;
    private bool    _sweepGoingRight;

    private int     _dashCount;
    private int     _dashTarget;
    private float   _currentDashY;
    private bool    _dashGoingRight;

    private bool    _middlePassGoingRight;
    private int     _middlePassCount;
    private int     _middlePassTarget;

    private float   _centerFireTimer;
    private float   _waveTimer;
    private int     _wavesFired;
    private int     _phase2PatternIndex;
    private int[]   _patternQueue;
    private float   _phase2PatrolYTarget;

    private bool                     _ambushGoingRight;
    private int                      _ambushCount;
    private int                      _ambushTarget;
    private List<BossFishAmbushFish>  _ambushFish     = new List<BossFishAmbushFish>();

    private GameObject               _shieldInstance;
    private List<RedOxygenBubble>    _activeBubbles  = new List<RedOxygenBubble>();
    private BossFishManager          _manager;

    private void Awake()
    {
        _rb           = GetComponent<Rigidbody2D>();
        _sr           = GetComponentInChildren<SpriteRenderer>();
        _cam          = Camera.main;
        _manager      = GetComponent<BossFishManager>();
        _wobbleOffset = Random.Range(0f, Mathf.PI * 2f);
        _patrolTimer  = patrolDuration;
    }

    public void EnterPhase2()
    {
        if (_isPhase2) return;
        _isPhase2 = true;
        _velocity  = Vector2.zero;
        _state     = State.Phase2Enter;
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Patrol:              UpdatePatrol();              break;
            case State.MoveToTop:           UpdateMoveToTop();           break;
            case State.MoveToSweepStart:    UpdateMoveToSweepStart();    break;
            case State.Sweep:               UpdateSweep();               break;
            case State.ReturnToPatrol:      UpdateReturnToPatrol();      break;
            case State.Phase2Enter:         UpdatePhase2Enter();         break;
            case State.TripleDashPosition:  UpdateTripleDashPosition();  break;
            case State.TripleDash:          UpdateTripleDash();          break;
            case State.MiddlePassPosition:  UpdateMiddlePassPosition();  break;
            case State.MiddlePass:          UpdateMiddlePass();          break;
            case State.CenterFirePosition:  UpdateCenterFirePosition();  break;
            case State.CenterFire:          UpdateCenterFire();          break;
            case State.AmbushPosition:      UpdateAmbushPosition();      break;
            case State.AmbushSweep:         UpdateAmbushSweep();         break;
            case State.Phase2Patrol:        UpdatePhase2Patrol();        break;
        }
    }

    private void FixedUpdate()
    {
        _rb.linearVelocity = _velocity;
    }

    private void UpdatePatrol()
    {
        float halfW     = _cam.orthographicSize * _cam.aspect;
        float camX      = _cam.transform.position.x;
        float leftEdge  = camX - halfW + patrolEdgePadding;
        float rightEdge = camX + halfW - patrolEdgePadding;

        if (_patrolDir > 0 && transform.position.x >= rightEdge) _patrolDir = -1;
        if (_patrolDir < 0 && transform.position.x <= leftEdge)  _patrolDir =  1;

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
        if (Mathf.Abs(_velocity.x) > 0.1f) SetFacing(_velocity.x > 0);

        if (transform.position.y >= topY - 0.1f)
        {
            _sweepGoingRight = Random.value > 0.5f;
            _state = State.MoveToSweepStart;
        }
    }

    private void UpdateMoveToSweepStart()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float camX   = _cam.transform.position.x;
        float startX = _sweepGoingRight
            ? camX - halfW - sweepEdgePadding
            : camX + halfW + sweepEdgePadding;
        float sweepY   = _cam.transform.position.y - _cam.orthographicSize + sweepYOffset;
        Vector2 target = new Vector2(startX, sweepY);

        MoveToward(target, sweepMoveSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f) SetFacing(_velocity.x > 0);

        if (Vector2.Distance(transform.position, target) < 0.2f)
        {
            _dropTimer = 0f;
            _state = State.Sweep;
        }
    }

    private void UpdateSweep()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        float camX  = _cam.transform.position.x;
        float endX  = _sweepGoingRight
            ? camX + halfW + sweepEdgePadding
            : camX - halfW - sweepEdgePadding;

        int dir = _sweepGoingRight ? 1 : -1;
        _velocity = new Vector2(dir * sweepSpeed, 0f);
        SetFacing(_sweepGoingRight);

        _dropTimer -= Time.deltaTime;
        if (_dropTimer <= 0f)
        {
            _dropTimer = fishDropInterval;
            DropSweepFish();
        }

        bool pastEnd = _sweepGoingRight
            ? transform.position.x >= endX
            : transform.position.x <= endX;
        if (pastEnd) _state = State.ReturnToPatrol;
    }

    private void UpdateReturnToPatrol()
    {
        float camY    = _cam.transform.position.y;
        Vector2 target = new Vector2(transform.position.x, camY);
        MoveToward(target, sweepMoveSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f) SetFacing(_velocity.x > 0);

        if (Mathf.Abs(transform.position.y - camY) < 0.2f)
        {
            _patrolTimer = patrolDuration;
            _state = State.Patrol;
        }
    }

    private void UpdatePhase2Enter()
    {
        Decelerate();
        if (_velocity.sqrMagnitude < 0.01f)
            StartNextPhase2Pattern();
    }

    private void StartNextPhase2Pattern()
    {
        if (_patternQueue == null || _phase2PatternIndex >= _patternQueue.Length)
        {
            _patternQueue       = ShuffledPatterns();
            _phase2PatternIndex = 0;
        }
        int next = _patternQueue[_phase2PatternIndex++];
        switch (next)
        {
            case 0: EnterTripleDash();   break;
            case 1: EnterMiddlePass();   break;
            case 2: EnterCenterFire();   break;
            case 3: EnterAmbushSweep();  break;
            case 4: EnterPhase2Patrol(); break;
        }
    }

    private int[] ShuffledPatterns()
    {
        int[] p = new int[] { 0, 1, 2, 3, 4 };
        for (int i = p.Length - 1; i > 0; i--)
        {
            int j   = Random.Range(0, i + 1);
            int tmp = p[i]; p[i] = p[j]; p[j] = tmp;
        }
        return p;
    }

    private void EnterTripleDash()
    {
        _dashCount      = 0;
        _dashTarget     = Random.Range(dashMinCount, dashMaxCount + 1);
        _dashGoingRight = Random.value > 0.5f;
        _currentDashY   = RandomDashY();
        _state = State.TripleDashPosition;
    }

    private float RandomDashY()
    {
        float camY  = _cam.transform.position.y;
        float halfH = _cam.orthographicSize;
        return Random.Range(camY - halfH * 0.75f, camY + halfH * 0.75f);
    }

    private void UpdateTripleDashPosition()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float camX   = _cam.transform.position.x;
        float startX = _dashGoingRight
            ? camX - halfW - dashEdgePadding
            : camX + halfW + dashEdgePadding;
        Vector2 target = new Vector2(startX, _currentDashY);

        MoveToward(target, dashMoveSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f)
            SetFacing(_velocity.x > 0);
        if (Vector2.Distance(transform.position, target) < 0.25f)
        {
            transform.position = target;
            _state = State.TripleDash;
        }
    }

    private void UpdateTripleDash()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        float camX  = _cam.transform.position.x;
        float endX  = _dashGoingRight
            ? camX + halfW + dashEdgePadding
            : camX - halfW - dashEdgePadding;

        _velocity = new Vector2((_dashGoingRight ? 1 : -1) * dashSpeed, 0f);
        SetFacing(_dashGoingRight);

        bool pastEnd = _dashGoingRight
            ? transform.position.x >= endX
            : transform.position.x <= endX;

        if (pastEnd)
        {
            _dashCount++;
            _dashGoingRight = Random.value > 0.5f;
            _currentDashY   = RandomDashY();
            if (_dashCount >= _dashTarget)
                StartNextPhase2Pattern();
            else
                _state = State.TripleDashPosition;
        }
    }

    private void EnterMiddlePass()
    {
        _middlePassGoingRight = Random.value > 0.5f;
        _middlePassCount      = 0;
        _middlePassTarget     = Random.Range(middlePassMinRepeats, middlePassMaxRepeats + 1);
        _state = State.MiddlePassPosition;
    }

    private void UpdateMiddlePassPosition()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float camX   = _cam.transform.position.x;
        float startX = _middlePassGoingRight
            ? camX - halfW - sweepEdgePadding
            : camX + halfW + sweepEdgePadding;
        Vector2 target = new Vector2(startX, _cam.transform.position.y);

        MoveToward(target, sweepMoveSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f)
            SetFacing(_velocity.x > 0);
        if (Vector2.Distance(transform.position, target) < 0.25f)
        {
            _dropTimer = 0f;
            _state = State.MiddlePass;
        }
    }

    private void UpdateMiddlePass()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        float camX  = _cam.transform.position.x;
        float endX  = _middlePassGoingRight
            ? camX + halfW + sweepEdgePadding
            : camX - halfW - sweepEdgePadding;

        _velocity = new Vector2((_middlePassGoingRight ? 1 : -1) * middlePassSpeed, 0f);
        SetFacing(_middlePassGoingRight);

        _dropTimer -= Time.deltaTime;
        if (_dropTimer <= 0f)
        {
            _dropTimer = middlePassDropInterval;
            DropRowFish();
        }

        bool pastEnd = _middlePassGoingRight
            ? transform.position.x >= endX
            : transform.position.x <= endX;
        if (pastEnd)
        {
            _middlePassCount++;
            _middlePassGoingRight = !_middlePassGoingRight;
            if (_middlePassCount >= _middlePassTarget)
                StartNextPhase2Pattern();
            else
                _state = State.MiddlePassPosition;
        }
    }

    private void EnterCenterFire()
    {
        _state = State.CenterFirePosition;
    }

    private void UpdateCenterFirePosition()
    {
        Vector2 center = new Vector2(_cam.transform.position.x, _cam.transform.position.y);
        MoveToward(center, sweepMoveSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f)
            SetFacing(_velocity.x > 0);

        if (Vector2.Distance(transform.position, center) < 0.3f)
        {
            transform.position = center;
            _velocity          = Vector2.zero;
            _centerFireTimer   = -1f;   // negative = waiting for bubbles
            _wavesFired        = 0;
            _waveTimer         = 0f;
            _activeBubbles.Clear();
            if (shieldPrefab != null)
                _shieldInstance = Instantiate(shieldPrefab, transform);
            _manager?.ActivateShield();
            _state = State.CenterFire;
        }
    }

    private void UpdateCenterFire()
    {
        _velocity = Vector2.zero;

        // Phase 1: fire all waves
        if (_wavesFired < bubbleWaveCount)
        {
            _waveTimer -= Time.deltaTime;
            if (_waveTimer <= 0f)
            {
                _waveTimer = bubbleWaveInterval;
                FireRedBubbles(_wavesFired);
                _wavesFired++;
            }
            return;
        }

        // Phase 2: wait until every bubble is destroyed
        if (_centerFireTimer < 0f)
        {
            _activeBubbles.RemoveAll(b => b == null);
            if (_activeBubbles.Count == 0)
            {
                // All bubbles gone — drop shield and start exit hold
                if (_shieldInstance != null) { Destroy(_shieldInstance); _shieldInstance = null; }
                _manager?.DeactivateShield();
                _centerFireTimer = centerFireHold;
            }
            return;
        }

        // Phase 3: brief hold after shield drops
        _centerFireTimer -= Time.deltaTime;
        if (_centerFireTimer <= 0f)
            StartNextPhase2Pattern();
    }

    private void EnterAmbushSweep()
    {
        _ambushGoingRight = Random.value > 0.5f;
        _ambushCount      = 0;
        _ambushTarget     = Random.Range(ambushMinRepeats, ambushMaxRepeats + 1);
        _ambushFish.Clear();
        _state = State.AmbushPosition;
    }

    private void UpdateAmbushPosition()
    {
        float halfW  = _cam.orthographicSize * _cam.aspect;
        float camX   = _cam.transform.position.x;
        float startX = _ambushGoingRight
            ? camX - halfW - sweepEdgePadding
            : camX + halfW + sweepEdgePadding;
        Vector2 target = new Vector2(startX, _cam.transform.position.y);

        MoveToward(target, ambushSweepSpeed);
        if (Mathf.Abs(_velocity.x) > 0.1f)
            SetFacing(_velocity.x > 0);

        if (Vector2.Distance(transform.position, target) < 0.25f)
        {
            _dropTimer = 0f;
            _state = State.AmbushSweep;
        }
    }

    private void UpdateAmbushSweep()
    {
        float halfW = _cam.orthographicSize * _cam.aspect;
        float camX  = _cam.transform.position.x;
        float endX  = _ambushGoingRight
            ? camX + halfW + sweepEdgePadding
            : camX - halfW - sweepEdgePadding;

        _velocity = new Vector2((_ambushGoingRight ? 1 : -1) * ambushSweepSpeed, 0f);
        SetFacing(_ambushGoingRight);

        _dropTimer -= Time.deltaTime;
        if (_dropTimer <= 0f && spawnEnabled && ambushFishPrefab != null)
        {
            _dropTimer = ambushDropInterval;
            SpawnAmbushPair();
        }

        bool pastEnd = _ambushGoingRight
            ? transform.position.x >= endX
            : transform.position.x <= endX;

        if (pastEnd)
        {
            LaunchAllAmbushFish();
            _ambushCount++;
            if (_ambushCount >= _ambushTarget)
            {
                StartNextPhase2Pattern();
            }
            else
            {
                _ambushGoingRight = !_ambushGoingRight;
                _ambushFish.Clear();
                _state = State.AmbushPosition;
            }
        }
    }

    private void SpawnAmbushPair()
    {
        float halfH     = _cam.orthographicSize;
        float camY      = _cam.transform.position.y;
        float topY      = camY + halfH + ambushEdgeOffset;
        float bottomY   = camY - halfH - ambushEdgeOffset;
        float x         = transform.position.x;

        var topObj  = Instantiate(ambushFishPrefab, new Vector3(x, topY), Quaternion.identity);
        var topFish = topObj.GetComponent<BossFishAmbushFish>();
        if (topFish != null) _ambushFish.Add(topFish);

        var botObj  = Instantiate(ambushFishPrefab, new Vector3(x, bottomY), Quaternion.identity);
        var botFish = botObj.GetComponent<BossFishAmbushFish>();
        if (botFish != null) _ambushFish.Add(botFish);
    }

    private void LaunchAllAmbushFish()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 targetPos = player != null
            ? (Vector2)player.transform.position
            : (Vector2)_cam.transform.position;

        foreach (var fish in _ambushFish)
        {
            if (fish != null) fish.Launch(targetPos);
        }
        _ambushFish.Clear();
    }

    private void EnterPhase2Patrol()
    {
        _patrolTimer         = Random.Range(phase2PatrolMinDuration, phase2PatrolMaxDuration);
        _phase2PatrolYTarget = _cam.transform.position.y +
                               Random.Range(-_cam.orthographicSize * phase2PatrolYRange,
                                             _cam.orthographicSize * phase2PatrolYRange);
        _state = State.Phase2Patrol;
    }

    private void UpdatePhase2Patrol()
    {
        float halfW     = _cam.orthographicSize * _cam.aspect;
        float camX      = _cam.transform.position.x;
        float leftEdge  = camX - halfW + patrolEdgePadding;
        float rightEdge = camX + halfW - patrolEdgePadding;

        if (_patrolDir > 0 && transform.position.x >= rightEdge) _patrolDir = -1;
        if (_patrolDir < 0 && transform.position.x <= leftEdge)  _patrolDir =  1;

        // Drift toward random Y target; pick a new one when close
        if (Mathf.Abs(transform.position.y - _phase2PatrolYTarget) < 0.3f)
        {
            _phase2PatrolYTarget = _cam.transform.position.y +
                                   Random.Range(-_cam.orthographicSize * phase2PatrolYRange,
                                                 _cam.orthographicSize * phase2PatrolYRange);
        }
        float yDiff = _phase2PatrolYTarget - transform.position.y;
        float yVel  = Mathf.Clamp(yDiff * 4f, -phase2PatrolSpeed * 0.5f, phase2PatrolSpeed * 0.5f);

        _velocity = new Vector2(_patrolDir * phase2PatrolSpeed, yVel);
        SetFacing(_patrolDir > 0);

        _patrolTimer -= Time.deltaTime;
        if (_patrolTimer <= 0f)
            StartNextPhase2Pattern();
    }

    private void DropSweepFish()
    {
        if (!spawnEnabled || droppedFishPrefab == null) return;
        var obj  = Instantiate(droppedFishPrefab, transform.position, Quaternion.identity);
        var fish = obj.GetComponent<BossFishDroppedFish>();
        if (fish != null) fish.Init();
    }

    private void DropRowFish()
    {
        if (!spawnEnabled || rowFishPrefab == null) return;

        var up   = Instantiate(rowFishPrefab, transform.position, Quaternion.identity);
        var fishU = up.GetComponent<BossFishDroppedFish>();
        if (fishU != null) fishU.InitDirection(Vector2.up, rowFishSpeed);

        var down  = Instantiate(rowFishPrefab, transform.position, Quaternion.identity);
        var fishD = down.GetComponent<BossFishDroppedFish>();
        if (fishD != null) fishD.InitDirection(Vector2.down, rowFishSpeed);
    }

    private void FireRedBubbles(int waveIndex)
    {
        if (redBubblePrefab == null) return;
        float step       = 360f / bubbleCount;
        float waveOffset = waveIndex * (step * 0.5f);
        for (int i = 0; i < bubbleCount; i++)
        {
            float rad  = (i * step + waveOffset) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            float curl  = (i % 2 == 0 ? 1f : -1f) * bubbleCurlSpeed;
            var obj    = Instantiate(redBubblePrefab, transform.position, Quaternion.identity);
            var bubble = obj.GetComponent<RedOxygenBubble>();
            if (bubble != null)
            {
                bubble.Init(dir, bubbleOutSpeed, bubbleReturnDelay, curl);
                _activeBubbles.Add(bubble);
            }
        }
    }

    private void MoveToward(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        _velocity   = Vector2.MoveTowards(_velocity, dir * speed, 20f * Time.deltaTime);
    }

    private void SetFacing(bool faceLeft)
    {
        if (_sr != null) _sr.flipX = faceLeft;
    }

    private void Decelerate()
    {
        _velocity = Vector2.MoveTowards(_velocity, Vector2.zero, 20f * Time.deltaTime);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            Debug.Log($"[BossFish] Body contact with player! State: {_state}");
    }

    private void OnDrawGizmosSelected()
    {
        if (Camera.main == null) return;
        Camera c     = Camera.main;
        float halfW  = c.orthographicSize * c.aspect;
        float sweepY = c.transform.position.y - c.orthographicSize + sweepYOffset;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(new Vector3(c.transform.position.x - halfW, sweepY),
                        new Vector3(c.transform.position.x + halfW, sweepY));
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(new Vector3(c.transform.position.x, c.transform.position.y), 0.5f);
    }
}
