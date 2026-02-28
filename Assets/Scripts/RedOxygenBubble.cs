using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class RedOxygenBubble : MonoBehaviour
{
    [SerializeField] private float damage        = 2f;
    [SerializeField] private float returnSpeed   = 10f;
    [SerializeField] private float destroyRadius = 0.4f;

    private Rigidbody2D _rb;
    private Vector2 _outDir;
    private float   _outSpeed;
    private float   _returnTimer;
    private float   _curlSpeed;    // degrees per second; positive = CCW, negative = CW
    private bool    _returning;
    private bool    _initialised;

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public void Init(Vector2 direction, float outSpeed, float returnDelay, float curlSpeed = 0f)
    {
        _outDir      = direction.normalized;
        _outSpeed    = outSpeed;
        _returnTimer = returnDelay;
        _curlSpeed   = curlSpeed;
        _initialised = true;
    }

    private void Update()
    {
        if (!_initialised) return;

        if (!_returning)
        {
            if (_curlSpeed != 0f)
            {
                float angle = _curlSpeed * Time.deltaTime * Mathf.Deg2Rad;
                float cos   = Mathf.Cos(angle);
                float sin   = Mathf.Sin(angle);
                _outDir = new Vector2(
                    cos * _outDir.x - sin * _outDir.y,
                    sin * _outDir.x + cos * _outDir.y
                ).normalized;
            }
            _rb.linearVelocity = _outDir * _outSpeed;

            _returnTimer -= Time.deltaTime;
            if (_returnTimer <= 0f)
                _returning = true;
        }
        else
        {
            Vector2 toOrigin = (Vector2.zero - (Vector2)transform.position);
            if (toOrigin.magnitude <= destroyRadius)
            {
                Destroy(gameObject);
                return;
            }
            _rb.linearVelocity = toOrigin.normalized * returnSpeed;
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
