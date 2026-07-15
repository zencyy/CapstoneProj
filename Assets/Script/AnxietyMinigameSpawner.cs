using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MinigameSpawner : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject[] npcPrefabs; 
    public Transform[] lanes; 
    
    [Header("Difficulty Settings")]
    public float initialSpawnDelay = 2.0f; 
    public float maxSpawnDelay = 1.2f; 
    public float minSpawnDelay = 0.15f; 
    
    public float minNpcSpeed = 7f; 
    public float maxNpcSpeed = 15f; 

    [Header("Organic Crowd Tweaks")]
    [Tooltip("How much random speed variation to give each NPC so they don't walk perfectly side-by-side")]
    public float speedVariance = 2.0f;
    [Tooltip("How far back to randomly push an NPC when they spawn so waves aren't flat walls")]
    public float maxZStagger = 3.5f;

    [Header("Pacing")]
    public AnimationCurve panicCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    void Start()
    {
        StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(initialSpawnDelay);

        while (true)
        {
            if (AnxietyMinigameManager.Instance != null && AnxietyMinigameManager.Instance.isGameOver)
            {
                yield break; 
            }

            float rawProgress = 0f;
            if (AnxietyMinigameManager.Instance != null)
            {
                rawProgress = AnxietyMinigameManager.Instance.GetTimeProgress();
            }

            float curveProgress = panicCurve.Evaluate(rawProgress);

            int npcsToSpawn = Random.Range(1, 3); 
            npcsToSpawn = Mathf.Min(npcsToSpawn, lanes.Length);

            List<Transform> availableLanes = new List<Transform>(lanes);

            for (int i = 0; i < npcsToSpawn; i++)
            {
                if (npcPrefabs.Length == 0 || availableLanes.Count == 0) break;

                int laneIndex = Random.Range(0, availableLanes.Count);
                Transform spawnPoint = availableLanes[laneIndex];
                availableLanes.RemoveAt(laneIndex);

                int randomNpcIndex = Random.Range(0, npcPrefabs.Length);
                GameObject objToSpawn = npcPrefabs[randomNpcIndex];

                GameObject spawnedObj = Instantiate(objToSpawn, spawnPoint.position, spawnPoint.rotation);

                // ---> NEW 1: THE STAGGER
                // Push the NPC slightly further back (assuming your hallway uses positive Z for distance)
                // If this pushes them forward instead of backward, change it to -randomStagger!
                float randomStagger = Random.Range(0f, maxZStagger);
                spawnedObj.transform.position += new Vector3(0, 0, randomStagger);

                MinigameObject minigameLogic = spawnedObj.GetComponent<MinigameObject>();
                if (minigameLogic != null)
                {
                    // ---> NEW 2: THE SHUFFLE
                    // Calculate the base speed, then add or subtract a random amount
                    float baseSpeed = Mathf.Lerp(minNpcSpeed, maxNpcSpeed, curveProgress);
                    float randomizedSpeed = baseSpeed + Random.Range(-speedVariance, speedVariance);
                    
                    // Clamp it just to ensure no one accidentally walks backwards or stops
                    minigameLogic.speed = Mathf.Max(3f, randomizedSpeed); 
                }
            }

            float currentWaitTime = Mathf.Lerp(maxSpawnDelay, minSpawnDelay, curveProgress);
            yield return new WaitForSeconds(currentWaitTime);
        }
    }
}