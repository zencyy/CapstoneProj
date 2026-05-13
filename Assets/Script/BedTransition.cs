using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BedTransition : MonoBehaviour
{
    [Header("Scene Settings")]
    public string wakeUpSceneName = "WakeUpScene"; // Type your exact scene name here

    [Header("Fade Settings")]
    public Image fadeScreen;
    public float fadeDuration = 2f;
    
    private bool isSleeping = false;

    // This method is called when the player clicks the bed
    public void GoToSleep()
    {
        if (!isSleeping)
        {
            StartCoroutine(SleepSequence());
        }
    }

    private IEnumerator SleepSequence()
    {
        isSleeping = true;
        float elapsedTime = 0f;
        Color fadeColor = fadeScreen.color;

        // Fade screen from clear to solid black
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            fadeColor.a = Mathf.Clamp01(elapsedTime / fadeDuration);
            fadeScreen.color = fadeColor;
            yield return null;
        }

        // Keep the screen completely black for 1 second to simulate time passing
        yield return new WaitForSeconds(1f);

        // Load the messy room scene
        SceneManager.LoadScene(wakeUpSceneName);
    }
}