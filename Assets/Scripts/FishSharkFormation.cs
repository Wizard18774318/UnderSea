using UnityEngine;

public class FishSharkFormation : MonoBehaviour
{
    [Header("Formation Settings")]
    [SerializeField] private float fishSize = 0.8f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private Vector2 moveDirection = Vector2.left;

    [Header("Shark Fish GameObjects (one per region)")]
    [SerializeField] private GameObject fishBody;
    [SerializeField] private GameObject fishBelly;
    [SerializeField] private GameObject fishFin;
    [SerializeField] private GameObject fishOutline;
    [SerializeField] private GameObject fishStripe;

    private static readonly (int col, int row, int c)[] sharkGrid =
    {
        (10,17,2),(11,17,2),
        (9,16,2),(10,16,2),(11,16,2),(12,16,2),
        (8,15,2),(9,15,2),(10,15,2),(11,15,2),(12,15,2),(13,15,2),
        (8,14,2),(9,14,2),(10,14,2),(11,14,2),(12,14,2),(13,14,2),(14,14,2),
        (8,13,0),(9,13,0),(10,13,0),(11,13,0),(12,13,0),(13,13,0),(14,13,0),

        (26,16,2),(27,16,2),(28,16,2),
        (25,15,2),(26,15,2),(27,15,2),(28,15,2),(29,15,2),
        (24,14,2),(25,14,2),(26,14,2),(27,14,2),(28,14,2),
        (23,13,2),(24,13,2),(25,13,2),(26,13,2),(27,13,2),

        (0,12,3),(1,12,0),(2,12,0),(3,12,0),(4,12,0),(5,12,0),(6,12,0),(7,12,0),
        (8,12,0),(9,12,0),(10,12,0),(11,12,0),(12,12,0),(13,12,0),(14,12,0),
        (15,12,0),(16,12,0),(17,12,0),(18,12,0),(19,12,0),(20,12,0),(21,12,0),(22,12,0),
        (23,12,2),(24,12,2),(25,12,2),(26,12,2),(27,12,2),

        (0,11,3),(1,11,0),(2,11,0),(3,11,0),(4,11,0),(5,11,0),(6,11,0),(7,11,0),
        (8,11,4),(9,11,4),(10,11,4),(11,11,0),(12,11,0),(13,11,0),(14,11,0),
        (15,11,0),(16,11,0),(17,11,0),(18,11,0),(19,11,0),(20,11,0),(21,11,0),(22,11,0),
        (23,11,2),(24,11,2),(25,11,2),(26,11,2),(27,11,2),

        (0,10,3),(1,10,0),(2,10,0),(3,10,0),(4,10,0),(5,10,0),(6,10,0),(7,10,0),
        (8,10,0),(9,10,0),(10,10,0),(11,10,0),(12,10,0),(13,10,0),(14,10,0),
        (15,10,0),(16,10,0),(17,10,0),(18,10,0),(19,10,0),(20,10,0),(21,10,0),(22,10,0),
        (23,10,2),(24,10,2),(25,10,2),(26,10,2),(27,10,2),

        (0,9,3),(1,9,0),(2,9,0),(3,9,0),(4,9,0),(5,9,0),(6,9,0),(7,9,0),
        (8,9,0),(9,9,0),(10,9,0),(11,9,0),(12,9,0),(13,9,0),(14,9,0),
        (15,9,0),(16,9,0),(17,9,0),(18,9,0),(19,9,0),(20,9,0),(21,9,0),(22,9,0),
        (23,9,2),(24,9,2),(25,9,2),(26,9,2),

        (0,8,3),(1,8,0),(2,8,0),
        (3,8,1),(4,8,1),(5,8,1),(6,8,1),(7,8,1),(8,8,1),(9,8,1),(10,8,1),
        (11,8,1),(12,8,1),(13,8,1),(14,8,1),(15,8,1),(16,8,1),(17,8,1),(18,8,1),(19,8,1),
        (20,8,0),(21,8,0),(22,8,0),(23,8,2),(24,8,2),(25,8,2),

        (0,7,3),(1,7,0),(2,7,0),
        (3,7,1),(4,7,1),(5,7,1),(6,7,1),(7,7,1),(8,7,1),(9,7,1),(10,7,1),
        (11,7,1),(12,7,1),(13,7,1),(14,7,1),(15,7,1),(16,7,1),(17,7,1),(18,7,1),
        (19,7,0),(20,7,0),(21,7,0),(22,7,2),(23,7,2),(24,7,2),

        (1,6,0),(2,6,0),
        (3,6,1),(4,6,1),(5,6,1),(6,6,1),(7,6,1),(8,6,1),(9,6,1),(10,6,1),
        (11,6,1),(12,6,1),(13,6,1),(14,6,1),(15,6,1),
        (16,6,0),(17,6,0),(18,6,0),(19,6,2),(20,6,2),(21,6,2),

        (8,5,2),(9,5,2),(10,5,2),(11,5,2),(12,5,2),
        (9,4,2),(10,4,2),(11,4,2),(12,4,2),(13,4,2),
        (10,3,2),(11,3,2),(12,3,2),

        (19,5,2),(20,5,2),(21,5,2),(22,5,2),
        (20,4,2),(21,4,2),(22,4,2),(23,4,2),

        (21,3,2),(22,3,2),(23,3,2),(24,3,2),(25,3,2),(26,3,2),
        (22,2,2),(23,2,2),(24,2,2),(25,2,2),(26,2,2),(27,2,2),
        (23,1,2),(24,1,2),(25,1,2),(26,1,2),(27,1,2),
        (24,0,2),(25,0,2),(26,0,2),(27,0,2),
    };

    void Start()
    {
        if (fishBody == null || fishBelly == null || fishFin == null || fishOutline == null || fishStripe == null)
        {
            Debug.LogWarning("FishSharkFormation: assign all 5 fish GameObjects in the Inspector.");
            return;
        }

        GameObject[] palette = { fishBody, fishBelly, fishFin, fishOutline, fishStripe };

        const float colOffset = 14f;
        const float rowOffset = 9f;

        foreach (var (col, row, ci) in sharkGrid)
        {
            float x = (col - colOffset) * fishSize;
            float y = (row - rowOffset) * fishSize;

            GameObject fish = Instantiate(palette[ci],
                transform.position + new Vector3(x, y, 0f),
                Quaternion.identity, transform);

            Vector2 dir = moveDirection.normalized;
            float scaleX = dir.x < 0f ? -fishSize : fishSize;
            fish.transform.localScale = new Vector3(scaleX, fishSize, 1f);

            Vector2 absDir = new Vector2(Mathf.Abs(dir.x), dir.y);
            float angle = Mathf.Atan2(absDir.y, absDir.x) * Mathf.Rad2Deg;
            fish.transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    void Update()
    {
        transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);
    }
}

