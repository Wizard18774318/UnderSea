using UnityEngine;

/// <summary>
/// Small fish minion spawned by FishMidBoss.
/// Can fly in a fixed direction OR home toward the player depending on Init() call.
/// </summary>
public class FishMinionScript : MonoBehaviour
{
    [SerializeField] private float moveSpeed   = 4f;
    [SerializeField] private float lifetime    = 8f;
    [SerializeField] private int   damage      = 2;
    [SerializeField] private float turnSpeed   = 90f; // degrees/sec when homing

    private Vector2 _dir;
    private bool _launched;
    private bool _homing;
    private Transform _player;
    private SpriteRenderer _sr;

    /// <summary>
    /// fixedDir      – flies straight in that direction.<br/>
    /// followPlayer  – homes toward the player, steers gradually.
    /// </summary>
    public void Init(Vector2 fixedDir, bool followPlayer = false)
    {
        _dir      = fixedDir.normalized;
        _homing   = followPlayer;
        _launched = true;

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) _player = p.transform;

        _sr = GetComponentInChildren<SpriteRenderer>();
        if (_sr != null && _dir.x < 0) _sr.flipX = true;
    }

    private void Update()
    {
        if (!_launched) return;

        if (_homing && _player != null)
        {
            Vector2 toPlayer = ((Vector2)_player.position - (Vector2)transform.position).normalized;
            _dir = Vector2.MoveTowards(_dir, toPlayer, turnSpeed * Mathf.Deg2Rad * Time.deltaTime).normalized;

            if (_sr != null)
                _sr.flipX = _dir.x < 0;
        }

        transform.position += (Vector3)(_dir * moveSpeed * Time.deltaTime);

        lifetime -= Time.deltaTime;
        if (lifetime <= 0f)
            Destroy(gameObject);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            var pm = other.GetComponent<PlayerManager>();
            if (pm != null) pm.TakeMeleeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Projectile"))
        {
            Destroy(other.gameObject);
            Destroy(gameObject);
        }
        else if (other.CompareTag("Destroyer"))
        {
            Destroy(gameObject);
        }
    }
}
