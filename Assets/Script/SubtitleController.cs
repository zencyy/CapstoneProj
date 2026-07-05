using UnityEngine;
using TMPro;
using System.Collections;

public class SubtitleController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Drag your SubtitleText (TextMeshPro) object here")]
    public TMP_Text subtitleText;

    [Header("Audio & Text Setup")]
    [Tooltip("The AudioSource that will play the voice over")]
    public AudioSource voiceOverAudio;
    
    [TextArea(2, 5)]
    [Tooltip("The exact text you want to appear")]
    public string dialogueText = "*yawns*, It's getting late, I should head to bed soon.";

    [Header("Timing")]
    [Tooltip("How many seconds after the scene loads should the voiceover start?")]
    public float initialDelay = 1.5f;

    void Start()
    {
        // 1. Ensure the subtitle is empty when the scene starts
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }

        // 2. Start the sequence
        if (voiceOverAudio != null && voiceOverAudio.clip != null)
        {
            StartCoroutine(PlayDialogue());
        }
        else
        {
            Debug.LogWarning("SubtitleController is missing an AudioSource or Audio Clip!");
        }
    }

    private IEnumerator PlayDialogue()
    {
        // Wait for the scene to settle before talking
        yield return new WaitForSeconds(initialDelay);

        // Turn on the subtitle text
        if (subtitleText != null)
        {
            subtitleText.text = dialogueText;
        }

        // Play the audio
        voiceOverAudio.Play();

        // Wait for the EXACT duration of the audio clip
        yield return new WaitForSeconds(voiceOverAudio.clip.length);

        // Erase the subtitle text smoothly when the audio finishes
        if (subtitleText != null)
        {
            subtitleText.text = "";
        }
    }
}