using System.Linq;
using UnityEngine;

public class UndeadPirateManager : MonoBehaviour
{
    [SerializeField] private float maxHp = 10f;
    [SerializeField] private GameObject fishAttackLeftPrefab;
    [SerializeField] private GameObject fishAttackRightPrefab;

    [SerializeField] private GameObject fishRandomConstantAttackPrefab;


    private float currentHp;
    private GameObject[] spawnedAttack = new GameObject[3];

    void Start()
    {
        currentHp = maxHp;

        if (fishAttackLeftPrefab != null)
        {
            spawnedAttack[0] = Instantiate(fishAttackLeftPrefab, transform.position, Quaternion.identity);
        }
        if (fishAttackRightPrefab != null)
        {
            spawnedAttack[1] = Instantiate(fishAttackRightPrefab, transform.position, Quaternion.identity);
        }
        if (fishRandomConstantAttackPrefab != null)
        {
            spawnedAttack[2] = Instantiate(fishRandomConstantAttackPrefab, transform.position, Quaternion.identity);
        }
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
            if (spawnedAttack != null && spawnedAttack.Length > 0)
            {
                foreach (GameObject attack in spawnedAttack.Where(a => a != null))
                {
                    Destroy(attack);
                }
            }

            Destroy(gameObject);
        }
    }
}

