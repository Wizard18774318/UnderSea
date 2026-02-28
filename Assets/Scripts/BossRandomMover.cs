using UnityEngine;

/// <summary>
/// Moves the Level 4 boss toward random targets that stay within the
/// camera view. Acceleration grows logarithmically with remaining distance
/// so the boss eases in/out without feeling linear.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossRandomMover : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private float cameraPadding = 0.75f;

    [Header("Movement")]
    [SerializeField] private float accelerationScale = 6f;
    [SerializeField] private float maxSpeed = 8f;
    [SerializeField] private float velocityResponsiveness = 8f;
    [SerializeField] private float arrivalThreshold = 0.3f;

    [Header("Water Drag")]
    [SerializeField] private float dragStrength = 1.1f;

    [Header("Difficulty Scaling")]
    [SerializeField] private bool adjustForMouseAiming = true;
    [SerializeField] private float mouseAimingSpeedMultiplier = 0.85f;

    [Header("Target Hold")]
    [SerializeField] private float minHoldTime = 0.35f;
    [SerializeField] private float maxHoldTime = 0.9f;

    private Rigidbody2D _rb;
    private Camera _cam;
    private Vector2 _currentTarget;
    private float _holdTimer;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
        _currentTarget = _rb.position;

        if (adjustForMouseAiming && GameSettings.Instance != null && GameSettings.Instance.mouseAiming)
        {
            float mult = Mathf.Clamp(mouseAimingSpeedMultiplier, 0.1f, 2f);
            accelerationScale *= mult;
            maxSpeed *= mult;
        }
    }

    private void Update()
    {
        if (_cam == null) _cam = Camera.main;
        if (_cam == null) return;

        if (_holdTimer > 0f)
        {
            _holdTimer -= Time.deltaTime;
            if (_holdTimer <= 0f)
                ChooseNextTarget();
            return;
        }

        float distance = Vector2.Distance(_rb.position, _currentTarget);
        if (distance <= arrivalThreshold)
        {
            _holdTimer = NextHoldTime();
            return;
        }
    }

    private void FixedUpdate()
    {
        if (_cam == null) return;

        Vector2 velocity = _rb.linearVelocity;

        if (_holdTimer > 0f)
        {
            _rb.linearVelocity = ApplyWaterDrag(velocity);
            return;
        }

        Vector2 toTarget = _currentTarget - _rb.position;
        if (toTarget.sqrMagnitude < arrivalThreshold * arrivalThreshold)
        {
            _rb.linearVelocity = ApplyWaterDrag(velocity);
            return;
        }

        // Logarithmic acceleration makes long moves ramp faster without overshooting close targets.
        float acceleration = Mathf.Log(toTarget.magnitude + 1f) * accelerationScale;
        float speed = Mathf.Clamp(acceleration, 0f, maxSpeed);
        Vector2 desiredVelocity = toTarget.normalized * speed;
        Vector2 blendedVelocity = Vector2.Lerp(
            velocity,
            desiredVelocity,
            Time.fixedDeltaTime * velocityResponsiveness);

        _rb.linearVelocity = ApplyWaterDrag(blendedVelocity);
    }

    private void ChooseNextTarget()
    {
        if (_cam == null) return;
        Vector3 camPos = _cam.transform.position;
        float halfHeight = _cam.orthographicSize;
        float halfWidth = halfHeight * _cam.aspect;

        float minX = camPos.x - halfWidth + cameraPadding;
        float maxX = camPos.x + halfWidth - cameraPadding;
        float minY = camPos.y - halfHeight + cameraPadding;
        float maxY = camPos.y + halfHeight - cameraPadding;

        if (minX > maxX)
        {
            float tmp = minX;
            minX = maxX;
            maxX = tmp;
        }

        if (minY > maxY)
        {
            float tmp = minY;
            minY = maxY;
            maxY = tmp;
        }

        _currentTarget = new Vector2(
            Random.Range(minX, maxX),
            Random.Range(minY, maxY));
    }

    private void OnEnable()
    {
        if (_cam == null) _cam = Camera.main;
        ChooseNextTarget();
        _holdTimer = 0f;
    }

    private float NextHoldTime()
    {
        float low  = Mathf.Max(0f, Mathf.Min(minHoldTime, maxHoldTime));
        float high = Mathf.Max(low, Mathf.Max(minHoldTime, maxHoldTime));
        return Mathf.Approximately(low, high) ? low : Random.Range(low, high);
    }

    private Vector2 ApplyWaterDrag(Vector2 velocity)
    {
        float strength = Mathf.Max(0f, dragStrength);
        float decay = Mathf.Exp(-strength * Time.fixedDeltaTime);
        return velocity * decay;
    }
}
