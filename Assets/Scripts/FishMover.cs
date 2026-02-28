using UnityEngine;

public class FishMover : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;

    public void Init(Vector2 direction, float fishSpeed)
    {
        moveDir = direction;
        speed = fishSpeed;

        Vector3 scale = transform.localScale;
        scale.x = direction.x < 0f ? -Mathf.Abs(scale.x) : Mathf.Abs(scale.x);
        transform.localScale = scale;

        Vector2 absDir = new Vector2(Mathf.Abs(direction.x), direction.y);
        float angle = Mathf.Atan2(absDir.y, absDir.x) * Mathf.Rad2Deg;
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
            Destroy(gameObject);
        }
    }
}
