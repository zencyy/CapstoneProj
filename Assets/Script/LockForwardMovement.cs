using UnityEngine;

public class LockForwardMovement : MonoBehaviour
{
    [Header("Left/Right Boundaries")]
    [Tooltip("How far LEFT the player can move before hitting the invisible wall")]
    public float maxLeftX = -4.5f;
    
    [Tooltip("How far RIGHT the player can move before hitting the invisible wall")]
    public float maxRightX = 4.5f;

    private float startingZPosition;

    void Start()
    {
        // Remember exactly where the player spawned on the Z axis
        startingZPosition = transform.position.z;
    }

    void LateUpdate()
    {
        Vector3 lockedPosition = transform.position;
        
        // 1. Prevent moving Forward/Backward
        lockedPosition.z = startingZPosition;
        
        // 2. Prevent moving out of the sphere (Clamps the X value between your min and max)
        lockedPosition.x = Mathf.Clamp(lockedPosition.x, maxLeftX, maxRightX);
        
        // Apply the corrected position
        transform.position = lockedPosition;
    }
}