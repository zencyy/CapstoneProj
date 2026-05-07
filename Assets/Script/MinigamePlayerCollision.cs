using UnityEngine;

public class MinigamePlayerCollision : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        MinigameObject minigameObj = other.GetComponent<MinigameObject>();

        if (minigameObj != null)
        {
            // This calls the GameManager to add or subtract from the bar
            AnxietyMinigameManager.Instance.ModifyAnxiety(minigameObj.effectAmount);
            
            // Optional: Play a sound effect here based on if it's a thought or NPC
            
            Destroy(other.gameObject);
        }
    }
}