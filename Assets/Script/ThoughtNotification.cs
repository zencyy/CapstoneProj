using UnityEngine;
using TMPro;
using System.Collections;

public class ThoughtNotification : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text popupText;

    [Header("Animation Settings")]
    public float floatSpeed = 0.2f; // Slower speed since it's very close to the face
    public float fadeDuration = 2.0f;

    void Start()
    {
        StartCoroutine(AnimateAndDestroy());
    }

    void Update()
    {
        // Float gently upwards relative to the camera's current angle
        transform.localPosition += Vector3.up * floatSpeed * Time.deltaTime;
    }

    private IEnumerator AnimateAndDestroy()
    {
        if (popupText == null) yield break;

        Color startColor = popupText.color;
        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            float alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            popupText.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            
            yield return null;
        }

        Destroy(gameObject);
    }
}