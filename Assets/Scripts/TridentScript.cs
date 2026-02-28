using UnityEngine;

/// <summary>
/// Trident projectile – pure transform movement, no Rigidbody2D needed.
/// • Flies in any direction set via Init().
/// • Damages the player on contact but is NOT destroyed by it.
/// • Destroyed by any collider tagged "Projectile" (player shots).
/// • Self-destructs when it exits the screen.
/// </summary>
public class TridentScript : MonoBehaviour
{
    [SerializeField] private float damage            = 3f;
    [SerializeField] private float offScreenKill     = 2f;
    [SerializeField] private float spriteAngleOffset = -45f; // sprite art rests at 45° → subtract to align with movement

    private Vector2 _dir;
    private float   _speed;
    private bool    _launched;

    private void Awake()
    {
        // Disable any Rigidbody2D on this object so physics can't fight movement
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.simulated = false;
    }

    /// <summary>Set movement direction and speed. Call immediately after Instantiate.</summary>
    public void Init(Vector2 direction, float speed)
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

        transform.position += (Vector3)(_dir * _speed * Time.deltaTime);

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
            return; // keep flying
        }
    }
}
