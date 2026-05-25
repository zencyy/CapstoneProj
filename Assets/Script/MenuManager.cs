using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scene Navigation")]
    public string firstSceneName = "NightBeforeScene"; 

    [Header("UI Elements")]
    public Button startButton;
    
    [Header("Fade Settings")]
    public Image fadeScreen;
    public float fadeDuration = 2f;

    public void OnStartClicked()
    {
        StartCoroutine(FadeOutAndLoad());
    }

    private IEnumerator FadeOutAndLoad()
    {
        if (startButton != null)
        {
            startButton.interactable = false;
        }

        float elapsedTime = 0f;
        Color fadeColor = fadeScreen.color;

        // Updated to use Mathf.Lerp exactly like your CutsceneManager
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = Mathf.Lerp(0f, 1f, elapsedTime / fadeDuration);
            fadeScreen.color = fadeColor;
            yield return null;
        }

        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene(firstSceneName);
    }
}