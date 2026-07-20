using UnityEngine;
using TMPro; // Required for 3D Text

public class PositiveThought : MonoBehaviour
{
    [Header("Collectible Settings")]
    public float speed = 5f;
    public float healAmount = 25f; 
    public Vector3 movementDirection = new Vector3(0, 0, -1);
    public float hitRadius = 0.8f;
    public AudioClip collectSound;

    [Header("Dynamic Typography")]
    [Tooltip("Drag the TextMeshPro 3D component on this prefab here")]
    public TMP_Text thoughtText;
    [Tooltip("Add all the phrases you want to randomly appear!")]
    public string[] positivePhrases = {
        "Breathe",
        "I am safe",
        "This will pass",
        "I am in control",
        "Keep moving"
    };

    [Header("Animation & Positioning")]
    [Tooltip("How much higher to place the text above its normal spawn point.")]
    public float heightOffset = 1.0f;
    [Tooltip("How fast it bobs up and down.")]
    public float bobSpeed = 2f; 
    [Tooltip("How far up and down it travels.")]
    public float bobHeight = 0.2f;

    private Transform playerCamera;
    private bool hasCollected = false; 
    private float baseY; // Tracks the center point for the bobbing math

    void Start()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        // ---> NEW: Raise the text higher right when it spawns
        transform.position += new Vector3(0, heightOffset, 0);
        
        // Save this new height to use as the baseline for the bobbing calculation
        baseY = transform.position.y; 

        // Randomly pick a phrase when this object spawns!
        if (thoughtText != null && positivePhrases.Length > 0)
        {
            int randomIndex = Random.Range(0, positivePhrases.Length);
            thoughtText.text = positivePhrases[randomIndex];
        }
    }

    void Update()
    {
        // ---> NEW: The Floating (Bobbing) Math
        // 1. Calculate the standard forward movement
        Vector3 nextPos = transform.position + (movementDirection.normalized * speed * Time.deltaTime);
        
        // 2. Override the Y position using a Sine wave for a smooth floating effect
        nextPos.y = baseY + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        
        // 3. Apply the final position
        transform.position = nextPos;

        // 3. Collision Logic
        if (playerCamera != null)
        {
            if (!hasCollected)
            {
                // We only check X and Z (flat) distance so the new height doesn't break the collection!
                Vector2 objFlat = new Vector2(transform.position.x, transform.position.z);
                Vector2 playerFlat = new Vector2(playerCamera.position.x, playerCamera.position.z);

                if (Vector2.Distance(objFlat, playerFlat) <= hitRadius)
                {
                    hasCollected = true;
                    
                    if (AnxietyMinigameManager.Instance != null)
                    {
                        AnxietyMinigameManager.Instance.ModifyAnxiety(healAmount);
                    }

                    if (collectSound != null)
                    {
                        AudioSource.PlayClipAtPoint(collectSound, playerCamera.position);
                    }

                    Destroy(gameObject);
                    return; 
                }
            }

            // Cleanup behind player
            if (movementDirection.z < 0 && transform.position.z < playerCamera.position.z - 3f) Destroy(gameObject);
        }
        else Destroy(gameObject, 15f); 
    }
}