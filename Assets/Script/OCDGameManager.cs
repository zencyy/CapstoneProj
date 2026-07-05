using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI; 
using TMPro; 
using System.Collections;

// A custom container for the final subtitle lines
[System.Serializable]
public struct FinalDialogue
{
    [TextArea(2, 3)]
    public string text;
    public float duration;
}

public class OCDGameManager : MonoBehaviour
{
    public static OCDGameManager Instance;

    [Header("Win Conditions")]
    public int totalItemsToClean = 5;
    private int itemsCleaned = 0;

    [Header("UI Tracker")]
    public TextMeshProUGUI progressText;

    [Header("Ending Sequence")]
    [Tooltip("The text component for subtitles (can use the same one from the panic attack)")]
    public TMP_Text subtitleText;
    public AudioSource finalVoiceOver;
    public FinalDialogue[] finalDialogueLines;
    
    [Tooltip("A pure black UI Image used to fade the screen out")]
    public Image fadeToBlackImage;
    public float fadeDuration = 2f;

    [Header("Stage 3 Transition")]
    public string partySceneName = "PartyScene"; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        if (progressText != null) progressText.gameObject.SetActive(false);
        if (fadeToBlackImage != null) 
        {
            // Ensure the fade image is clear at the start of the game
            Color c = fadeToBlackImage.color;
            c.a = 0;
            fadeToBlackImage.color = c;
            fadeToBlackImage.gameObject.SetActive(false);
        }
        UpdateUI(); 
    }

    public void ShowProgressUI()
    {
        if (progressText != null) progressText.gameObject.SetActive(true);
    }

    public void ItemRestored()
    {
        itemsCleaned++;
        UpdateUI(); 
        
        if (itemsCleaned >= totalItemsToClean)
        {
            CompleteRoom();
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = "Things Organized: " + itemsCleaned + " / " + totalItemsToClean;
        }
    }

    private void CompleteRoom()
    {
        // Hide the progress text
        if (progressText != null) progressText.gameObject.SetActive(false);
        
        StartCoroutine(EndingSequence());
    }

    private IEnumerator EndingSequence()
    {
        // 1. Play Voiceover
        if (finalVoiceOver != null) finalVoiceOver.Play();

        // 2. Play Subtitles
        foreach (FinalDialogue line in finalDialogueLines)
        {
            if (subtitleText != null) subtitleText.text = line.text;
            yield return new WaitForSeconds(line.duration);
        }
        if (subtitleText != null) subtitleText.text = "";

        // 3. Fade to Black
        if (fadeToBlackImage != null)
        {
            fadeToBlackImage.gameObject.SetActive(true);
            float timer = 0;
            Color c = fadeToBlackImage.color;

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                c.a = Mathf.Lerp(0, 1, timer / fadeDuration);
                fadeToBlackImage.color = c;
                yield return null;
            }
        }
        else
        {
            yield return new WaitForSeconds(fadeDuration);
        }

        // 4. Load Concert Scene
        SceneManager.LoadScene(partySceneName);
    }
}