using UnityEngine;

public class RandomFishSpawner : MonoBehaviour
{
    [Header("Prefab & Sprites")]
    [SerializeField] private bool spawnEnabled = true;
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private Sprite[] fishSprites;

    [Header("Spawn Rate")]
    [SerializeField] private float minSpawnInterval = 0.5f;
    [SerializeField] private float maxSpawnInterval = 2f;

    [Header("Speed")]
    [SerializeField] private float minSpeed = 2f;
    [SerializeField] private float maxSpeed = 8f;

    [Header("Scale")]
    [SerializeField] private float minScale = 0.5f;
    [SerializeField] private float maxScale = 1.5f;

    [Header("Screen Bounds")]
    [SerializeField] private float screenPadding = 1f; 

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        ScheduleNextSpawn();
    }

    private void ScheduleNextSpawn()
    {
        float delay = Random.Range(minSpawnInterval, maxSpawnInterval);
        Invoke(nameof(SpawnFish), delay);
    }

    private void SpawnFish()
    {
        if (!spawnEnabled) { ScheduleNextSpawn(); return; }
        if (fishPrefab == null) { ScheduleNextSpawn(); return; }

        int side = Random.Range(0, 4);

        Vector3 camPos = mainCam.transform.position;
        float halfH = mainCam.orthographicSize;
        float halfW = halfH * mainCam.aspect;

        Vector2 spawnPos;
        Vector2 targetPos;

        float randAlongEdge;

        switch (side)
        {
            case 0:
                randAlongEdge = Random.Range(camPos.y - halfH, camPos.y + halfH);
                spawnPos = new Vector2(camPos.x - halfW - screenPadding, randAlongEdge);
                targetPos = new Vector2(camPos.x + halfW + screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 1:
                randAlongEdge = Random.Range(camPos.y - halfH, camPos.y + halfH);
                spawnPos = new Vector2(camPos.x + halfW + screenPadding, randAlongEdge);
                targetPos = new Vector2(camPos.x - halfW - screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 2:
                randAlongEdge = Random.Range(camPos.x - halfW, camPos.x + halfW);
                spawnPos = new Vector2(randAlongEdge, camPos.y - halfH - screenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH + screenPadding);
                break;
            default:
                randAlongEdge = Random.Range(camPos.x - halfW, camPos.x + halfW);
                spawnPos = new Vector2(randAlongEdge, camPos.y + halfH + screenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH - screenPadding);
                break;
        }

        Vector2 dir = (targetPos - spawnPos).normalized;
        float speed = Random.Range(minSpeed, maxSpeed);
        float scale = Random.Range(minScale, maxScale);

        GameObject fish = Instantiate(fishPrefab, new Vector3(spawnPos.x, spawnPos.y, 0f), Quaternion.identity);

        if (fishSprites != null && fishSprites.Length > 0)
        {
            SpriteRenderer sr = fish.GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = fishSprites[Random.Range(0, fishSprites.Length)];
        }

        float scaleX = dir.x < 0f ? -scale : scale;
        fish.transform.localScale = new Vector3(scaleX, scale, 1f);

        Vector2 absDir = new Vector2(Mathf.Abs(dir.x), dir.y);
        float angle = Mathf.Atan2(absDir.y, absDir.x) * Mathf.Rad2Deg;
        fish.transform.rotation = Quaternion.Euler(0f, 0f, angle);

        RandomFishMover mover = fish.GetComponent<RandomFishMover>();
        if (mover != null)
            mover.Init(dir, speed, targetPos);

        ScheduleNextSpawn();
    }
}
