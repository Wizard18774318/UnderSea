using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BossFishDroppedFish : MonoBehaviour
{
    [SerializeField] private float acceleration  = 18f;
    [SerializeField] private float maxSpeed      = 16f;
    [SerializeField] private float damage        = 2f;
    [SerializeField] private float offScreenKill = 3f;
    [SerializeField] private float spriteAngleOffset = 0f; // adjust if sprite isn't facing right by default

    private Rigidbody2D _rb;
    private SpriteRenderer _sr;
    private Vector2 _moveDir;
    private float   _speed;
    private bool    _initialised;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
        _sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        Vector2 targetPos = player != null ? (Vector2)player.transform.position : (Vector2)transform.position + Vector2.down;

        InitDirection((targetPos - (Vector2)transform.position).normalized, maxSpeed);
    }

    public void InitDirection(Vector2 direction, float speed)
    {
        _moveDir     = direction.normalized;
        maxSpeed     = speed;
        _initialised = true;

        float angle = Mathf.Atan2(_moveDir.y, _moveDir.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!_initialised) return;

        _speed = Mathf.MoveTowards(_speed, maxSpeed, acceleration * Time.deltaTime);
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
