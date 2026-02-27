using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed = 10f;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float spawnOffset = 0.5f;
    [SerializeField] private float fireCooldown = 0.2f;

    private Vector2 lastMoveDir = Vector2.right;
    private float nextFireTime;

    void Update()
    {
        float moveX = Input.GetAxisRaw("Horizontal");
        float moveY = Input.GetAxisRaw("Vertical");
        Vector2 input = new Vector2(moveX, moveY);
        if (input.sqrMagnitude > 0f)
        {
            lastMoveDir = SnapTo8Directions(input.normalized);
        }

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            nextFireTime = Time.time + fireCooldown;
            Shoot();
        }
    }

    private void Shoot()
    {
        if (projectilePrefab == null) return;

        Vector2 dir = lastMoveDir;
        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        spawnPos += (Vector3)(dir * spawnOffset);

        GameObject projectile = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);

        ProjectileMovement pm = projectile.GetComponent<ProjectileMovement>();
        if (pm != null)
        {
            pm.Init(dir, projectileSpeed);
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

