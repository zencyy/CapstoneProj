using UnityEngine;

public class KeepAwake : MonoBehaviour
{
    void Start()
    {
        // Find the physics component on the pillow
        Rigidbody rb = GetComponent<Rigidbody>();
        
        // Force it to wake up on the very first frame
        if (rb != null)
        {
            rb.WakeUp();
        }
    }
}