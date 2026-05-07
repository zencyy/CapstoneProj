using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ZoneTransition : MonoBehaviour
{
    [Header("Settings")]
    public string minigameSceneName = "MinigameScene";
    public float fadeDuration = 1.5f;

    [Header("UI References")]
    // Create a Canvas with a black UI Image stretched over the screen for this
    public Image blackFadeImage; 

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            StartCoroutine(FadeAndLoadScene());
        }
    }

    private IEnumerator FadeAndLoadScene()
    {
        float elapsedTime = 0f;
        Color fadeColor = blackFadeImage.color;

        // Fade to black
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            blackFadeImage.color = fadeColor;
            yield return null;
        }

        // Load the new scene
        SceneManager.LoadScene(minigameSceneName);
    }
}