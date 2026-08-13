using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class OCDPanicTrigger : MonoBehaviour
{
    [System.Serializable]
    public struct SubtitleLine
    {
        [TextArea(2, 3)]
        public string text;
        [Tooltip("How many seconds this specific line should stay on screen")]
        public float duration;
    }
    
    [Header("UI References")]
    public Image darkScreenImage;
    public TMP_Text subtitleText;

    [Header("Audio")]
    public AudioSource voiceOverAudio;
    public AudioSource heartbeatAudio;

    [Header("Player Movement")]
    public MonoBehaviour[] movementScripts;

    [Header("Dialogue & Pacing")]
    [Tooltip("Add your split dialogue lines here in order.")]
    public SubtitleLine[] dialogueLines; 
    
    public float initialDelay = 2.0f;
    public float maxDarkness = 0.75f;

    [Header("Objective UI")] // ---> NEW: Variables for the post-panic instruction
    public TMP_Text objectiveText;
    [TextArea(1, 2)]
    public string objectiveString = "Everything is out of place... I need to put it back.";
    public float objectiveFadeDuration = 1.5f;
    public float objectiveStayDuration = 3.0f;

    void Start()
    {
        if (subtitleText != null) subtitleText.text = "";
        
        // Ensure objective text starts completely transparent
        if (objectiveText != null) 
        {
            Color oc = objectiveText.color;
            oc.a = 0;
            objectiveText.color = oc;
            objectiveText.text = objectiveString; 
        }
        
        if (darkScreenImage != null)
        {
            Color c = darkScreenImage.color;
            c.a = 0;
            darkScreenImage.color = c;
        }

        if (heartbeatAudio != null) heartbeatAudio.volume = 0;

        StartCoroutine(PanicSequence());
    }

    private IEnumerator PanicSequence()
    {
        // 0. LOCK PLAYER MOVEMENT
        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = false;
        }

        // 1. Wait for the player to wake up
        yield return new WaitForSeconds(initialDelay);

        // 2. Start the heartbeat
        if (heartbeatAudio != null) heartbeatAudio.Play();

        // 3. Play audio and start the independent subtitle coroutine
        if (voiceOverAudio != null) voiceOverAudio.Play();
        StartCoroutine(PlaySubtitles());

        // 4. Slowly fade in the darkness and the heartbeat volume
        float fadeDuration = 2.0f;
        float timer = 0;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            if (darkScreenImage != null)
            {
                Color c = darkScreenImage.color;
                c.a = Mathf.Lerp(0, maxDarkness, progress);
                darkScreenImage.color = c;
            }

            if (heartbeatAudio != null)
            {
                heartbeatAudio.volume = Mathf.Lerp(0, 1f, progress);
            }

            yield return null; 
        }

        // 5. Wait for the voiceover to completely finish talking
        float waitTime = voiceOverAudio != null ? voiceOverAudio.clip.length - fadeDuration : 2f;
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        // 6. UNLOCK PLAYER MOVEMENT
        if (subtitleText != null) subtitleText.text = "";

        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = true;
        }

        if (OCDGameManager.Instance != null) OCDGameManager.Instance.ShowProgressUI();

        // 7. Slowly fade the darkness and heartbeat back out
        timer = 0;
        fadeDuration = 3.0f; 

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeDuration;

            if (darkScreenImage != null)
            {
                Color c = darkScreenImage.color;
                c.a = Mathf.Lerp(maxDarkness, 0, progress);
                darkScreenImage.color = c;
            }

            if (heartbeatAudio != null)
            {
                heartbeatAudio.volume = Mathf.Lerp(1f, 0, progress);
            }

            yield return null;
        }

        // 8. Turn off heartbeat completely
        if (heartbeatAudio != null) heartbeatAudio.Stop();

        // ---> NEW: 9. Trigger the final objective UI sequence
        if (objectiveText != null)
        {
            StartCoroutine(FadeObjectiveUI());
        }
    }

    private IEnumerator PlaySubtitles()
    {
        foreach (SubtitleLine line in dialogueLines)
        {
            if (subtitleText != null) subtitleText.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }
        
        if (subtitleText != null) subtitleText.text = "";
    }

    // ---> NEW COROUTINE: Handles the smooth fade in, hold, and fade out of the objective
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

        // Wait so the player can read it
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