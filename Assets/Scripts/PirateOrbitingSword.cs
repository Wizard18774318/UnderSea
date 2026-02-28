using UnityEngine;

/// <summary>
/// Orbits around its parent transform (the Pirate boss).
/// Destroys player Projectiles it touches. Spawned as a child of the boss
/// so it is automatically cleaned up when the boss dies.
/// </summary>
public class PirateOrbitingSword : MonoBehaviour
{
    // Set via Init(); can also be tweaked in Inspector on the prefab
    [SerializeField] private float orbitRadius      = 2.5f;
    [SerializeField] private float orbitSpeed        = 200f; // degrees per second
    [SerializeField] private float spriteAngleOffset = -123.09f; // offset so sprite faces orbit direction

    private float _angle;

    /// <param name="startAngleDeg">Initial angle offset in degrees so multiple swords spread evenly.</param>
    /// <param name="speedVariance">±variance added to orbitSpeed for randomness.</param>
    public void Init(float startAngleDeg, float speedVariance = 20f)
    {
        _angle      = startAngleDeg;
        orbitSpeed += Random.Range(-speedVariance, speedVariance);
    }

    private void Update()
    {
        if (transform.parent == null) { Destroy(gameObject); return; }

        _angle += orbitSpeed * Time.deltaTime;
        float rad = _angle * Mathf.Deg2Rad;
        // localPosition keeps it relative to the boss
        transform.localPosition = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * orbitRadius;
        transform.localRotation = Quaternion.Euler(0f, 0f, _angle + spriteAngleOffset);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Destroy player projectiles
        if (other.CompareTag("Projectile"))
            Destroy(other.gameObject);
    }
}
