using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinigameSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the Main Camera here so the spawner stays ahead of the player")]
    public Transform playerCamera;

    [Header("Prefabs")]
    public GameObject[] npcPrefabs; 
    public GameObject[] positiveThoughtPrefabs; // ---> NEW: Slot for collectibles
    public Transform[] lanes; 
    
    [Header("Difficulty Settings")]
    [Tooltip("How far ahead of the player should objects spawn?")]
    public float spawnDistanceAhead = 25f;
    public float initialSpawnDelay = 2.0f; 
    public float maxSpawnDelay = 1.2f; 
    public float minSpawnDelay = 0.4f; 
    
    public float minSpeed = 5f; 
    public float maxSpeed = 12f; 

    void Start()
    {
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            if (AnxietyMinigameManager.Instance != null && AnxietyMinigameManager.Instance.isGameOver) yield break; 

            float progress = AnxietyMinigameManager.Instance != null ? AnxietyMinigameManager.Instance.GetTimeProgress() : 0f;
            bool isPhaseTwo = AnxietyMinigameManager.Instance != null && AnxietyMinigameManager.Instance.isPhaseTwo;

            int objectsToSpawn = Random.Range(1, 3); 
            objectsToSpawn = Mathf.Min(objectsToSpawn, lanes.Length);
            List<Transform> availableLanes = new List<Transform>(lanes);

            for (int i = 0; i < objectsToSpawn; i++)
            {
                if (availableLanes.Count == 0) break;

                int laneIndex = Random.Range(0, availableLanes.Count);
                Transform laneData = availableLanes[laneIndex];
                availableLanes.RemoveAt(laneIndex);

                // ---> NEW: Math to spawn the object ahead of the moving player
                Vector3 dynamicSpawnPos = laneData.position;
                dynamicSpawnPos.z = playerCamera.position.z + spawnDistanceAhead;

                GameObject objToSpawn = null;

                // ---> NEW: In Phase 2, 40% chance to spawn a Positive Thought instead of an NPC
                if (isPhaseTwo && positiveThoughtPrefabs.Length > 0 && Random.value > 0.6f)
                {
                    objToSpawn = positiveThoughtPrefabs[Random.Range(0, positiveThoughtPrefabs.Length)];
                }
                else if (npcPrefabs.Length > 0)
                {
                    objToSpawn = npcPrefabs[Random.Range(0, npcPrefabs.Length)];
                }

                if (objToSpawn != null)
                {
                    GameObject spawnedObj = Instantiate(objToSpawn, dynamicSpawnPos, laneData.rotation);

                    // If it's an NPC
                    MinigameObject npcLogic = spawnedObj.GetComponent<MinigameObject>();
                    if (npcLogic != null) npcLogic.speed = Mathf.Lerp(minSpeed, maxSpeed, progress);

                    // If it's a Positive Thought
                    PositiveThought thoughtLogic = spawnedObj.GetComponent<PositiveThought>();
                    if (thoughtLogic != null) thoughtLogic.speed = Mathf.Lerp(minSpeed, maxSpeed, progress) * 0.8f; // Thoughts move slightly slower
                }
            }

            float currentWaitTime = Mathf.Lerp(maxSpawnDelay, minSpawnDelay, progress);
            yield return new WaitForSeconds(currentWaitTime);
        }
    }
}