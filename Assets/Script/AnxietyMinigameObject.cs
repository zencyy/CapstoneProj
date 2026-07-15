using UnityEngine;

public class MinigameObject : MonoBehaviour
{
    public float speed = 5f;
    public float effectAmount = -15f; 

    [Header("Movement")]
    public Vector3 movementDirection = new Vector3(0, 0, -1);
    
    [Tooltip("If the model faces away from you, set this to 180. If sideways, try 90 or -90.")]
    public float modelRotationOffset = 0f;

    [Header("Hit Detection")]
    public float hitRadius = 0.6f;
    public AudioClip hitSound;

    private Animator anim;
    private Transform playerCamera;
    private bool hasHitPlayer = false; 

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        if (Camera.main != null) playerCamera = Camera.main.transform;

        // --- NEW: FORCED ROTATION ---
        // Make the NPC visually turn its body to face the direction it is walking
        if (movementDirection != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(movementDirection.normalized);
            transform.rotation = lookRotation * Quaternion.Euler(0, modelRotationOffset, 0);
        }
    }

    void Update()
    {
        // 1. Move the NPC along the math line
        transform.position += movementDirection.normalized * speed * Time.deltaTime;

        if (anim != null) anim.speed = speed / 5f; 

        if (playerCamera != null)
        {
            // 2. Math-based hit detection
            if (!hasHitPlayer)
            {
                Vector2 npcFlatPos = new Vector2(transform.position.x, transform.position.z);
                Vector2 playerFlatPos = new Vector2(playerCamera.position.x, playerCamera.position.z);

                if (Vector2.Distance(npcFlatPos, playerFlatPos) <= hitRadius)
                {
                    hasHitPlayer = true;
                    
                    if (AnxietyMinigameManager.Instance != null)
                    {
                        AnxietyMinigameManager.Instance.ModifyAnxiety(effectAmount);
                    }

                    if (hitSound != null)
                    {
                        AudioSource.PlayClipAtPoint(hitSound, playerCamera.position);
                    }

                    Destroy(gameObject);
                    return; 
                }
            }

            // 3. Smart Destruction
            if (movementDirection.z < 0 && transform.position.z < playerCamera.position.z - 3f)
            {
                Destroy(gameObject);
            }
            else if (movementDirection.z > 0 && transform.position.z > playerCamera.position.z + 3f)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject, 15f); 
        }
    }
}