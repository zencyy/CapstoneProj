using UnityEngine;
using TMPro; 

public class PositiveThought : MonoBehaviour
{
    [Header("Collectible Settings")]
    public float speed = 5f;
    public float healAmount = 25f; 
    public Vector3 movementDirection = new Vector3(0, 0, -1);
    public float hitRadius = 0.8f;
    public AudioClip collectSound;

    [Header("Dynamic Typography")]
    public TMP_Text thoughtText;
    public string[] positivePhrases = {
        "Breathe",
        "I am safe",
        "This will pass",
        "I am in control",
        "Keep moving"
    };

    [Header("Animation & Positioning")]
    public float heightOffset = 1.0f;
    public float bobSpeed = 2f; 
    public float bobHeight = 0.2f;

    [Header("UI Popup Setup")] // ---> NEW SECTION
    [Tooltip("Drag your new PopupNotificationPrefab here")]
    public GameObject popupPrefab;

    private Transform playerCamera;
    private bool hasCollected = false; 
    private float baseY; 

    void Start()
    {
        if (Camera.main != null) playerCamera = Camera.main.transform;

        transform.position += new Vector3(0, heightOffset, 0);
        baseY = transform.position.y; 

        if (thoughtText != null && positivePhrases.Length > 0)
        {
            int randomIndex = Random.Range(0, positivePhrases.Length);
            thoughtText.text = positivePhrases[randomIndex];
        }
    }

    void Update()
    {
        Vector3 nextPos = transform.position + (movementDirection.normalized * speed * Time.deltaTime);
        nextPos.y = baseY + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.position = nextPos;

        if (playerCamera != null)
        {
            if (!hasCollected)
            {
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

                    // ---> NEW: Spawn the popup UI!
                    if (popupPrefab != null && playerCamera != null)
                    {
                        // 1. Spawn it as a child of the Main Camera
                        GameObject popup = Instantiate(popupPrefab, playerCamera);
                        
                        // 2. Force it to sit exactly in front of the player's eyes
                        // X = 0 (Center), Y = -0.1 (Slightly below eye level), Z = 0.5 (Half a meter forward)
                        popup.transform.localPosition = new Vector3(0, -0.1f, 0.5f);
                        
                        // 3. Ensure it perfectly matches the camera's rotation
                        popup.transform.localRotation = Quaternion.identity;
                        
                        ThoughtNotification notificationScript = popup.GetComponent<ThoughtNotification>();
                        if (notificationScript != null && notificationScript.popupText != null)
                        {
                            notificationScript.popupText.text = "+ " + thoughtText.text; 
                        }
                    }

                    Destroy(gameObject);
                    return; 
                }
            }

            if (movementDirection.z < 0 && transform.position.z < playerCamera.position.z - 3f) Destroy(gameObject);
        }
        else Destroy(gameObject, 15f); 
    }
}