using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

// 1. NEW: We create a custom data container to hold both the text and its timing
[System.Serializable]
public struct SubtitleLine
{
    [TextArea(2, 3)]
    public string text;
    [Tooltip("How many seconds this specific line should stay on screen")]
    public float duration;
}

public class OCDPanicTrigger : MonoBehaviour
{
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
    public SubtitleLine[] dialogueLines; // <--- This replaces the single string
    
    public float initialDelay = 2.0f;
    public float maxDarkness = 0.75f;

    void Start()
    {
        if (subtitleText != null) subtitleText.text = "";
        
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
    }

    // NEW COROUTINE: This handles the subtitles independently of the screen fading
    private IEnumerator PlaySubtitles()
    {
        foreach (SubtitleLine line in dialogueLines)
        {
            if (subtitleText != null) subtitleText.text = line.text;
            
            // Wait for this specific line's duration before moving to the next
            yield return new WaitForSeconds(line.duration);
        }
        
        // Clear the text just in case the audio is slightly longer than the subtitle durations
        if (subtitleText != null) subtitleText.text = "";
    }
}