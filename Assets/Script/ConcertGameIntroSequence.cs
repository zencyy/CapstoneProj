using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro; 

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
    
    [Header("Subtitles")] 
    public TMP_Text subtitleText;
    public SubtitleLine[] dialogueLines;

    [Header("Audio")]
    public AudioSource voiceOverSource;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript; 

    [Header("Game Management")]
    // ---> AMENDED: Changed this to GameObject so we can turn the whole object on and off
    public GameObject minigameSpawner; 

    [Header("Timing")]
    public float fadeDuration = 2.0f;
    public float initialDelay = 1.0f;

    void Start()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        
        // ---> AMENDED: Hard-disable the Spawner GameObject on frame 1
        if (minigameSpawner != null) minigameSpawner.SetActive(false); 
        
        if (subtitleText != null) subtitleText.text = ""; 

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
        
        // ---> AMENDED: Turn the spawner GameObject back on right as the simulation officially starts
        if (minigameSpawner != null) minigameSpawner.SetActive(true);
        
        if (AnxietyMinigameManager.Instance != null)
        {
            AnxietyMinigameManager.Instance.StartMinigame();
        }
        
        Debug.Log("Intro finished! Minigame Started.");
    }

    private IEnumerator PlaySubtitles()
    {
        if (subtitleText == null || dialogueLines.Length == 0) yield break;

        foreach (SubtitleLine line in dialogueLines)
        {
            subtitleText.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }

        subtitleText.text = ""; 
    }
}