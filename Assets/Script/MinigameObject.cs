using UnityEngine;

public class MinigameObject : MonoBehaviour
{
    public float speed = 5f;
    public bool isPositiveThought;
    
    // Set this to -15 for NPCs (damage) and +20 for Thoughts (heal)
    public float effectAmount; 

    void Update()
    {
        transform.Translate(Vector3.back * speed * Time.deltaTime);

        if (transform.position.z < -10f)
        {
            Destroy(gameObject);
        }
    }
}