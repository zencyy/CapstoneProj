using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class SubtitleBackgroundVisibility : MonoBehaviour
{
    [Tooltip("Drag your child TMP Text object here")]
    public TMP_Text subtitleText;
    
    private Image backgroundImage;

    void Start()
    {
        backgroundImage = GetComponent<Image>();
    }

    void Update()
    {
        // If the text is missing, or if the text is completely empty, turn off the black background
        if (subtitleText == null || string.IsNullOrWhiteSpace(subtitleText.text))
        {
            backgroundImage.enabled = false;
        }
        else
        {
            backgroundImage.enabled = true;
        }
    }
}