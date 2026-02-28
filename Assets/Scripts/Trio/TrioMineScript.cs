using UnityEngine;

/// <summary>
/// Mine dropped by FishBigBoss. Sits in place, explodes on player contact
/// or after its lifetime expires.
///
/// SETUP
///   1. Uses Mine/MineBig/MineSmall prefab from Underwater Diving.
///   2. Add: Collider2D (IsTrigger = true), SpriteRenderer, this script.
///   3. No Rigidbody2D needed (static mine).
///   4. FishBigBoss calls Init() after instantiation.
/// </summary>
public class TrioMineScript : MonoBehaviour
{
    private float _lifetime;
    private int   _damage;
    private float _timer;
    private GameObject _explosionPrefab;
    private float _explosionScale = 1f;
    private bool _initialized;

    [Tooltip("Pulsing speed as mine is about to explode")]
    [SerializeField] private float pulseSpeed = 4f;
    [Tooltip("Fraction of lifetime left when pulsing starts")]
    [SerializeField] private float pulseThreshold = 0.3f;

    private SpriteRenderer _sr;
    private Vector3 _baseScale;

    /// <summary>Called by FishBigBoss right after Instantiate.</summary>
    public void Init(float lifetime, int damage, GameObject explosionPrefab, float explosionScale = 1f)
    {
        _lifetime        = lifetime;
        _damage          = damage;
        _explosionPrefab = explosionPrefab;
        _explosionScale  = explosionScale;
        _timer           = lifetime;
        _initialized     = true;
    }

    private void Awake()
    {
        _sr = GetComponentInChildren<SpriteRenderer>();
        _baseScale = transform.localScale;
    }

    private void Update()
    {
        if (!_initialized) return;

        _timer -= Time.deltaTime;

        // Pulse when about to blow
        if (_timer < _lifetime * pulseThreshold)
        {
            float pulse = 1f + 0.15f * Mathf.Sin(Time.time * pulseSpeed * Mathf.PI * 2f);
            transform.localScale = _baseScale * pulse;
        }

        if (_timer <= 0f)
            Explode();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // Deal damage through PlayerManager
            var pm = other.GetComponent<PlayerManager>();
            if (pm != null) pm.TakeMeleeDamage(_damage);
            Explode();
        }
        // Detonated by player projectile too
        else if (other.CompareTag("Projectile"))
        {
            Destroy(other.gameObject);
            Explode();
        }
    }

    private void Explode()
    {
        if (_explosionPrefab != null)
        {
            GameObject fx = Instantiate(_explosionPrefab, transform.position, Quaternion.identity);
            fx.transform.localScale = new Vector3(_explosionScale, _explosionScale, 1f);
            Destroy(fx, 2f);
        }

        Destroy(gameObject);
    }
}
