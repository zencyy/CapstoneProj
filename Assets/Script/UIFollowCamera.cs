using UnityEngine;

public class UIFollowCamera : MonoBehaviour
{
    [Header("Camera Reference")]
    public Transform playerCamera;

    [Header("Position Settings")]
    [Tooltip("How far in front of the player the menu should float.")]
    public float distanceFromCamera = 2.0f;
    
    [Tooltip("How smoothly the menu catches up to the player's gaze. Lower is smoother.")]
    public float smoothSpeed = 5.0f;

    [Header("Height Settings")]
    [Tooltip("Check this if you want the menu to stay at a flat height, even if the player looks up at the ceiling or down at the floor.")]
    public bool keepFlatY = true;
    public float fixedHeightOffset = -0.2f; // Lowers it slightly below absolute eye-center

    void Start()
    {
        // Automatically find the Main Camera if you forget to assign it
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }
    }

    void LateUpdate()
    {
        if (playerCamera == null) return;

        // 1. Figure out the direction the camera is looking
        Vector3 gazeDirection = playerCamera.forward;

        // Optional: Flatten the Y axis so the menu doesn't fly into the ceiling if they look up
        if (keepFlatY)
        {
            gazeDirection.y = 0;
            gazeDirection.Normalize(); 
        }

        // 2. Calculate the exact target position in the air
        Vector3 targetPosition = playerCamera.position + (gazeDirection * distanceFromCamera);

        if (keepFlatY)
        {
            targetPosition.y = playerCamera.position.y + fixedHeightOffset;
        }

        // 3. Smoothly glide the Canvas to that position
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // 4. Smoothly rotate the Canvas so it always cleanly faces the player
        Quaternion targetRotation = Quaternion.LookRotation(transform.position - playerCamera.position);
        
        if (keepFlatY)
        {
            // Lock the tilt so the panel stays perfectly vertical
            targetRotation.x = 0;
            targetRotation.z = 0;
        }

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, smoothSpeed * Time.deltaTime);
    }
}