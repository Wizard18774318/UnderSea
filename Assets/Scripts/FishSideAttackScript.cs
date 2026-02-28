using UnityEngine;

public class FishSideAttackScript : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private Sprite[] fishSprites;
    [SerializeField] private int fishCount = 7;
    [SerializeField] private float verticalSpacing = 1.5f;
    [SerializeField] private float spawnX = -10f;
    [SerializeField] private float centerY = 0f;

    [Header("Gap Settings")]
    [SerializeField] private int gapSize = 2;

    [Header("Movement Settings")]
    [SerializeField] private float fishSpeed = 6f;
    [SerializeField] private Vector2 moveDirection = Vector2.right;

    [Header("Timing")]
    [SerializeField] private float repeatInterval = 4f;

    void Start()
    {
        InvokeRepeating(nameof(SpawnWave), 0f, repeatInterval);
    }

    private void SpawnWave()
    {
        float totalHeight = (fishCount - 1) * verticalSpacing;
        float startY = centerY - totalHeight / 2f;

        int gapStart = Random.Range(0, fishCount - gapSize + 1);

        for (int i = 0; i < fishCount; i++)
        {
            if (i >= gapStart && i < gapStart + gapSize)
                continue;

            float y = startY + i * verticalSpacing;
            Vector3 spawnPos = new Vector3(spawnX, y, 0f);
            GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);

            // assign random sprite
            if (fishSprites != null && fishSprites.Length > 0)
            {
                SpriteRenderer sr = fish.GetComponent<SpriteRenderer>();
                if (sr != null)
                    sr.sprite = fishSprites[Random.Range(0, fishSprites.Length)];
            }

            FishMover mover = fish.GetComponent<FishMover>();
            if (mover != null)
            {
                mover.Init(moveDirection.normalized, fishSpeed);
            }
        }
    }
}



