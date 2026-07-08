using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinigameSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag your Main Camera from the XR Origin into this slot!")]
    public Transform playerTarget;

    [Header("Prefabs")]
    public GameObject[] npcPrefabs; 
    public Transform[] lanes; 
    
    [Header("Difficulty Settings")]
    [Tooltip("How many seconds to wait before the VERY FIRST wave spawns")]
    public float initialSpawnDelay = 3.0f; // <--- NEW VARIABLE
    
    [Tooltip("The longest wait between waves (at the start of the game)")]
    public float maxSpawnDelay = 2.5f; 
    [Tooltip("The shortest wait between waves (near the end of the game)")]
    public float minSpawnDelay = 0.8f; 
    public float minNpcSpeed = 5f;
    public float maxNpcSpeed = 12f; 

    void Start()
    {
        // Start the continuous wave spawning loop
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        // ---> NEW: Wait for a few seconds before starting the first wave <---
        yield return new WaitForSeconds(initialSpawnDelay);

        // Keep looping as long as this GameObject is active
        while (true)
        {
            float progress = 0f;
            if (AnxietyMinigameManager.Instance != null)
            {
                progress = AnxietyMinigameManager.Instance.GetTimeProgress();
            }

            // 1. Determine how many NPCs to spawn this wave (1, 2, or 3)
            int npcsToSpawn = Random.Range(1, 4);
            
            // Safety check: Don't try to spawn more NPCs than we have lanes!
            npcsToSpawn = Mathf.Min(npcsToSpawn, lanes.Length);

            // 2. Create a temporary list of available lanes so they don't overlap
            List<Transform> availableLanes = new List<Transform>(lanes);

            for (int i = 0; i < npcsToSpawn; i++)
            {
                if (npcPrefabs.Length == 0 || availableLanes.Count == 0 || playerTarget == null) break;

                // Pick a random lane, then remove it from the available list for this wave
                int laneIndex = Random.Range(0, availableLanes.Count);
                Transform spawnPoint = availableLanes[laneIndex];
                availableLanes.RemoveAt(laneIndex);

                // Pick a random NPC
                int randomNpcIndex = Random.Range(0, npcPrefabs.Length);
                GameObject objToSpawn = npcPrefabs[randomNpcIndex];

                // Spawn the NPC
                GameObject spawnedObj = Instantiate(objToSpawn, spawnPoint.position, spawnPoint.rotation);

                MinigameObject minigameLogic = spawnedObj.GetComponent<MinigameObject>();
                if (minigameLogic != null)
                {
                    minigameLogic.target = playerTarget;
                    minigameLogic.speed = Mathf.Lerp(minNpcSpeed, maxNpcSpeed, progress);
                }
            }

            // 3. Calculate how long to wait before the next wave (gets faster over time)
            float currentWaitTime = Mathf.Lerp(maxSpawnDelay, minSpawnDelay, progress);
            
            yield return new WaitForSeconds(currentWaitTime);
        }
    }
}