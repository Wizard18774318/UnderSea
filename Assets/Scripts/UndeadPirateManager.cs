using System.Linq;
using UnityEngine;

public class UndeadPirateManager : MonoBehaviour
{
    [SerializeField] private float maxHp = 10f;

    [Header("Attack Spawns")]
    [SerializeField] private GameObject fishAttackLeftPrefab;
    [SerializeField] private bool spawnFishAttackLeft = true;

    [SerializeField] private GameObject fishAttackRightPrefab;
    [SerializeField] private bool spawnFishAttackRight = true;

    [SerializeField] private GameObject fishRandomConstantAttackPrefab;
    [SerializeField] private bool spawnFishRandomConstantAttack = true;

    [Header("Spawn Position Noise")]
    [SerializeField] private float spawnNoiseRadius = 1.5f;  // max random offset from pirate position

    private float currentHp;
    private GameObject[] spawnedAttack = new GameObject[3];

    private Vector3 RandomOffset() =>
        (Vector3)Random.insideUnitCircle * spawnNoiseRadius;

    void Start()
    {
        currentHp = maxHp;

        if (fishAttackLeftPrefab != null && spawnFishAttackLeft)
        {
            spawnedAttack[0] = Instantiate(fishAttackLeftPrefab, transform.position + RandomOffset(), Quaternion.identity);
        }
        if (fishAttackRightPrefab != null && spawnFishAttackRight)
        {
            spawnedAttack[1] = Instantiate(fishAttackRightPrefab, transform.position + RandomOffset(), Quaternion.identity);
        }
        if (fishRandomConstantAttackPrefab != null && spawnFishRandomConstantAttack)
        {
            spawnedAttack[2] = Instantiate(fishRandomConstantAttackPrefab, transform.position + RandomOffset(), Quaternion.identity);
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

