using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float acceleration = 12f;
    [SerializeField] private float deceleration = 6f;
    [SerializeField] private float dashSpeed = 12f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 1f;


    private Rigidbody2D rb;
    private Camera _cam;
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    // Grace period: prevents diagonal→cardinal snap when releasing keys slightly apart
    private const float DirectionGracePeriod = 0.08f;
    private float directionGraceTimer;
    private bool wasDiagonal;

    // 45° snap directions
    private static readonly Vector2[] SnapDirs =
    {
        Vector2.right,
        new Vector2( 1f,  1f).normalized,
        Vector2.up,
        new Vector2(-1f,  1f).normalized,
        Vector2.left,
        new Vector2(-1f, -1f).normalized,
        Vector2.down,
        new Vector2( 1f, -1f).normalized,
    };

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        _cam = Camera.main;
    }

    void Update()
    {
        if (_cam == null) _cam = Camera.main;

        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY);

        bool isDiagonal = Mathf.Abs(moveX) > 0.01f && Mathf.Abs(moveY) > 0.01f;
        bool isCardinal = !isDiagonal && moveInput.sqrMagnitude > 0.0001f;

        if (isDiagonal)
        {
            // Diagonal input: update direction immediately, reset grace
            lastMoveDirection = moveInput.normalized;
            wasDiagonal = true;
            directionGraceTimer = 0f;
        }
        else if (isCardinal && wasDiagonal)
        {
            // Just went from diagonal to cardinal — one key released early
            directionGraceTimer += Time.deltaTime;
            if (directionGraceTimer >= DirectionGracePeriod)
            {
                // Held single key long enough, accept new direction
                lastMoveDirection = moveInput.normalized;
                wasDiagonal = false;
                directionGraceTimer = 0f;
            }
            // else: keep the old diagonal lastMoveDirection
        }
        else if (isCardinal)
        {
            // Pure cardinal input (not coming from diagonal)
            lastMoveDirection = moveInput.normalized;
        }
        else
        {
            // No input — reset grace tracking
            wasDiagonal = false;
            directionGraceTimer = 0f;
        }

        // Aim-lock: hold Ctrl to rotate in place without moving
        if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            moveInput = Vector2.zero;

        if (dashCooldownTimer > 0f)
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);

        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetMouseButtonDown(1);
        if (dashPressed && !isDashing && dashCooldownTimer <= 0f && lastMoveDirection.sqrMagnitude > 0.0001f)
            StartDash();

        UpdateRotation();
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            currentVelocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;
            if (dashTimer <= 0f) isDashing = false;
            rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
            return;
        }

        bool hasInput = moveInput.sqrMagnitude > 0.01f;
        Vector2 targetVelocity = hasInput ? moveInput.normalized * moveSpeed : Vector2.zero;
        float ramp = hasInput ? acceleration : deceleration;
        float logStep = Mathf.Log(1f + ramp * Time.fixedDeltaTime);

        Vector2 delta = targetVelocity - currentVelocity;
        if (delta.sqrMagnitude > 0.0001f)
        {
            Vector2 step = delta.normalized * Mathf.Min(delta.magnitude, logStep);
            currentVelocity += step;
        }

        rb.MovePosition(rb.position + currentVelocity * Time.fixedDeltaTime);
    }

    private void UpdateRotation()
    {
        float angle;

        bool mouseAiming = GameSettings.Instance != null && GameSettings.Instance.mouseAiming;
        if (mouseAiming)
        {
            // Rotate toward mouse cursor
            Vector3 mouseWorld = _cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 dir = (mouseWorld - transform.position).normalized;
            angle = Mathf.Atan2(-dir.x, dir.y) * Mathf.Rad2Deg;
        }
        else
        {
            // Snap to nearest 45° based on last movement direction
            if (lastMoveDirection.sqrMagnitude < 0.0001f) return;
            Vector2 best = SnapDirs[0];
            float bestDot = -2f;
            foreach (Vector2 sd in SnapDirs)
            {
                float dot = Vector2.Dot(lastMoveDirection.normalized, sd);
                if (dot > bestDot) { bestDot = dot; best = sd; }
            }
            angle = Mathf.Atan2(-best.x, best.y) * Mathf.Rad2Deg;
        }

        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = lastMoveDirection;
    }

    public Vector2 CurrentVelocity => currentVelocity;
}

