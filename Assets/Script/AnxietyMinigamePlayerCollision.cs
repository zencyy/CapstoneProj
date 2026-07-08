using UnityEngine;

public class MinigamePlayerCollision : MonoBehaviour
{
    [Header("Hit Feedback")]
    [Tooltip("The Audio Source attached to the Player to play the sound")]
    public AudioSource playerAudioSource;
    [Tooltip("The sound effect to play when hitting an NPC")]
    public AudioClip hitSound;

    private void OnTriggerEnter(Collider other)
    {
        // We use GetComponentInParent just in case the collider is on a child bone of the 3D model
        MinigameObject minigameObj = other.GetComponentInParent<MinigameObject>();

        if (minigameObj != null)
        {
            // 1. Deplete the bar
            if (AnxietyMinigameManager.Instance != null)
            {
                AnxietyMinigameManager.Instance.ModifyAnxiety(minigameObj.effectAmount);
            }
            
            // 2. Play the sound effect from the PLAYER (If you play it from the NPC, it gets destroyed instantly!)
            if (playerAudioSource != null && hitSound != null)
            {
                playerAudioSource.PlayOneShot(hitSound);
            }
            
            // 3. Destroy the NPC
            Destroy(minigameObj.gameObject);
        }
    }
}