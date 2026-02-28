using System.Collections;
using UnityEngine;

/// <summary>
/// Handles player-side game logic: invincibility frames, oxygen passive drain,
/// suffocation, and incoming damage. All actual HP / oxygen values live in
/// PlayerStatsManager so any HUD or system can read them globally.
/// </summary>
public class PlayerManager : MonoBehaviour
{
    [Header("Invincibility")]
    [SerializeField] private float invincibilityDuration = 1.5f;

    [Header("Damage")]
    [SerializeField] private int projectileDamage = 4;   // 4 quarters = 1 full heart
    [SerializeField] private int contactDamage = 4;   // boss body contact
    [SerializeField] private int suffocationDamage = 1;   // 1 quarter-heart per tick

    [Header("Oxygen Drain")]
    [SerializeField] private float oxygenDrainRate = 10f; // oxygen per second
    [SerializeField] private float suffocationDamageInterval = 1f;

    [Header("Blink on Hit")]
    [SerializeField] private float blinkInterval = 0.08f;

    [Header("Death Float")]
    [SerializeField] private float deathBobAmplitude = 0.15f;
    [SerializeField] private float deathBobSpeed = 1.5f;

    [Header("Oxygen Tint")]
    [SerializeField] private SpriteRenderer[] tintTargets;
    [SerializeField] private Color fullOxygenColor = Color.white;
    [SerializeField] private Color lowOxygenColor = new Color(0.4f, 0.65f, 0.89f);

    private bool isInvincible;
    private bool isDead;
    private float invincibilityTimer;
    private float suffocationTimer;
    private Coroutine _blinkCoroutine;
    private SpriteRenderer[] _blinkRenderers;

    private void Start()
    {
        suffocationTimer = suffocationDamageInterval;

        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnPlayerDied += HandleDeath;
            PlayerStatsManager.Instance.OnOxygenChanged += UpdateOxygenTint;
            UpdateOxygenTint();
        }
    }

    private void OnDestroy()
    {
        if (PlayerStatsManager.Instance != null)
        {
            PlayerStatsManager.Instance.OnPlayerDied -= HandleDeath;
            PlayerStatsManager.Instance.OnOxygenChanged -= UpdateOxygenTint;
        }
    }

    private void Update()
    {
        if (isDead) return;

        // Invincibility countdown
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
                StopBlinking();
            }
        }

        if (PlayerStatsManager.Instance == null) return;

        // Passive oxygen drain
        PlayerStatsManager.Instance.DrainOxygen(oxygenDrainRate * Time.deltaTime);

        // Suffocation when out of oxygen
        if (PlayerStatsManager.Instance.CurrentOxygen <= 0f)
        {
            suffocationTimer -= Time.deltaTime;
            if (suffocationTimer <= 0f)
            {
                suffocationTimer = suffocationDamageInterval;
                PlayerStatsManager.Instance.TakeDamage(suffocationDamage);
                Debug.Log("Player is suffocating!");
            }
        }
        else
        {
            suffocationTimer = suffocationDamageInterval;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        Debug.Log($"[Player] OnTriggerEnter2D fired — other: '{other.gameObject.name}' tag: '{other.tag}' hasBossMovement: {other.GetComponentInParent<BossFishMovement>() != null} isInvincible: {isInvincible}");

        if (isInvincible) return;

        if (other.CompareTag("Enemy_Projectile"))
        {
            TakeDamage(projectileDamage);
        }
        else if (other.GetComponentInParent<BossFishMovement>() != null)
        {
            Debug.Log("[Player] Hit by boss fish body!");
            TakeDamage(contactDamage);
        }
        else if (other.GetComponentInParent<UndeadPirateMovement>() != null)
        {
            Debug.Log("[Player] Hit by pirate body!");
            TakeDamage(contactDamage);
        }
    }

    /// <summary>Called by enemies that handle their own trigger detection (e.g. Pirate lunge).
    /// Goes through invincibility frames so the player can't be double-hit.</summary>
    public bool TakeMeleeDamage(float amount)
    {
        if (isDead || isInvincible) return false;
        TakeDamage((int)amount);
        return true;
    }

    /// <summary>Called by OxygenBubble while the player is inside.</summary>
    public void GainOxygen(float amount)
    {
        PlayerStatsManager.Instance?.GainOxygen(amount);
    }

    private void TakeDamage(int amount)
    {
        isInvincible = true;
        invincibilityTimer = invincibilityDuration;
        StartBlinking();
        float hpBefore = PlayerStatsManager.Instance?.CurrentHp ?? 0f;
        PlayerStatsManager.Instance?.TakeDamage(amount);
        float hpAfter = PlayerStatsManager.Instance?.CurrentHp ?? 0f;
        Debug.Log($"[Player] Took {amount} damage — HP: {hpBefore} → {hpAfter}");
    }

    private SpriteRenderer[] GetBlinkRenderers()
    {
        if (tintTargets != null && tintTargets.Length > 0) return tintTargets;
        return GetComponentsInChildren<SpriteRenderer>();
    }

    private void StartBlinking()
    {
        if (_blinkCoroutine != null) StopCoroutine(_blinkCoroutine);
        _blinkRenderers = GetBlinkRenderers();
        _blinkCoroutine = StartCoroutine(BlinkCoroutine());
    }

    private void StopBlinking()
    {
        if (_blinkCoroutine != null)
        {
            StopCoroutine(_blinkCoroutine);
            _blinkCoroutine = null;
        }
        if (_blinkRenderers != null)
            foreach (var sr in _blinkRenderers)
                if (sr != null) sr.enabled = true;
    }

    private IEnumerator BlinkCoroutine()
    {
        bool visible = true;
        while (true)
        {
            visible = !visible;
            if (_blinkRenderers != null)
                foreach (var sr in _blinkRenderers)
                    if (sr != null) sr.enabled = visible;
            yield return new WaitForSeconds(blinkInterval);
        }
    }

    private void HandleDeath()
    {
        if (isDead) return;
        isDead = true;
        Debug.Log("Player died!");

        StopBlinking();

        // Disable gameplay components so the player no longer moves or shoots
        var movement = GetComponent<PlayerMovement>();
        if (movement != null) movement.enabled = false;

        var actions = GetComponent<PlayerActions>();
        if (actions != null) actions.enabled = false;
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        foreach (var col in GetComponentsInChildren<Collider2D>())
            col.enabled = false;

        // Rotate player so animation is ready for this direction
        transform.rotation = Quaternion.Euler(0f, 0f, 0f);

        // Trigger death animation
        var animator = GetComponent<Animator>();
        if (animator != null)
            animator.SetTrigger("isDead");

        // Gentle floating bob
        StartCoroutine(DeathBobCoroutine());
    }

    private IEnumerator DeathBobCoroutine()
    {
        Vector3 origin = transform.position;
        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * deathBobSpeed;
            transform.position = origin + Vector3.up * Mathf.Sin(t) * deathBobAmplitude;
            yield return null;
        }
    }

    private void UpdateOxygenTint()
    {
        if (tintTargets == null || tintTargets.Length == 0 || PlayerStatsManager.Instance == null)
            return;

        float max = Mathf.Max(0.0001f, PlayerStatsManager.Instance.MaxOxygen);
        float fraction = Mathf.Clamp01(PlayerStatsManager.Instance.CurrentOxygen / max);
        Color targetColor = Color.Lerp(lowOxygenColor, fullOxygenColor, fraction);

        for (int i = 0; i < tintTargets.Length; i++)
        {
            if (tintTargets[i] != null)
                tintTargets[i].color = targetColor;
        }
    }
}


