using UnityEngine;

public class FishMover : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;

    public void Init(Vector2 direction, float fishSpeed)
    {
        moveDir = direction;
        speed = fishSpeed;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void Update()
    {
        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            // player damage is handled by PlayerManager on the player side
            Destroy(gameObject);
        }
    }
}
