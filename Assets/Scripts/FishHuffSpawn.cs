using UnityEngine;

public class FishHuffSpawn : MonoBehaviour
{
    [SerializeField] private bool spawnEnabled = true;
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private float spawnInterval = 2f;
    [SerializeField] private float fishSpeed = 5f;
    [SerializeField] private float screenPadding = 1f;

    private Camera mainCam;

    void Start()
    {
        mainCam = Camera.main;
        InvokeRepeating(nameof(SpawnFish), 0f, spawnInterval);
    }

    private void SpawnFish()
    {
        if (!spawnEnabled) return;
        if (fishPrefab == null) return;

        float halfH = mainCam.orthographicSize;
        float halfW = halfH * mainCam.aspect;
        Vector3 camPos = mainCam.transform.position;

        int side = Random.Range(0, 4);
        Vector2 spawnPos;
        Vector2 targetPos;

        switch (side)
        {
            case 0:
                spawnPos  = new Vector2(camPos.x - halfW - screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                targetPos = new Vector2(camPos.x + halfW + screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 1:
                spawnPos  = new Vector2(camPos.x + halfW + screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                targetPos = new Vector2(camPos.x - halfW - screenPadding, Random.Range(camPos.y - halfH, camPos.y + halfH));
                break;
            case 2:
                spawnPos  = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH - screenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH + screenPadding);
                break;
            default:
                spawnPos  = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y + halfH + screenPadding);
                targetPos = new Vector2(Random.Range(camPos.x - halfW, camPos.x + halfW), camPos.y - halfH - screenPadding);
                break;
        }

        Vector2 dir = (targetPos - spawnPos).normalized;

        GameObject fish = Instantiate(fishPrefab, new Vector3(spawnPos.x, spawnPos.y, 0f), Quaternion.identity);

        FishMover mover = fish.GetComponent<FishMover>();
        if (mover != null)
            mover.Init(dir, fishSpeed);
        else
            Debug.LogWarning("FishHuffSpawn: fishPrefab has no FishMover component.");
    }
}

