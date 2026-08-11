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
    public GameObject[] positiveThoughtPrefabs; 
    public Transform[] lanes; 
    
    [Header("Difficulty Settings")]
    [Tooltip("How far ahead of the player should objects spawn?")]
    public float spawnDistanceAhead = 25f;
    public float initialSpawnDelay = 2.0f; 
    public float maxSpawnDelay = 1.2f; 
    public float minSpawnDelay = 0.4f; 
    
    public float minSpeed = 5f; 
    public float maxSpeed = 12f; 

    [Header("Cruel Difficulty")]
    [Tooltip("At max difficulty, what is the chance (0.0 to 1.0) to spawn objects in ALL lanes?")]
    public float maxWallSpawnChance = 0.3f; 

    [Header("Dynamic Spawning")] 
    [Tooltip("How much to stagger the objects forward/backward so they don't form a perfect horizontal line")]
    public float zStaggerAmount = 3.0f; 
    
    [Tooltip("Maximum delay in seconds before an individual object spawns within a wave")]
    public float maxTimeStagger = 0.4f; 

    // ---> AMENDED: Changed Start() to OnEnable() so the spawner reboots after being teleported
    void OnEnable()
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

            float currentWallChance = Mathf.Lerp(0f, maxWallSpawnChance, progress);
            int objectsToSpawn = 1;

            if (Random.value <= currentWallChance)
            {
                objectsToSpawn = lanes.Length; 
            }
            else
            {
                objectsToSpawn = Random.Range(1, lanes.Length); 
            }

            List<Transform> availableLanes = new List<Transform>(lanes);

            for (int i = 0; i < objectsToSpawn; i++)
            {
                if (availableLanes.Count == 0) break;

                int laneIndex = Random.Range(0, availableLanes.Count);
                Transform laneData = availableLanes[laneIndex];
                availableLanes.RemoveAt(laneIndex);

                GameObject objToSpawn = null;

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
                    float randomDelay = Random.Range(0f, maxTimeStagger);
                    StartCoroutine(SpawnSingleObjectDelayed(objToSpawn, laneData, progress, randomDelay));
                }
            }

            float currentWaitTime = Mathf.Lerp(maxSpawnDelay, minSpawnDelay, progress);
            yield return new WaitForSeconds(currentWaitTime);
        }
    }

    private IEnumerator SpawnSingleObjectDelayed(GameObject prefab, Transform laneData, float progress, float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
        }

        if (AnxietyMinigameManager.Instance != null && AnxietyMinigameManager.Instance.isGameOver) yield break;
        if (prefab == null || laneData == null) yield break;

        Vector3 dynamicSpawnPos = laneData.position;
        float randomOffset = Random.Range(-zStaggerAmount, zStaggerAmount);
        dynamicSpawnPos.z = playerCamera.position.z + spawnDistanceAhead + randomOffset;

        GameObject spawnedObj = Instantiate(prefab, dynamicSpawnPos, laneData.rotation);

        MinigameObject npcLogic = spawnedObj.GetComponent<MinigameObject>();
        if (npcLogic != null) npcLogic.speed = Mathf.Lerp(minSpeed, maxSpeed, progress);

        PositiveThought thoughtLogic = spawnedObj.GetComponent<PositiveThought>();
        if (thoughtLogic != null) thoughtLogic.speed = Mathf.Lerp(minSpeed, maxSpeed, progress) * 0.8f; 
    }
}