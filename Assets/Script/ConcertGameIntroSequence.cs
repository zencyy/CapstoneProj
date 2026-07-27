using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; // ---> NEW: Required for TextMeshPro

// ---> NEW: This creates a clean formatting block in your Inspector for subtitles


public class ConcertIntroSequence : MonoBehaviour
{

    [System.Serializable]
    public struct SubtitleLine
    {
        [TextArea(2, 3)] 
        public string text;
        [Tooltip("How long in seconds this specific line should stay on screen")]
        public float duration;
    }

    [Header("UI References")]
    public Image blackScreen;
    
    [Header("Subtitles")] // ---> NEW: Subtitle variables
    public TMP_Text subtitleText;
    public SubtitleLine[] dialogueLines;

    [Header("Audio")]
    public AudioSource voiceOverSource;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript; 

    [Header("Game Management")]
    public MonoBehaviour minigameSpawner;

    [Header("Timing")]
    public float fadeDuration = 2.0f;
    public float initialDelay = 1.0f;

    void Start()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        if (minigameSpawner != null) minigameSpawner.enabled = false;
        if (subtitleText != null) subtitleText.text = ""; // Ensure subtitles are empty on frame 1

        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 1f;
            blackScreen.color = c;
            blackScreen.gameObject.SetActive(true);
        }

        StartCoroutine(PlayIntroSequence());
    }

   private IEnumerator PlayIntroSequence()
    {
        yield return new WaitForSeconds(initialDelay);

        StartCoroutine(PlaySubtitles());

        if (voiceOverSource != null && voiceOverSource.clip != null)
        {
            voiceOverSource.Play();
            yield return new WaitForSeconds(voiceOverSource.clip.length);
        }
        else
        {
            Debug.LogWarning("No Voice Over assigned to Intro Sequence!");
            yield return new WaitForSeconds(3f); 
        }

        // Fade out the black screen
        if (blackScreen != null)
        {
            float elapsedTime = 0f;
            Color startColor = blackScreen.color;

            while (elapsedTime < fadeDuration)
            {
                elapsedTime += Time.deltaTime;
                float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
                blackScreen.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
                yield return null;
            }
            
            blackScreen.gameObject.SetActive(false); 
        }

        // Unlock the player and start the spawner
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        if (minigameSpawner != null) minigameSpawner.enabled = true;
        
        // ---> NEW: Tell the master manager to start the timer and meter!
        if (AnxietyMinigameManager.Instance != null)
        {
            AnxietyMinigameManager.Instance.StartMinigame();
        }
        
        Debug.Log("Intro finished! Minigame Started.");
    }

    // ---> NEW: Coroutine that handles the timing of the subtitle text
    private IEnumerator PlaySubtitles()
    {
        if (subtitleText == null || dialogueLines.Length == 0) yield break;

        foreach (SubtitleLine line in dialogueLines)
        {
            subtitleText.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }

        // Clear the text once the sequence is completely finished
        subtitleText.text = ""; 
    }
}