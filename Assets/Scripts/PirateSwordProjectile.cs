using UnityEngine;

/// <summary>
/// A sword fired during the Pirate boss formation attack.
/// Flies in a fixed direction, damages the player on contact.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class PirateSwordProjectile : MonoBehaviour
{
    [SerializeField] private float damage             = 3f;
    [SerializeField] private float offScreenKill       = 2f;
    [SerializeField] private float spriteAngleOffset   = -123.09f; // offset so sprite faces movement dir

    private Rigidbody2D _rb;
    private Vector2     _dir;
    private float       _speed;
    private bool        _launched;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Launch(Vector2 direction, float speed)
    {
        _dir      = direction.normalized;
        _speed    = speed;
        _launched = true;

        float angle = Mathf.Atan2(_dir.y, _dir.x) * Mathf.Rad2Deg + spriteAngleOffset;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void Update()
    {
        if (!_launched) return;

        _rb.linearVelocity = _dir * _speed;

        Camera cam = Camera.main;
        if (cam == null) return;
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

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerManager>() != null)
        {
            PlayerStatsManager.Instance?.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}
