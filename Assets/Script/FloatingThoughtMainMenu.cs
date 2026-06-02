using UnityEngine;

public class FloatingThought : MonoBehaviour
{
    [Header("Movement Settings")]
    public float floatAmplitude = 0.2f; // How high/low it bobs
    public float floatSpeed = 1f;       // How fast it bobs
    public Vector3 rotationSpeed = new Vector3(10f, 15f, 5f); // How fast it spins

    private Vector3 startPos;

    void Start()
    {
        // Remember where we placed it in the scene
        startPos = transform.position;
        
        // Randomize the start time so they don't all bob perfectly in sync
        floatSpeed += Random.Range(-0.2f, 0.2f); 
    }

    void Update()
    {
        // 1. Calculate the bobbing up and down using a Sine wave
        float newY = startPos.y + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
        transform.position = new Vector3(startPos.x, newY, startPos.z);

        // 2. Slowly rotate the object
        transform.Rotate(rotationSpeed * Time.deltaTime);
    }
}