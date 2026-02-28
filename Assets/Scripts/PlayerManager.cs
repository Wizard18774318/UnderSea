using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    [SerializeField] private int maxHp = 12;
    [SerializeField] private float invincibilityDuration = 1.5f;

    private int currentHp;
    private bool isInvincible;
    private float invincibilityTimer;

    void Start()
    {
        currentHp = maxHp;
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer <= 0f)
            {
                isInvincible = false;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Enemy_Projectile") && !isInvincible)
        {
            TakeDamage(4);
        }
    }

    private void TakeDamage(int amount)
    {
        currentHp -= amount;
        Debug.Log($"Player HP: {currentHp}/{maxHp}");

        isInvincible = true;
        invincibilityTimer = invincibilityDuration;

        if (currentHp <= 0)
        {
            Debug.Log("Player died!");
            Destroy(gameObject);
        }
    }
}

