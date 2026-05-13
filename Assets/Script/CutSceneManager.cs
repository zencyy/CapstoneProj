using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;

public class CutsceneManager : MonoBehaviour
{
    [Header("Player Movement Setup")]
    // Drag your Locomotion System or Move Provider here
    public MonoBehaviour playerMovementScript; 

    [Header("Fade Effect")]
    public Image fadeScreen;
    public float fadeDuration = 3f;

    [Header("The Phone")]
    public AudioSource phoneAudio;
    public GameObject phoneScreenLight;

    [Header("The Bird")]
    public AudioSource birdAudio;
    public Animator birdAnimator; // Optional: If the bird flies away

    void Start()
    {
        // 1. Ensure player cannot move and screen is black when the scene loads
        playerMovementScript.enabled = false;
        fadeScreen.color = Color.black;
        phoneScreenLight.SetActive(false);

        // 2. Start the sequence
        StartCoroutine(PlayWakeUpCutscene());
    }

    private IEnumerator PlayWakeUpCutscene()
    {
        // Wait 1 second in pure darkness
        yield return new WaitForSeconds(1f);

        // Step 1: Fade from black to clear (Opening eyes)
        float elapsedTime = 0f;
        Color color = fadeScreen.color;
        
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            fadeScreen.color = color;
            yield return null;
        }

        // Wait a moment for the player to take in the ceiling
        yield return new WaitForSeconds(1.5f);

        // Step 2: The phone rings to grab their attention
        phoneAudio.Play();
        phoneScreenLight.SetActive(true); // Screen lights up
        
        // Wait for the player to theoretically look at the phone (e.g., 3 seconds)
        yield return new WaitForSeconds(3f);
        
        phoneAudio.Stop();
        phoneScreenLight.SetActive(false);

        // Wait a beat for dramatic effect
        yield return new WaitForSeconds(1f);

        // Step 3: The Bird makes a noise to draw attention to the mess
        birdAudio.Play();
        
        if (birdAnimator != null)
        {
            // Trigger a flying away animation if you have one
            birdAnimator.SetTrigger("FlyAway"); 
        }

        // Wait for the bird animation/sound to finish
        yield return new WaitForSeconds(2.5f);

        // Step 4: Cutscene over, give control back to the player
        playerMovementScript.enabled = true;
        Debug.Log("Cutscene complete. Player can now move and clean the room.");
        
        // Optional: Trigger the UI that shows the "Clean Room Reference Image" here
    }
}