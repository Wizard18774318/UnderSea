using UnityEngine;

public class FishSideAttackScript : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject fishPrefab;
    [SerializeField] private int fishCount = 5;
    [SerializeField] private float verticalSpacing = 1.5f;
    [SerializeField] private float spawnX = -10f;
    [SerializeField] private float centerY = 0f;

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

        for (int i = 0; i < fishCount; i++)
        {
            float y = startY + i * verticalSpacing;
            Vector3 spawnPos = new Vector3(spawnX, y, 0f);
            GameObject fish = Instantiate(fishPrefab, spawnPos, Quaternion.identity);

            FishMover mover = fish.GetComponent<FishMover>();
            if (mover != null)
            {
                mover.Init(moveDirection.normalized, fishSpeed);
            }
        }
    }
}

