using System.Linq;
using UnityEngine;

public class BossFishManager : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHp = 30f;

    [Header("Phase 2")]
    [SerializeField] private float phase2HpThreshold = 0.5f;
    [SerializeField] private float phase2ScaleMultiplier = 1.4f;
    [SerializeField] private Color phase2Color = new Color(1f, 0.15f, 0.1f);
    [SerializeField] private float phase2SpeedMultiplier = 1.5f;

    [Header("Attack Spawns")]
    [SerializeField] private GameObject fishAttackLeftPrefab;
    [SerializeField] private bool spawnFishAttackLeft = true;

    [SerializeField] private GameObject fishAttackRightPrefab;
    [SerializeField] private bool spawnFishAttackRight = true;

    [SerializeField] private GameObject fishRandomConstantAttackPrefab;
    [SerializeField] private bool spawnFishRandomConstantAttack = true;

    [Header("Spawn Position Noise")]
    [SerializeField] private float spawnNoiseRadius = 1.5f;

    private float currentHp;
    private bool  _phase2Triggered;
    private GameObject[] spawnedAttack = new GameObject[3];

    public bool IsShielded { get; private set; }

    public void ActivateShield()   { IsShielded = true; }
    public void DeactivateShield() { IsShielded = false; }

    private BossFishMovement _movement;

    private Vector3 RandomOffset() =>
        (Vector3)Random.insideUnitCircle * spawnNoiseRadius;

    private void Start()
    {
        currentHp = maxHp;
        _movement = GetComponent<BossFishMovement>();

        if (fishAttackLeftPrefab != null && spawnFishAttackLeft)
            spawnedAttack[0] = Instantiate(fishAttackLeftPrefab, transform.position + RandomOffset(), Quaternion.identity);

        if (fishAttackRightPrefab != null && spawnFishAttackRight)
            spawnedAttack[1] = Instantiate(fishAttackRightPrefab, transform.position + RandomOffset(), Quaternion.identity);

        if (fishRandomConstantAttackPrefab != null && spawnFishRandomConstantAttack)
            spawnedAttack[2] = Instantiate(fishRandomConstantAttackPrefab, transform.position + RandomOffset(), Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (IsShielded) return;
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(other.GetComponent<ProjectileMovement>().getDamageAmount());
            Destroy(other.gameObject);
        }
    }

    private void TakeDamage(float amount)
    {
        currentHp -= amount;
        Debug.Log($"BossFish HP: {currentHp}/{maxHp}");

        if (!_phase2Triggered && currentHp <= maxHp * phase2HpThreshold)
            TriggerPhase2();

        if (currentHp <= 0)
        {
            foreach (GameObject attack in spawnedAttack.Where(a => a != null))
                Destroy(attack);

            Destroy(gameObject);
        }
    }

    private void TriggerPhase2()
    {
        _phase2Triggered = true;

        transform.localScale *= phase2ScaleMultiplier;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.color = phase2Color;

        if (_movement != null)
            _movement.EnterPhase2();
    }
}