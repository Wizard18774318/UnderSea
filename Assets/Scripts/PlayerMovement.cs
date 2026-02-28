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
    private Vector2 moveInput;
    private Vector2 currentVelocity;
    private Vector2 lastMoveDirection = Vector2.right;
    private Vector2 dashDirection;
    private bool isDashing;
    private float dashTimer;
    private float dashCooldownTimer;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        moveInput = new Vector2(moveX, moveY);

        if (moveInput.sqrMagnitude > 0.0001f)
        {
            lastMoveDirection = moveInput.normalized;
        }

        if (dashCooldownTimer > 0f)
        {
            dashCooldownTimer = Mathf.Max(0f, dashCooldownTimer - Time.deltaTime);
        }

        bool dashPressed = Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        if (dashPressed && !isDashing && dashCooldownTimer <= 0f && lastMoveDirection.sqrMagnitude > 0.0001f)
        {
            StartDash();
        }
    }

    void FixedUpdate()
    {
        if (isDashing)
        {
            currentVelocity = dashDirection * dashSpeed;
            dashTimer -= Time.fixedDeltaTime;

            if (dashTimer <= 0f)
            {
                isDashing = false;
            }

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

    private void StartDash()
    {
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        dashDirection = lastMoveDirection;
    }

    public Vector2 CurrentVelocity => currentVelocity;
}
