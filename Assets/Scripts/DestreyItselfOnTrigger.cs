using UnityEngine;

public class ObjectGetsDestroyed : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Destroyer") || (other.CompareTag("Projectile") && gameObject.CompareTag("Enemy_Projectile")))
        {
            Destroy(gameObject);
        }
    }
}

