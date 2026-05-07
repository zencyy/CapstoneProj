using UnityEngine;

public class MinigameSpawner : MonoBehaviour
{
    public GameObject npcPrefab;
    public GameObject positiveThoughtPrefab;
    public Transform[] lanes; 
    
    public float spawnInterval = 1.5f;
    private float timer;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnObject();
            timer = 0f;
        }
    }

    void SpawnObject()
    {
        int randomLane = Random.Range(0, lanes.Length);
        Transform spawnPoint = lanes[randomLane];

        // 70% chance for an NPC, 30% chance for a Positive Thought
        GameObject objToSpawn = Random.value > 0.3f ? npcPrefab : positiveThoughtPrefab;

        Instantiate(objToSpawn, spawnPoint.position, spawnPoint.rotation);
    }
}