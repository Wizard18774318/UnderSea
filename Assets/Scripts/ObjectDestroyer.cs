using UnityEngine;

public class ObjectDestroyerColllisionDetection : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        Destroy(other.gameObject);
    }
}

