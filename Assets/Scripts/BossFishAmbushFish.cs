using UnityEngine;

/// <summary>
/// Sits still at the top or bottom screen edge, then launches toward a target
/// position when Launch() is called by the boss.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class BossFishAmbushFish : MonoBehaviour
{
    [SerializeField] private float launchSpeed   = 16f;
    [SerializeField] private float acceleration  = 20f;
    [SerializeField] private float damage        = 2f;
    [SerializeField] private float offScreenKill = 3f;

    private Rigidbody2D    _rb;
    private SpriteRenderer _sr;
    private Vector2        _moveDir;
    private float          _speed;
    private bool           _launched;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    /// <summary>
    /// Called by boss when all ambush fish should fire simultaneously.
    /// targetPos is the player's world position at this moment.
    /// </summary>
    public void Launch(Vector2 targetPos)
    {
        _moveDir  = (targetPos - (Vector2)transform.position).normalized;
        _launched = true;
        if (_sr != null) _sr.flipX = _moveDir.x < 0f;
    }

    private void Update()
    {
        if (!_launched)
        {
            _rb.linearVelocity = Vector2.zero;
            return;
        }

        _speed = Mathf.MoveTowards(_speed, launchSpeed, acceleration * Time.deltaTime);
        _rb.linearVelocity = _moveDir * _speed;

        Camera cam = Camera.main;
        if (cam != null)
        {
            float halfW = cam.orthographicSize * cam.aspect;
            float halfH = cam.orthographicSize;
            Vector3 c   = cam.transform.position;
            if (transform.position.x < c.x - halfW - offScreenKill ||
                transform.position.x > c.x + halfW + offScreenKill ||
                transform.position.y < c.y - halfH - offScreenKill ||
                transform.position.y > c.y + halfH + offScreenKill)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerManager>() != null)
        {
            PlayerStatsManager.Instance?.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
