using UnityEngine;

public class SpawnOxygenBubbles : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private bool spawnEnabled = true;
    [SerializeField] private GameObject oxygenBubblePrefab;
    [SerializeField] private float spawnInterval = 8f;     // seconds between spawns
    [SerializeField] private int maxBubblesAtOnce = 5;

    [Header("Bubble Settings")]
    [SerializeField] private float bubbleOxygenCapacity = 60f;
    [SerializeField] private float bubbleCapacityMinMultiplier = 0.75f;  // smallest bubble relative to base
    [SerializeField] private float bubbleCapacityMaxMultiplier = 1.5f;   // biggest bubble relative to base
    [SerializeField] private float bubbleOxygenRate = 20f; // oxygen per second given to player

    private float spawnTimer;
    private int currentBubbleCount;

    void Start()
    {
        spawnTimer = spawnInterval;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            spawnTimer = spawnInterval;
            TrySpawnBubble();
        }
    }

    private void TrySpawnBubble()
    {
        if (!spawnEnabled) return;
        if (currentBubbleCount >= maxBubblesAtOnce) return;
        if (oxygenBubblePrefab == null)
        {
            Debug.LogWarning("SpawnOxygenBubbles: No bubble prefab assigned!");
            return;
        }

        // Spawn just below the bottom edge of the camera viewport
        Camera cam = Camera.main;
        Vector3 bottomLeft  = cam.ViewportToWorldPoint(new Vector3(0f, 0f, cam.nearClipPlane));
        Vector3 bottomRight = cam.ViewportToWorldPoint(new Vector3(1f, 0f, cam.nearClipPlane));

        float randomX = Random.Range(bottomLeft.x, bottomRight.x);
        float spawnY  = bottomLeft.y - 1f; // 1 unit below screen so it's not visible yet
        Vector3 spawnPos = new Vector3(randomX, spawnY, 0f);

        GameObject bubble = Instantiate(oxygenBubblePrefab, spawnPos, Quaternion.identity);

        OxygenBubble bubbleScript = bubble.GetComponent<OxygenBubble>();
        if (bubbleScript != null)
        {
            float bubbleOxygenCapacity_random = bubbleOxygenCapacity * Random.Range(bubbleCapacityMinMultiplier, bubbleCapacityMaxMultiplier);
            bubbleScript.Init(bubbleOxygenCapacity_random, bubbleOxygenCapacity, bubbleOxygenRate, OnBubbleDepleted);
        }

        currentBubbleCount++;
    }

    private void OnBubbleDepleted()
    {
        currentBubbleCount = Mathf.Max(0, currentBubbleCount - 1);
    }
}
