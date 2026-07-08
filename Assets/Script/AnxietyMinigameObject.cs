using UnityEngine;

public class MinigameObject : MonoBehaviour
{
    public float speed = 5f;
    public float effectAmount = -15f; 

    [Tooltip("If the model faces away from you, set this to 180. If sideways, try 90 or -90.")]
    public float modelRotationOffset = 180f;

    [HideInInspector] 
    public Transform target; // Handled by the Spawner automatically
    
    private Animator anim;
    private Vector3 moveDirection;

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
        
        if (target != null)
        {
            // 1. Calculate the exact flat direction to the player
            Vector3 directionToTarget = target.position - transform.position;
            directionToTarget.y = 0; 
            
            // 2. Save this direction so they move strictly in world space
            moveDirection = directionToTarget.normalized;

            // 3. Fix the visual rotation (Look at you, then apply the offset if the model is rigged backwards)
            if (directionToTarget != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(directionToTarget);
                transform.rotation = lookRotation * Quaternion.Euler(0, modelRotationOffset, 0);
            }
        }
        else
        {
            moveDirection = Vector3.back; // Ultimate fallback
        }
    }

    void Update()
    {
        // Move forcefully through world space along the calculated line
        transform.position += moveDirection * speed * Time.deltaTime;

        if (anim != null)
        {
            anim.speed = speed / 5f; 
        }

        // Destroy when they pass behind your headset
        if (target != null)
        {
            Vector3 localPos = target.InverseTransformPoint(transform.position);
            if (localPos.z < -2f)
            {
                Destroy(gameObject);
            }
        }
    }
}