using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class OxygenBubble : MonoBehaviour
{
    [Header("Defaults (overridden by spawner)")]
    [SerializeField] private float oxygenCapacity = 60f;
    [SerializeField] private float oxygenTransferRate = 20f;

    [Header("Underwater Float")]
    [SerializeField] private float riseSpeed = 1.2f;        // upward speed
    [SerializeField] private float swayAmplitude = 0.3f;    // horizontal sway width
    [SerializeField] private float swayFrequency = 0.8f;    // how fast it sways
    [SerializeField] private float swayPhaseOffset = 0f;    // randomised in Awake

    private float remainingOxygen;
    private float maxOxygen;
    private Vector3 initialScale;
    private PlayerManager playerInside;
    private Action onDepletedCallback;

    private void Awake()
    {
        initialScale = transform.localScale;
        swayPhaseOffset = Random.Range(0f, Mathf.PI * 2f);
    }

    /// <param name="baseCapacity">The spawner's base capacity — used to derive the scale ratio.</param>
    public void Init(float capacity, float baseCapacity, float transferRate, Action onDepleted)
    {
        oxygenCapacity = capacity;
        oxygenTransferRate = transferRate;
        onDepletedCallback = onDepleted;
        remainingOxygen = oxygenCapacity;
        maxOxygen = oxygenCapacity;

        // Scale the bubble so its radius reflects how much oxygen it holds
        float scaleFactor = capacity / Mathf.Max(baseCapacity, 0.001f);
        initialScale = initialScale * scaleFactor;
        transform.localScale = initialScale;
    }

    private void Start()
    {
        if (remainingOxygen == 0f)
        {
            remainingOxygen = oxygenCapacity;
            maxOxygen = oxygenCapacity;
        }
    }

    private void Update()
    {
        // Smooth upward rise with gentle horizontal sway
        float swayX = Mathf.Sin(Time.time * swayFrequency + swayPhaseOffset) * swayAmplitude;
        transform.position += new Vector3(swayX, riseSpeed, 0f) * Time.deltaTime;

        if (playerInside != null)
        {
            float oxygenToGive = oxygenTransferRate * Time.deltaTime;
            oxygenToGive = Mathf.Min(oxygenToGive, remainingOxygen);

            playerInside.GainOxygen(oxygenToGive);
            remainingOxygen -= oxygenToGive;

            // Shrink the bubble proportionally to remaining oxygen
            float t = remainingOxygen / maxOxygen;
            transform.localScale = initialScale * t;

            if (remainingOxygen <= 10f)
            {
                Deplete();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (playerInside == null)
        {
            PlayerManager pm = other.GetComponent<PlayerManager>();
            if (pm != null)
            {
                playerInside = pm;
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (playerInside != null && other.GetComponent<PlayerManager>() == playerInside)
        {
            playerInside = null;
        }
    }

    private void Deplete()
    {
        onDepletedCallback?.Invoke();
        Destroy(gameObject);
    }
}
