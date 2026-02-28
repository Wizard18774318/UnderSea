using System.Linq;
using UnityEngine;

public class UndeadPirateManager : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHp = 30f;

    [Header("Phase Thresholds")]
    [Tooltip("Enter Phase 2 when HP drops to this fraction of max (e.g. 0.75 = lost 25%)")]
    [SerializeField] private float phase2HpFraction = 0.75f;
    [Tooltip("Enter Phase 3 when HP drops to this fraction of max (e.g. 0.34 = lost 66%)")]
    [SerializeField] private float phase3HpFraction = 0.34f;

    [Header("Phase 2 Visual")]
    [SerializeField] private Color phase2Color = new Color(1f, 0.55f, 0f);  // orange

    [Header("Phase 3 Visual")]
    [SerializeField] private Color phase3Color = new Color(1f, 0.1f, 0.1f); // red

    [Header("Attack Spawns")]
    [SerializeField] private GameObject fishAttackLeftPrefab;
    [SerializeField] private bool spawnFishAttackLeft = true;

    [SerializeField] private GameObject fishAttackRightPrefab;
    [SerializeField] private bool spawnFishAttackRight = true;

    [SerializeField] private GameObject fishRandomConstantAttackPrefab;
    [SerializeField] private bool spawnFishRandomConstantAttack = true;

    [Header("Spawn Position Noise")]
    [SerializeField] private float spawnNoiseRadius = 1.5f;

    private float _currentHp;
    private int   _currentPhase = 1;
    private float _damageMultiplier = 1f;

    private GameObject[]         _spawnedAttack = new GameObject[3];
    private UndeadPirateMovement _movement;

    private Vector3 RandomOffset() => (Vector3)Random.insideUnitCircle * spawnNoiseRadius;

    /// <summary>Called by UndeadPirateMovement to apply damage reduction (0.5) or restore (1.0).</summary>
    public void SetDamageMultiplier(float mult) => _damageMultiplier = mult;

    private void Start()
    {
        _currentHp = maxHp;
        _movement  = GetComponent<UndeadPirateMovement>();

        if (fishAttackLeftPrefab != null && spawnFishAttackLeft)
            _spawnedAttack[0] = Instantiate(fishAttackLeftPrefab, transform.position + RandomOffset(), Quaternion.identity);
        if (fishAttackRightPrefab != null && spawnFishAttackRight)
            _spawnedAttack[1] = Instantiate(fishAttackRightPrefab, transform.position + RandomOffset(), Quaternion.identity);
        if (fishRandomConstantAttackPrefab != null && spawnFishRandomConstantAttack)
            _spawnedAttack[2] = Instantiate(fishRandomConstantAttackPrefab, transform.position + RandomOffset(), Quaternion.identity);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Projectile"))
        {
            TakeDamage(other.GetComponent<ProjectileMovement>().getDamageAmount());
            Destroy(other.gameObject);
        }
    }

    private void TakeDamage(float rawAmount)
    {
        float amount = rawAmount * _damageMultiplier;
        _currentHp -= amount;
        Debug.Log($"[Pirate] HP: {_currentHp:F1}/{maxHp}  (x{_damageMultiplier:F2} reduction)");

        CheckPhaseTransitions();

        if (_currentHp <= 0)
            Die();
    }

    private void CheckPhaseTransitions()
    {
        float ratio = _currentHp / maxHp;
        if (_currentPhase < 2 && ratio <= phase2HpFraction)
            TriggerPhase2();
        else if (_currentPhase < 3 && ratio <= phase3HpFraction)
            TriggerPhase3();
    }

    private void TriggerPhase2()
    {
        _currentPhase = 2;
        Debug.Log("[Pirate] *** PHASE 2 ***");
        TintSprites(phase2Color);
        _movement?.EnterPhase2();
    }

    private void TriggerPhase3()
    {
        _currentPhase = 3;
        Debug.Log("[Pirate] *** PHASE 3 ***");
        TintSprites(phase3Color);
        _movement?.EnterPhase3();
    }

    private void TintSprites(Color c)
    {
        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.color = c;
    }

    private void Die()
    {
        foreach (GameObject a in _spawnedAttack.Where(a => a != null))
            Destroy(a);
        Destroy(gameObject);
    }
}

