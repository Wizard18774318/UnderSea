using UnityEngine;

public class OnGameStart : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private GameObject objectDestroyerPrefab;
    [SerializeField] private GameObject fishHuffSpawnPrefab;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    void Start()
    {
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No player prefab assigned to spawn.");
        }

        if (bossPrefab != null)
        {
            Instantiate(bossPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No boss prefab assigned to spawn.");
        }

        if (objectDestroyerPrefab != null)
        {
            Instantiate(objectDestroyerPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No object destroyer prefab assigned to spawn.");
        }

        if (fishHuffSpawnPrefab != null)
        {
            Instantiate(fishHuffSpawnPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No fish huff spawn prefab assigned to spawn.");
        }
    }
}
