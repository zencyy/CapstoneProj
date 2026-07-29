using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ConcertIntroFader : MonoBehaviour
{
    [Header("Debug Settings")]
    [Tooltip("Check this box to force the intro to play, ignoring saved PlayerPrefs.")]
    public bool forcePlayIntro = true; // ---> NEW: Check this in the Inspector!

    [Header("Intro UI References")]
    public Image blackScreen;
    public TMP_Text titleText;

    [Header("Player Control")]
    public GameObject locomotionSystem;

    [Header("Intro Timing")]
    public float displayTime = 3f;
    public float fadeOutDuration = 2.5f;

    [Header("Anxiety Visual Effect")]
    [Range(0f, 1f)] public float dialogueDarkenAlpha = 0.7f;
    public float darkenFadeDuration = 1.5f;

    [Header("Hallway Anxiety Dialogue")]
    public float delayBeforeSpeaking = 2.0f;
    public TMP_Text subtitleDisplay;
    public AudioSource voiceOverAudio;
    public ConcertSubtitle[] dialogueLines;

    [Header("Crowd Audio Settings")]
    public AudioSource crowdAudioSource;
    [Range(0f, 1f)] public float softCrowdVolume = 0.2f;
    [Range(0f, 1f)] public float normalCrowdVolume = 1.0f;
    public float crowdFadeDuration = 2.0f;

    void Start()
    {
        // Force the screen to be pitch black immediately
        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 1f; 
            blackScreen.color = c;
            blackScreen.gameObject.SetActive(true);
        }

        // Check if we are returning from the minigame
        int minigameStatus = PlayerPrefs.GetInt("MinigameCompleted", 0);

        // ---> AMENDED: Now it checks if you have forced the intro to play for testing
        if (minigameStatus == 1 && !forcePlayIntro)
        {
            // We just beat the minigame! Skip the intro and just fade in.
            StartCoroutine(ReturnFromMinigameSequence());
        }
        else
        {
            // First time loading. Play the normal intro.
            if (titleText != null)
            {
                Color c = titleText.color;
                c.a = 1f;
                titleText.color = c;
                titleText.gameObject.SetActive(true);
            }
            if (subtitleDisplay != null) subtitleDisplay.text = "";
            StartCoroutine(ConcertStartSequence());
        }
    }

    private IEnumerator ReturnFromMinigameSequence()
    {
        // Instantly unlock movement and ensure titles are hidden
        if (locomotionSystem != null) locomotionSystem.SetActive(true);
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        // Play the normal crowd volume immediately
        if (crowdAudioSource != null)
        {
            crowdAudioSource.volume = normalCrowdVolume;
            if (!crowdAudioSource.isPlaying) crowdAudioSource.Play();
        }

        // Just fade the black screen away smoothly
        if (blackScreen != null)
        {
            yield return StartCoroutine(FadeImageAlpha(blackScreen, 1f, 0f, 1.5f));
        }
    }

    private IEnumerator ConcertStartSequence()
    {
        if (locomotionSystem != null) locomotionSystem.SetActive(false);

        if (crowdAudioSource != null)
        {
            crowdAudioSource.volume = softCrowdVolume;
            if (!crowdAudioSource.isPlaying) crowdAudioSource.Play();
        }

        yield return new WaitForSeconds(displayTime);

        float timer = 0;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            float progress = timer / fadeOutDuration;

            if (titleText != null)
            {
                Color tc = titleText.color;
                tc.a = Mathf.Lerp(1, 0, progress);
                titleText.color = tc;
            }

            if (blackScreen != null)
            {
                Color bc = blackScreen.color;
                bc.a = Mathf.Lerp(1, 0, progress);
                blackScreen.color = bc;
            }
            yield return null;
        }

        if (titleText != null) titleText.gameObject.SetActive(false);
        
        yield return new WaitForSeconds(delayBeforeSpeaking);

        if (blackScreen != null)
        {
            blackScreen.gameObject.SetActive(true);
            StartCoroutine(FadeImageAlpha(blackScreen, 0f, dialogueDarkenAlpha, darkenFadeDuration));
        }

        if (voiceOverAudio != null) voiceOverAudio.Play();

        foreach (ConcertSubtitle line in dialogueLines)
        {
            if (subtitleDisplay != null) subtitleDisplay.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }
        
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        if (blackScreen != null)
        {
            StartCoroutine(FadeImageAlpha(blackScreen, dialogueDarkenAlpha, 0f, darkenFadeDuration));
        }

        if (locomotionSystem != null) locomotionSystem.SetActive(true);

        if (crowdAudioSource != null)
        {
            StartCoroutine(FadeAudio(crowdAudioSource, softCrowdVolume, normalCrowdVolume, crowdFadeDuration));
        }
    }

    private IEnumerator FadeAudio(AudioSource audioSource, float startVol, float endVol, float duration)
    {
        if (audioSource == null) yield break;
        float timer = 0f;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(startVol, endVol, timer / duration);
            yield return null;
        }
        audioSource.volume = endVol;
    }

    private IEnumerator FadeImageAlpha(Image img, float startAlpha, float endAlpha, float duration)
    {
        if (img == null) yield break;
        float timer = 0f;
        Color c = img.color;
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            img.color = c;
            yield return null;
        }
        c.a = endAlpha;
        img.color = c;
        if (endAlpha == 0f) img.gameObject.SetActive(false);
    }
}

[System.Serializable]
public struct ConcertSubtitle
{
    [TextArea(2, 3)]
    public string text;
    public float duration;
}