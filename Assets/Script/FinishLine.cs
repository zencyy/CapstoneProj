using UnityEngine;

public class FinishLine : MonoBehaviour
{
    [Tooltip("Make sure your XR Origin is tagged with this exact word!")]
    public string targetTag = "Player";

    [Header("Hide Manager Link")]
    [Tooltip("Drag your HideSequenceManager here to prevent winning while hiding")]
    public HideMechanicManager hideManager; // ---> NEW: Reference to the hide manager

    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that walked into this invisible wall is the Player
        if (other.CompareTag(targetTag))
        {
            // ---> NEW: Block the win entirely if they are in the middle of a hiding event!
            if (hideManager != null && hideManager.IsHiding)
            {
                Debug.Log("Player touched the finish line, but they are currently trapped in a hiding event! Win blocked.");
                return; // This stops the rest of the code from running
            }

            // Normal win logic
            if (AnxietyMinigameManager.Instance != null && !AnxietyMinigameManager.Instance.isGameOver)
            {
                Debug.Log("Player successfully crossed the finish line!");
                AnxietyMinigameManager.Instance.WinGame();
            }
        }
    }
}