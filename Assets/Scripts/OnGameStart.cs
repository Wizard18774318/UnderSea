using UnityEngine;

public class OnGameStart : MonoBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Vector3 playerSpawnPosition = Vector3.zero;
    [SerializeField] private bool spawnBoss = true;
    [SerializeField] private GameObject bossPrefab;
    [SerializeField] private Vector3 bossSpawnPosition = Vector3.zero;
    [SerializeField] private GameObject objectDestroyerPrefab;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    void Start()
    {
        if (playerPrefab != null)
        {
            Instantiate(playerPrefab, playerSpawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No player prefab assigned to spawn.");
        }

        if (spawnBoss && bossPrefab != null)
        {
            Instantiate(bossPrefab, bossSpawnPosition, Quaternion.identity);
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
    }
}
