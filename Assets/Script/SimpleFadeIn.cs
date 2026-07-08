using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class SimpleFadeIn : MonoBehaviour
{
    [Tooltip("Drag your full-screen black image here")]
    public Image blackScreen;
    public float fadeDuration = 1.5f;

    void Start()
    {
        if (blackScreen != null)
        {
            // Force the screen to black instantly when the scene loads
            Color c = blackScreen.color;
            c.a = 1f;
            blackScreen.color = c;
            blackScreen.gameObject.SetActive(true);

            // Start fading to clear
            StartCoroutine(FadeToClear());
        }
    }

    private IEnumerator FadeToClear()
    {
        float timer = 0f;
        Color c = blackScreen.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeDuration);
            blackScreen.color = c;
            yield return null;
        }

        c.a = 0f;
        blackScreen.color = c;
        blackScreen.gameObject.SetActive(false); // Turn it off so it doesn't block UI clicks
    }
}