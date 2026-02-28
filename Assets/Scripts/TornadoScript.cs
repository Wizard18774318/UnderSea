using System.Collections;
using UnityEngine;

/// <summary>
/// Single tornado pillar for the Pirate boss tornado attack.
/// Spawn two of these (one from the left edge, one from the right edge).
/// Each moves toward the centre, stops to leave a triangle safe zone, holds, then retreats.
///
/// Call Init() immediately after Instantiate to configure direction and gap position.
/// </summary>
public class TornadoScript : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed    = 5f;
    [SerializeField] private float holdDuration = 3f;

    [Header("Damage")]
    [SerializeField] private float damage         = 2f;
    [SerializeField] private float damageInterval = 0.5f;

    // --------------- runtime ---------------
    private float  _targetX;         // X to stop at
    private float  _retreatX;        // X to fly off-screen to
    private float  _damageTimer;
    private bool   _retreating;
    private bool   _holding;

    /// <summary>
    /// Configure after spawning.
    /// targetX  = world-X this tornado should stop at
    /// retreatX = world-X far off screen to fly toward when retreating
    /// </summary>
    public void Init(float targetX, float retreatX)
    {
        _targetX  = targetX;
        _retreatX = retreatX;
        StartCoroutine(LifeCycle());
    }

    private IEnumerator LifeCycle()
    {
        // Phase 1: advance toward centre gap
        _holding = false;
        int sign = (_retreatX > _targetX) ? -1 : 1; // moving left or right

        while (sign * (transform.position.x - _targetX) < 0f)
        {
            transform.position += Vector3.right * sign * moveSpeed * Time.deltaTime;
            yield return null;
        }

        // Snap to stop position
        transform.position = new Vector3(_targetX, transform.position.y, transform.position.z);

        // Phase 2: hold
        _holding = true;
        yield return new WaitForSeconds(holdDuration);
        _holding = false;

        // Phase 3: retreat off-screen
        _retreating = true;
        int retreatSign = (_retreatX > transform.position.x) ? 1 : -1;

        while (true)
        {
            transform.position += Vector3.right * retreatSign * moveSpeed * Time.deltaTime;
            // check if off-screen
            Camera cam = Camera.main;
            if (cam != null)
            {
                float halfW = cam.orthographicSize * cam.aspect + 3f;
                float cx    = cam.transform.position.x;
                if (transform.position.x < cx - halfW || transform.position.x > cx + halfW)
                    break;
            }
            yield return null;
        }

        Destroy(gameObject);
    }

    private void Update()
    {
        if (!_holding) return;
        _damageTimer -= Time.deltaTime;
        if (_damageTimer <= 0f) _damageTimer = 0f; // reset happens in trigger
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!_holding) return;
        if (other.GetComponent<PlayerManager>() == null) return;

        _damageTimer += Time.deltaTime;
        if (_damageTimer >= damageInterval)
        {
            PlayerStatsManager.Instance?.TakeDamage(damage);
            _damageTimer = 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Damage player the instant the tornado touches them (on enter)
        if (other.GetComponent<PlayerManager>() != null)
        {
            PlayerStatsManager.Instance?.TakeDamage(damage);
            _damageTimer = 0f;
        }
    }
}
