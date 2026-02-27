using UnityEngine;

public class ProjectileMovement : MonoBehaviour
{
    private Vector2 moveDir;
    private float moveSpeed;
    [SerializeField] private float damageAmount = 2.5f;

    public void Init(Vector2 direction, float speed)
    {
        moveDir = direction;
        moveSpeed = speed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 90f; 
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);
    }
    

    public float getDamageAmount()
    {
        return damageAmount;
    }
}
