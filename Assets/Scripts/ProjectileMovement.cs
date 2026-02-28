using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    private Vector2 moveDir;
    private float moveSpeed;
    private Vector2 inheritedVelocity;
    [SerializeField] private float damageAmount = 2.5f;

    public void Init(Vector2 direction, float speed, Vector2 inheritedVelocity = default)
    {
        moveDir = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        moveSpeed = speed;
        this.inheritedVelocity = inheritedVelocity;

        float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg + 90f; 
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        Vector2 totalVelocity = moveDir * moveSpeed + inheritedVelocity;
        transform.Translate(totalVelocity * Time.deltaTime, Space.World);
    }
    

    public float getDamageAmount()
    {
        return damageAmount;
    }
}
