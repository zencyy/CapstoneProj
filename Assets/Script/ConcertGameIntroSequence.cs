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

    [Header("Objective UI")] // ---> NEW: Variables for the post-intro instruction
    public TMP_Text objectiveText;
    [TextArea(1, 2)]
    public string objectiveString = "Keep moving forward. Reach the doors.";
    public float objectiveFadeDuration = 1.5f;
    public float objectiveStayDuration = 3.0f;

    [Header("Audio")]
    public AudioSource voiceOverSource;

    [Header("Player Control")]
    public MonoBehaviour playerMovementScript; 

    [Header("Game Management")]
    public GameObject minigameSpawner; 

    [Header("Timing")]
    public float fadeDuration = 2.0f;
    public float initialDelay = 1.0f;

    void Start()
    {
        if (playerMovementScript != null) playerMovementScript.enabled = false;
        
        if (minigameSpawner != null) minigameSpawner.SetActive(false); 
        
        if (subtitleText != null) subtitleText.text = ""; 

        // ---> NEW: Ensure objective text starts completely transparent
        if (objectiveText != null) 
        {
            Color oc = objectiveText.color;
            oc.a = 0f;
            objectiveText.color = oc;
            objectiveText.text = objectiveString; 
        }

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
        
        if (minigameSpawner != null) minigameSpawner.SetActive(true);
        
        if (AnxietyMinigameManager.Instance != null)
        {
            AnxietyMinigameManager.Instance.StartMinigame();
        }
        
        Debug.Log("Intro finished! Minigame Started.");

        // ---> NEW: Trigger the objective text to fade in right as the player gets control
        if (objectiveText != null)
        {
            StartCoroutine(FadeObjectiveUI());
        }
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

    // ---> NEW: Coroutine to handle the smooth fade in, hold, and fade out of the objective
    private IEnumerator FadeObjectiveUI()
    {
        float timer = 0f;
        Color c = objectiveText.color;

        // Fade IN
        while (timer < objectiveFadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / objectiveFadeDuration);
            objectiveText.color = c;
            yield return null;
        }

        c.a = 1f;
        objectiveText.color = c;

        // Wait so the player can read it before moving
        yield return new WaitForSeconds(objectiveStayDuration);

        // Fade OUT
        timer = 0f;
        while (timer < objectiveFadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / objectiveFadeDuration);
            objectiveText.color = c;
            yield return null;
        }

        c.a = 0f;
        objectiveText.color = c;
    }
}