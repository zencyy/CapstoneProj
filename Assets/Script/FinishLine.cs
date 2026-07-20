using UnityEngine;

public class FinishLine : MonoBehaviour
{
    public Transform playerCamera;
    public float winDistance = 2.0f; // How close they need to get to the end

    void Start()
    {
        if (playerCamera == null && Camera.main != null) playerCamera = Camera.main.transform;
    }

    void Update()
    {
        if (playerCamera != null && AnxietyMinigameManager.Instance != null && !AnxietyMinigameManager.Instance.isGameOver)
        {
            // Only check Z distance to see if they crossed the line
            if (playerCamera.position.z >= transform.position.z)
            {
                AnxietyMinigameManager.Instance.WinGame();
            }
        }
    }
}