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
    [SerializeField] private int projectileDamage  = 4;   // 4 quarters = 1 full heart
    [SerializeField] private int contactDamage     = 4;   // boss body contact
    [SerializeField] private int suffocationDamage = 1;   // 1 quarter-heart per tick

    [Header("Oxygen Drain")]
    [SerializeField] private float oxygenDrainRate         = 2.5f; // oxygen per second
    [SerializeField] private float suffocationDamageInterval = 1.5f;

    private bool  isInvincible;
    private float invincibilityTimer;
    private float suffocationTimer;

    private void Start()
    {
        suffocationTimer = suffocationDamageInterval;

        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnPlayerDied += HandleDeath;
    }

    private void OnDestroy()
    {
        if (PlayerStatsManager.Instance != null)
            PlayerStatsManager.Instance.OnPlayerDied -= HandleDeath;
    }

    private void Update()
    {
        // Invincibility countdown
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
                isInvincible = false;
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
        if (isInvincible) return false;
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
        isInvincible       = true;
        invincibilityTimer = invincibilityDuration;
        float hpBefore = PlayerStatsManager.Instance?.CurrentHp ?? 0f;
        PlayerStatsManager.Instance?.TakeDamage(amount);
        float hpAfter = PlayerStatsManager.Instance?.CurrentHp ?? 0f;
        Debug.Log($"[Player] Took {amount} damage — HP: {hpBefore} → {hpAfter}");
    }

    private void HandleDeath()
    {
        Debug.Log("Player died!");
        Destroy(gameObject);
    }
}


