using UnityEngine;

public class UndeadPirateManager : MonoBehaviour
{
    [SerializeField] private float maxHp = 10f;

    private float currentHp;

    void Start()
    {
        currentHp = maxHp;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(other.GetComponent<ProjectileMovement>().getDamageAmount());
            Destroy(other.gameObject);
        }
    }

    private void TakeDamage(float amount)
    {
        currentHp -= amount;
        Debug.Log($"UndeadPirate HP: {currentHp}/{maxHp}");

        if (currentHp <= 0)
        {
            Destroy(gameObject);
        }
    }
}

