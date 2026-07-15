using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

// ---> NEW: Struct to hold the timed voiceover data
[System.Serializable]
public class TimedVoiceOver
{
    [Tooltip("Play this when the timer hits this number (e.g. 20 for 20 seconds left)")]
    public float triggerTime; 
    
    [TextArea(2, 3)]
    public string subtitleText;
    
    public AudioClip clip;
    
    [HideInInspector] 
    public bool hasPlayed = false;
}

public class AnxietyMinigameManager : MonoBehaviour
{
    public static AnxietyMinigameManager Instance;

    [Header("Anxiety Bar Settings")]
    public float maxAnxiety = 100f;
    public float currentAnxiety;
    public Slider anxietyBarSlider;
    
    public GameObject mainHUD; 

    [Header("Timer Settings")]
    public float timeLimit = 30f; 
    private float timeRemaining;
    public TextMeshProUGUI timerText;

    [Header("Mid-Game Voiceovers")]
    [Tooltip("The Audio Source that will play the character's internal thoughts")]
    public AudioSource voiceOverSource;
    [Tooltip("Drag your central Subtitle Text (TMP) here")]
    public TMP_Text subtitleDisplay;
    [Tooltip("Add your specific voice lines and when they should play here")]
    public TimedVoiceOver[] timedVoiceOvers;

    [Header("Scene Transition")]
    public string concertSceneName = "ConcertScene_Part2";
    public Image fadeScreen;
    public float fadeDuration = 1.5f;

    [Header("Experiential Audio")]
    public AudioSource minigameBGM; 
    public AudioSource crowdAudioSource;
    public AudioSource heartbeatAudio;
    public AudioSource reliefAudio;

    [Header("Failure UI")]
    public GameObject failureUIContainer; 

    [HideInInspector] 
    public bool isGameOver = false;

    private bool isLowHealthHeartbeatPlaying = false; 
    private Coroutine currentSubtitleCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        currentAnxiety = maxAnxiety;
        timeRemaining = timeLimit;
        
        if (anxietyBarSlider != null)
        {
            anxietyBarSlider.maxValue = maxAnxiety;
            anxietyBarSlider.value = currentAnxiety;
        }

        if (failureUIContainer != null) failureUIContainer.SetActive(false);
        if (mainHUD != null) mainHUD.SetActive(true);
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        if (minigameBGM != null) minigameBGM.Play();

        if (fadeScreen != null)
        {
            fadeScreen.color = Color.black;
            StartCoroutine(FadeRoutine(1f, 0f, null));
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString() + "s";

        // ---> NEW: Check if it is time to play a voiceover line
        foreach (TimedVoiceOver vo in timedVoiceOvers)
        {
            if (!vo.hasPlayed && timeRemaining <= vo.triggerTime)
            {
                vo.hasPlayed = true;
                
                // Stop the previous subtitle timer if they overlap
                if (currentSubtitleCoroutine != null) StopCoroutine(currentSubtitleCoroutine);
                
                currentSubtitleCoroutine = StartCoroutine(PlayVoiceOverLine(vo));
            }
        }

        // The 50% Heartbeat Trigger
        if (currentAnxiety <= (maxAnxiety / 2f) && currentAnxiety > 0)
        {
            if (!isLowHealthHeartbeatPlaying && heartbeatAudio != null)
            {
                heartbeatAudio.loop = true; 
                heartbeatAudio.Play();
                isLowHealthHeartbeatPlaying = true;
            }
        }

        if (timeRemaining <= 0)
        {
            WinGame();
            return;
        }

        if (currentAnxiety <= 0) LoseGame();
    }

    private IEnumerator PlayVoiceOverLine(TimedVoiceOver vo)
    {
        // 1. Play the audio
        if (voiceOverSource != null && vo.clip != null)
        {
            voiceOverSource.Stop(); 
            voiceOverSource.PlayOneShot(vo.clip);
        }

        // 2. Display the text
        if (subtitleDisplay != null)
        {
            subtitleDisplay.text = vo.subtitleText;
        }

        // 3. Wait for the exact length of the audio clip (or 2 seconds if missing)
        float waitTime = (vo.clip != null) ? vo.clip.length : 2.0f;
        yield return new WaitForSeconds(waitTime);

        // 4. Clear the text (which automatically hides your black background)
        if (subtitleDisplay != null && subtitleDisplay.text == vo.subtitleText)
        {
            subtitleDisplay.text = "";
        }
    }

    public void ModifyAnxiety(float amount)
    {
        if (isGameOver) return;

        currentAnxiety += amount;
        currentAnxiety = Mathf.Clamp(currentAnxiety, 0, maxAnxiety);
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (anxietyBarSlider != null) anxietyBarSlider.value = currentAnxiety;
    }

    public float GetTimeProgress()
    {
        return 1f - (timeRemaining / timeLimit);
    }

    private void WipeLeftoverNPCs()
    {
        MinigameObject[] leftoverNpcs = FindObjectsOfType<MinigameObject>();
        foreach (MinigameObject npc in leftoverNpcs)
        {
            Destroy(npc.gameObject);
        }
    }

    private void WinGame()
    {
        isGameOver = true;
        PlayerPrefs.SetInt("MinigameCompleted", 1); 
        
        WipeLeftoverNPCs(); 
        if (mainHUD != null) mainHUD.SetActive(false);

        if (minigameBGM != null) minigameBGM.Stop();
        if (crowdAudioSource != null) crowdAudioSource.Stop();
        if (heartbeatAudio != null) heartbeatAudio.Stop(); 
        
        // ---> NEW: Cut off mid-game dialogue and subtitles if they win
        if (voiceOverSource != null) voiceOverSource.Stop();
        if (subtitleDisplay != null) subtitleDisplay.text = "";
        
        if (reliefAudio != null) reliefAudio.Play();

        if (fadeScreen != null) 
        {
            fadeScreen.color = new Color(1f, 1f, 1f, 0f); 
            StartCoroutine(FadeRoutine(0f, 1f, () => SceneManager.LoadScene(concertSceneName)));
        }
        else 
        {
            SceneManager.LoadScene(concertSceneName);
        }
    }

    private void LoseGame()
    {
        isGameOver = true;
        
        WipeLeftoverNPCs(); 
        if (mainHUD != null) mainHUD.SetActive(false);

        if (minigameBGM != null) minigameBGM.Stop();
        if (crowdAudioSource != null) crowdAudioSource.Stop();
        
        // ---> NEW: Cut off mid-game dialogue and subtitles if they lose
        if (voiceOverSource != null) voiceOverSource.Stop();
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        if (heartbeatAudio != null && !heartbeatAudio.isPlaying) 
        {
            heartbeatAudio.loop = true;
            heartbeatAudio.Play();
        }

        if (fadeScreen != null) 
        {
            fadeScreen.gameObject.SetActive(true);
            Color c = Color.black;
            c.a = 1f; 
            fadeScreen.color = c;
        }

        if (failureUIContainer != null) failureUIContainer.SetActive(true);
    }

    public void RestartMinigame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private IEnumerator FadeRoutine(float startAlpha, float endAlpha, System.Action onComplete)
    {
        fadeScreen.gameObject.SetActive(true);
        float timer = 0f;
        Color c = fadeScreen.color;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(startAlpha, endAlpha, timer / fadeDuration);
            fadeScreen.color = c;
            yield return null;
        }

        c.a = endAlpha;
        fadeScreen.color = c;

        if (endAlpha == 0f) fadeScreen.gameObject.SetActive(false);

        onComplete?.Invoke(); 
    }
}