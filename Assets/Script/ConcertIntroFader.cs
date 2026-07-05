using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ConcertIntroFader : MonoBehaviour
{
    [Header("UI References")]
    public Image blackScreen;
    public TMP_Text titleText;

    [Header("Timing")]
    [Tooltip("How long does the text stay on screen before fading?")]
    public float displayTime = 3f;
    [Tooltip("How long does it take for the scene to fade in?")]
    public float fadeOutDuration = 2.5f;

    void Start()
    {
        // 1. Force the screen to be pitch black with visible text immediately when the scene loads
        if (blackScreen != null)
        {
            Color c = blackScreen.color;
            c.a = 1f; // 1 = fully opaque
            blackScreen.color = c;
            blackScreen.gameObject.SetActive(true);
        }
        
        if (titleText != null)
        {
            Color c = titleText.color;
            c.a = 1f;
            titleText.color = c;
            titleText.gameObject.SetActive(true);
        }

        // 2. Start the fade sequence
        StartCoroutine(FadeIntroSequence());
    }

    private IEnumerator FadeIntroSequence()
    {
        // Wait while the player reads "Concert Day" in the darkness
        yield return new WaitForSeconds(displayTime);

        float timer = 0;

        // Slowly fade both the text and the black screen to transparent
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

        // Clean up and disable the UI so it doesn't block raycasts or clicking
        if (titleText != null) titleText.gameObject.SetActive(false);
        if (blackScreen != null) blackScreen.gameObject.SetActive(false);
        
        Debug.Log("Intro complete. Welcome to the concert.");
    }
}