using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ToiletTransitionManager : MonoBehaviour
{
    [Header("Player & Positioning")]
    [Tooltip("Leave empty to auto-detect the VR Headset")]
    public Transform playerCamera;
    [Tooltip("Drag your XR Origin here so we can teleport the whole rig")]
    public Transform xrOrigin;
    [Tooltip("Create an empty GameObject where the player should sit, and drag it here")]
    public Transform sittingPosition;

    [Header("Transition Settings")]
    [Tooltip("How close the player's head needs to be to this object to trigger the transition")]
    public float triggerDistance = 1.5f;
    [Tooltip("Drag your full-screen black Image here")]
    public Image fadeScreen;
    public float fadeDuration = 1.0f;

    [Header("Post-Transition Dialogue")]
    public AudioSource voiceOverSource;
    public TMP_Text subtitleDisplay;
    [TextArea(2, 3)]
    public string subtitleText = "Okay... just breathe. It's over. I'm okay.";
    public AudioClip voiceClip;

    private bool hasTriggered = false;

    void Start()
    {
        if (playerCamera == null && Camera.main != null)
        {
            playerCamera = Camera.main.transform;
        }

        if (subtitleDisplay != null) subtitleDisplay.text = "";
    }

    void Update()
    {
        // Distance check: If the player gets close enough, trigger the cinematic!
        if (!hasTriggered && playerCamera != null)
        {
            // Only check flat X/Z distance so height doesn't break the trigger
            Vector2 playerFlat = new Vector2(playerCamera.position.x, playerCamera.position.z);
            Vector2 triggerFlat = new Vector2(transform.position.x, transform.position.z);

            if (Vector2.Distance(playerFlat, triggerFlat) <= triggerDistance)
            {
                hasTriggered = true;
                StartCoroutine(ExecuteSittingTransition());
            }
        }
    }

    private IEnumerator ExecuteSittingTransition()
    {
        // 1. Fade to Black
        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            yield return StartCoroutine(FadeImageAlpha(0f, 1f, fadeDuration));
        }

        // 2. Teleport the player!
        if (xrOrigin != null && sittingPosition != null)
        {
            // Move the base of the XR rig to the target spot
            xrOrigin.position = sittingPosition.position;
            // Rotate the rig so the player is facing the stall door
            xrOrigin.rotation = sittingPosition.rotation;
        }

        // 3. Wait in the dark for 2 seconds (simulates the time it takes to enter the stall and sit)
        yield return new WaitForSeconds(2.0f);

        // 4. Fade back in
        if (fadeScreen != null)
        {
            yield return StartCoroutine(FadeImageAlpha(1f, 0f, fadeDuration));
            fadeScreen.gameObject.SetActive(false);
        }

        // 5. Play the grounding dialogue
        if (voiceOverSource != null && voiceClip != null)
        {
            voiceOverSource.PlayOneShot(voiceClip);
        }

        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = subtitleText;
        }

        float waitTime = (voiceClip != null) ? voiceClip.length : 4.0f;
        yield return new WaitForSeconds(waitTime);

        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = "";
        }
    }

    private IEnumerator FadeImageAlpha(float startAlpha, float endAlpha, float duration)
    {
        float timer = 0f;
        Color c = fadeScreen.color;
        
        while (timer < duration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / duration);
            fadeScreen.color = c;
            yield return null;
        }
        
        c.a = endAlpha;
        fadeScreen.color = c;
    }
}