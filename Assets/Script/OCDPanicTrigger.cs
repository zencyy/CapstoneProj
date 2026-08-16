using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.Rendering; // ---> NEW: Required for Volumes
using UnityEngine.Rendering.Universal; // ---> NEW: Required for URP Effects

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

    [Header("Post Processing Effects")] // ---> NEW: Variables for the panic visual effects
    public Volume globalVolume;
    [Tooltip("How much the colors split/blur (0 to 1)")]
    public float maxChromaticAberration = 1f; 
    [Tooltip("How much the screen warps inwards. Negative numbers pinch the screen.")]
    public float maxLensDistortion = -0.5f; 

    private ChromaticAberration chromaticAberration;
    private LensDistortion lensDistortion;

    [Header("Objective UI")] 
    public TMP_Text objectiveText;
    [TextArea(1, 2)]
    public string objectiveString = "Everything is out of place... I need to put it back.";
    public float objectiveFadeDuration = 1.5f;
    public float objectiveStayDuration = 3.0f;

    void Start()
    {
        if (subtitleText != null) subtitleText.text = "";
        
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

        // ---> NEW: Try to grab the effects from the assigned Volume Profile
        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
            globalVolume.profile.TryGet(out lensDistortion);

            // Ensure they start completely turned off
            if (chromaticAberration != null) chromaticAberration.intensity.value = 0f;
            if (lensDistortion != null) lensDistortion.intensity.value = 0f;
        }

        StartCoroutine(PanicSequence());
    }

    private IEnumerator PanicSequence()
    {
        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = false;
        }

        yield return new WaitForSeconds(initialDelay);

        if (heartbeatAudio != null) heartbeatAudio.Play();
        if (voiceOverAudio != null) voiceOverAudio.Play();
        
        StartCoroutine(PlaySubtitles());

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

            // ---> NEW: Fade in the visual distortion alongside the audio
            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(0f, maxChromaticAberration, progress);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(0f, maxLensDistortion, progress);

            yield return null; 
        }

        float waitTime = voiceOverAudio != null ? voiceOverAudio.clip.length - fadeDuration : 2f;
        if (waitTime > 0) yield return new WaitForSeconds(waitTime);

        if (subtitleText != null) subtitleText.text = "";

        foreach (var script in movementScripts)
        {
            if (script != null) script.enabled = true;
        }

        if (OCDGameManager.Instance != null) OCDGameManager.Instance.ShowProgressUI();

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

            // ---> NEW: Fade the visual distortion back out to normal
            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(maxChromaticAberration, 0f, progress);
            if (lensDistortion != null) lensDistortion.intensity.value = Mathf.Lerp(maxLensDistortion, 0f, progress);

            yield return null;
        }

        if (heartbeatAudio != null) heartbeatAudio.Stop();

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

    private IEnumerator FadeObjectiveUI()
    {
        float timer = 0f;
        Color c = objectiveText.color;

        while (timer < objectiveFadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / objectiveFadeDuration);
            objectiveText.color = c;
            yield return null;
        }

        c.a = 1f;
        objectiveText.color = c;

        yield return new WaitForSeconds(objectiveStayDuration);

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