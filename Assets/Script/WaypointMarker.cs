using UnityEngine;

public class WaypointMarker : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to auto-detect the Main Camera")]
    public Transform playerCamera;
    
    [Tooltip("Drag the 3D object or Canvas holding your waypoint icon here")]
    public GameObject markerVisuals;

    [Header("Animation Settings")]
    [Tooltip("How fast the marker bobs up and down")]
    public float bobSpeed = 2f;
    [Tooltip("How high/low the marker travels")]
    public float bobHeight = 0.15f;
    
    private float startY;

    [Header("Arrival Settings")]
    [Tooltip("How close the player needs to get (in meters) for the marker to hide itself")]
    public float hideDistance = 2.5f;

    void Start()
    {
        // Auto-find the VR camera if not assigned
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
        
        // Remember the starting height for the bobbing math
        startY = transform.position.y;
    }

    void Update()
    {
        // 1. The Bobbing Animation
        float newY = startY + (Mathf.Sin(Time.time * bobSpeed) * bobHeight);
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);

        if (playerCamera != null)
        {
            // 2. The Billboard Effect (Always face the player)
            Vector3 lookDirection = transform.position - playerCamera.position;
            lookDirection.y = 0; // Keep the icon standing straight up, ignoring head tilt
            
            if (lookDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(lookDirection);
            }

            // 3. Hide the marker when the player reaches the toilet
            if (markerVisuals != null)
            {
                float distance = Vector3.Distance(transform.position, playerCamera.position);
                
                // If distance is greater than hideDistance, it stays active. Otherwise, it turns off.
                markerVisuals.SetActive(distance > hideDistance);
            }
        }
    }
}