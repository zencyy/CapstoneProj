using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Tooltip("Make sure your XR Origin is tagged with this exact word!")]
    public string targetTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that walked into this invisible wall is the Player
        if (other.CompareTag(targetTag))
        {
            if (AnxietyMinigameManager.Instance != null && !AnxietyMinigameManager.Instance.isGameOver)
            {
                Debug.Log("Player successfully crossed the finish line!");
                AnxietyMinigameManager.Instance.WinGame();
            }
        }
    }
}