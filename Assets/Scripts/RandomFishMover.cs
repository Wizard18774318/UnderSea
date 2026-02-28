using UnityEngine;

public class RandomFishMover : MonoBehaviour
{
    private Vector2 moveDir;
    private float speed;
    private Vector2 targetPos;

    public void Init(Vector2 direction, float fishSpeed, Vector2 destination)
    {
        moveDir = direction;
        speed = fishSpeed;
        targetPos = destination;
    }

    void Update()
    {
        transform.Translate(moveDir * speed * Time.deltaTime, Space.World);

        if (Vector2.Dot(moveDir, (Vector2)transform.position - targetPos) >= 0f)
        {
            Destroy(gameObject);
        }
    }
}
