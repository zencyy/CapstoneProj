using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string firstSceneName = "NightBeforeScene"; // Exactly as it is spelled in Build Settings

    [Header("UI Elements")]
    public Button startButton;
    
    [Header("Fade Settings")]
    public Image fadeScreen;
    public float fadeDuration = 2f;

    // This is the function we will link to the button click
    public void OnStartClicked()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        // 1. Disable the button immediately so the player can't double-click it
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        // 2. Fade the screen to black
        float elapsedTime = 0f;
        Color fadeColor = fadeScreen.color;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeScreen.color = fadeColor;
            yield return null;
        }

        // 3. Hold on the black screen for a fraction of a second for pacing
        yield return new WaitForSeconds(0.5f);

        // 4. Load the Night Before scene
        SceneManager.LoadScene(firstSceneName);
    }
}