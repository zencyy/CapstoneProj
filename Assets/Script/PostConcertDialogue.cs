using UnityEngine;
using UnityEngine.UI; // Required for the Image component
using TMPro;
using System.Collections;

public class PostConcertDialogue : MonoBehaviour
{
    [Header("Voiceover Setup")]
    public AudioSource voiceOverSource;
    [Tooltip("Drag your central Subtitle Text (TMP) here")]
    public TMP_Text subtitleDisplay;
    
    [Header("Darken Effect")]
    [Tooltip("Drag a full-screen black UI Image here (ensure Raycast Target is unchecked)")]
    public Image darkenScreen;
    [Range(0f, 1f)] public float targetDarkenAlpha = 0.7f;
    public float fadeDuration = 1.0f;

    [Header("Dialogue Settings")]
    [Tooltip("How many seconds to wait after the scene loads before speaking")]
    public float delayBeforeSpeaking = 2.5f;
    
    [TextArea(2, 3)]
    public string subtitleText = "I need to get out of this crowd. I just need to find the toilet...";
    public AudioClip voiceClip;

    void Start()
    {
        // Ensure subtitles are hidden right when the scene loads
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        // Ensure the darken screen is completely transparent at the start
        if (darkenScreen != null)
        {
            Color c = darkenScreen.color;
            c.a = 0f;
            darkenScreen.color = c;
            darkenScreen.gameObject.SetActive(false);
        }
        
        StartCoroutine(PlayPostGameDialogue());
    }

    private IEnumerator PlayPostGameDialogue()
    {
        // 1. Give the player a moment to breathe and let the scene fade in
        yield return new WaitForSeconds(delayBeforeSpeaking);

        // 2. Smoothly darken the screen
        if (darkenScreen != null)
        {
            darkenScreen.gameObject.SetActive(true);
            yield return StartCoroutine(FadeImageAlpha(0f, targetDarkenAlpha, fadeDuration));
        }

        // 3. Play the audio
        if (voiceOverSource != null && voiceClip != null)
        {
            voiceOverSource.PlayOneShot(voiceClip);
        }

        // 4. Display the text
        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = subtitleText;
        }

        // 5. Wait for the exact length of the audio clip (or default to 4 seconds)
        float waitTime = (voiceClip != null) ? voiceClip.length : 4.0f;
        yield return new WaitForSeconds(waitTime);

        // 6. Clear the text
        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = "";
        }

        // 7. Smoothly return the screen to normal brightness
        if (darkenScreen != null)
        {
            yield return StartCoroutine(FadeImageAlpha(targetDarkenAlpha, 0f, fadeDuration));
            darkenScreen.gameObject.SetActive(false); // Turn off to save performance
        }
    }

    // Helper Coroutine to handle the math for fading the Image
    private IEnumerator FadeImageAlpha(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color c = darkenScreen.color;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            darkenScreen.color = c;
            yield return null;
        }
        
        c.a = endAlpha;
        darkenScreen.color = c;
    }
}