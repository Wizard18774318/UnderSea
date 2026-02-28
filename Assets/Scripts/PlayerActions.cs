using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spawnOffset = 0.5f;
    [SerializeField] private float fireCooldown = 0.2f;
    [SerializeField] private float velocityInheritance = 1f;

    private Vector2 lastMoveDir = Vector2.right;
    private float nextFireTime;

    private Animator animator;
    private float lastDirectionX = 1f;
    private PlayerMovement playerMovement;

    void Awake()
    {
        animator = GetComponent<Animator>();
        playerMovement = GetComponent<PlayerMovement>() ?? GetComponentInParent<PlayerMovement>();
    }

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(moveX, moveY);

        if (input.sqrMagnitude > 0f)
        {
            lastMoveDir = SnapTo8Directions(input.normalized);
        }

        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        float mouseDirX = mouseWorldPos.x - transform.position.x;
        if (Mathf.Abs(mouseDirX) > 0.01f)
        {
            lastDirectionX = Mathf.Sign(mouseDirX);
        }

        animator.SetFloat("dir", lastDirectionX);

        bool shooting = Input.GetMouseButton(0);
        animator.SetBool("isShooting", shooting);

        if (shooting && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0f;
        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector2 dir = ((Vector2)(mouseWorld - origin)).normalized;
        if (dir == Vector2.zero) dir = lastMoveDir;

        Vector3 spawnPos = origin;
        spawnPos += (Vector3)(dir * spawnOffset);

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        ProjectileMovement pm = projectile.GetComponent<ProjectileMovement>();
        if (pm != null)
        {
            Vector2 inheritedVelocity = playerMovement != null
                ? playerMovement.CurrentVelocity * velocityInheritance
                : Vector2.zero;
            pm.Init(dir, projectileSpeed, inheritedVelocity);
        }
    }

    private Vector2 SnapTo8Directions(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float snapped = Mathf.Round(angle / 45f) * 45f;
        float rad = snapped * Mathf.Deg2Rad;
        return new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)).normalized;
    }
}

