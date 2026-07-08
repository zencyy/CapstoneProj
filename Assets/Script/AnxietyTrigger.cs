using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class MinigameTrigger : MonoBehaviour
{
    public string minigameSceneName = "AnxietyMinigameScene";
    
    [Header("Transition Settings")]
    [Tooltip("Drag your Black Screen Image here")]
    public Image fadeScreen; 
    public float fadeDuration = 1.5f;

    private bool isTransitioning = false;

    private void OnTriggerEnter(Collider other)
    {
        // Make sure it's the player and we haven't already started fading
        if (other.CompareTag("Player") && !isTransitioning)
        {
            StartCoroutine(TransitionToMinigame());
        }
    }

    private IEnumerator TransitionToMinigame()
    {
        isTransitioning = true;

        if (fadeScreen != null)
        {
            fadeScreen.gameObject.SetActive(true);
            float timer = 0;
            Color c = fadeScreen.color;

            // Fade from transparent (0) to black (1)
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, timer / fadeDuration);
                fadeScreen.color = c;
                yield return null;
            }
            c.a = 1;
            fadeScreen.color = c;
        }

        // Load the scene after it is completely black
        SceneManager.LoadScene(minigameSceneName);
    }
}