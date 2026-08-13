using UnityEngine;

public class DrawerSlider : MonoBehaviour
{
    [Header("Sliding Setup")]
    [Tooltip("Place an empty GameObject exactly where the drawer should end up when closed, and drag it here.")]
    public Transform closedTarget;

    [Header("Locking")]
    [Tooltip("How close it needs to get to the target to snap and lock (e.g., 0.08 meters)")]
    public float lockDistance = 0.08f;
    public AudioSource slamSound;

    private Vector3 startPos;
    private Rigidbody rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabScript;
    private bool isLocked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabScript = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        // Record the open position so the player can't pull it out further than this
        startPos = transform.position;
    }

    void LateUpdate()
    {
        // SAFETY CHECK: If the puzzle hasn't unlocked the grab script yet, do absolutely nothing.
        if (isLocked || closedTarget == null || grabScript == null || !grabScript.enabled) return;

        // 1. Prevent the drawer from being pulled OUT further than it started
        Vector3 directionToTarget = closedTarget.position - startPos;
        Vector3 currentDirection = transform.position - startPos;

        if (Vector3.Dot(directionToTarget, currentDirection) < 0)
        {
            transform.position = startPos;
            if (rb != null) rb.linearVelocity = Vector3.zero; 
        }

        // 2. Lock it permanently when it reaches the target inside the cabinet
        float distanceToTarget = Vector3.Distance(transform.position, closedTarget.position);
        if (distanceToTarget <= lockDistance)
        {
            LockDrawer();
        }
    }

    private void LockDrawer()
    {
        isLocked = true;
        
        grabScript.enabled = false;

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
        }

        transform.position = closedTarget.position;
        transform.rotation = closedTarget.rotation; 

        if (slamSound != null) slamSound.Play();

        // ---> AMENDED: Only update the progress meter exactly once when the drawer locks securely!
        if (OCDGameManager.Instance != null)
        {
            OCDGameManager.Instance.ItemRestored();
        }

        // ---> AMENDED: Automatically hide the puzzle UI feedback once the drawer is pushed in.
        AdvancedDrawerPuzzle puzzleScript = GetComponent<AdvancedDrawerPuzzle>();
        if (puzzleScript != null && puzzleScript.feedbackCanvas != null)
        {
            puzzleScript.feedbackCanvas.SetActive(false);
        }

        Debug.Log("Drawer snapped flush into position and locked forever! Progress updated.");
    }
}