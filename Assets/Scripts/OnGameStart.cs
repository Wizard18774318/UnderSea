using UnityEngine;

public class OnGameStart : MonoBehaviour
{
    [SerializeField] private GameObject prefabToSpawn;
    [SerializeField] private Vector3 spawnPosition = Vector3.zero;

    void Start()
    {
        if (prefabToSpawn != null)
        {
            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("OnGameStart: No prefab assigned to spawn.");
        }
    }
}
