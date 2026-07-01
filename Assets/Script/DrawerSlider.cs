using UnityEngine;

public class DrawerSlider : MonoBehaviour
{
    [Header("Sliding Setup")]
    [Tooltip("Place an empty GameObject exactly where the drawer should end up when closed, and drag it here.")]
    public Transform closedTarget;

    [Header("Locking")]
    [Tooltip("How close it needs to get to the target to lock (e.g., 0.05 meters)")]
    public float lockDistance = 0.05f;
    public AudioSource slamSound;

    private Vector3 startPos;
    private Rigidbody rb;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabScript;
    private bool isLocked = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        grabScript = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        
        // Remember exactly where the drawer is when the game starts (fully open)
        startPos = transform.position;
    }

    void LateUpdate()
    {
        // Don't do anything if it's already locked, or if the puzzle hasn't unlocked the grab script yet
        if (isLocked || closedTarget == null || grabScript == null || !grabScript.enabled) return;

        // 1. Prevent the drawer from being pulled OUT further than it started
        Vector3 directionToTarget = closedTarget.position - startPos;
        Vector3 currentDirection = transform.position - startPos;

        // If the drawer moves in the opposite direction of the target (pulling outward), teleport it back!
        if (Vector3.Dot(directionToTarget, currentDirection) < 0)
        {
            transform.position = startPos;
            if (rb != null) rb.linearVelocity = Vector3.zero; // Kill the pulling momentum
        }

        // 2. Lock it permanently when it reaches the target inside the cabinet
        float distanceToTarget = Vector3.Distance(transform.position, closedTarget.position);
        if (distanceToTarget <= lockDistance)
        {
            isLocked = true;
            grabScript.enabled = false; // Turn off the VR grab
            if (rb != null) rb.isKinematic = true; // Turn off physics so it freezes

            // Snap perfectly into the final closed position
            transform.position = closedTarget.position;

            // Play a satisfying shutting sound if you have one
            if (slamSound != null) slamSound.Play();

            Debug.Log("Drawer slammed shut and permanently locked!");
        }
    }
}