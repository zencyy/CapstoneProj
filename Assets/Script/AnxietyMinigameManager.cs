using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;

public class AnxietyMinigameManager : MonoBehaviour
{
    public static AnxietyMinigameManager Instance;

    [Header("Game State")]
    public bool gameHasStarted = false;

    [Header("Anxiety Bar Settings")]
    public float maxAnxiety = 100f;
    public float currentAnxiety;
    public Slider anxietyBarSlider;
    public float phaseTwoDrainRate = 5f;
    public GameObject mainHUD; 

    [Header("Timer Settings")]
    public float timeLimit = 60f; 
    private float timeRemaining;
    public TextMeshProUGUI timerText;

    [Header("Phase 2 UI")]
    public TMP_Text phaseTwoPromptText;
    public float promptDisplayTime = 4.0f;

    [Header("Mid-Game Voiceovers")]
    public AudioSource voiceOverSource;
    public TMP_Text subtitleDisplay;
    public TimedVoiceOver[] timedVoiceOvers;

    [Header("Win Sequence Settings")] // ---> NEW: Variables for the cinematic ending
    public AudioClip winVoiceOverClip;
    [TextArea(2, 3)]
    public string winSubtitleText;
    public AudioSource winCrowdAudioSource;
    [Tooltip("How long to listen to the cheering black screen before loading the next scene")]
    public float waitBeforeSceneLoad = 2.0f;

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

    [HideInInspector] public bool isGameOver = false;
    [HideInInspector] public bool isPhaseTwo = false;

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
        
        if (phaseTwoPromptText != null)
        {
            Color c = phaseTwoPromptText.color;
            c.a = 0f;
            phaseTwoPromptText.color = c;
            phaseTwoPromptText.gameObject.SetActive(false);
        }

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
        if (!gameHasStarted) return;

        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString() + "s";

        if (timeRemaining <= 30f)
        {
            if (!isPhaseTwo)
            {
                isPhaseTwo = true;
                if (phaseTwoPromptText != null) StartCoroutine(DisplayPhaseTwoPrompt());
            }
            
            ModifyAnxiety(-phaseTwoDrainRate * Time.deltaTime);
        }

        foreach (TimedVoiceOver vo in timedVoiceOvers)
        {
            if (!vo.hasPlayed && timeRemaining <= vo.triggerTime)
            {
                vo.hasPlayed = true;
                if (currentSubtitleCoroutine != null) StopCoroutine(currentSubtitleCoroutine);
                currentSubtitleCoroutine = StartCoroutine(PlayVoiceOverLine(vo));
            }
        }

        if (currentAnxiety <= (maxAnxiety / 2f) && currentAnxiety > 0)
        {
            if (!isLowHealthHeartbeatPlaying && heartbeatAudio != null)
            {
                heartbeatAudio.loop = true; 
                heartbeatAudio.Play();
                isLowHealthHeartbeatPlaying = true;
            }
        }

        if (timeRemaining <= 0) LoseGame();
        if (currentAnxiety <= 0) LoseGame();
    }

    private IEnumerator DisplayPhaseTwoPrompt()
    {
        phaseTwoPromptText.gameObject.SetActive(true);
        phaseTwoPromptText.text = "Collect the positive thoughts!";
        
        float fadeTime = 1f;
        float timer = 0f;
        Color c = phaseTwoPromptText.color;

        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, timer / fadeTime);
            phaseTwoPromptText.color = c;
            yield return null;
        }
        
        c.a = 1f;
        phaseTwoPromptText.color = c;

        yield return new WaitForSeconds(promptDisplayTime);

        timer = 0f;
        while (timer < fadeTime)
        {
            timer += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, timer / fadeTime);
            phaseTwoPromptText.color = c;
            yield return null;
        }
        
        c.a = 0f;
        phaseTwoPromptText.color = c;
        phaseTwoPromptText.gameObject.SetActive(false);
    }

    private IEnumerator PlayVoiceOverLine(TimedVoiceOver vo)
    {
        if (voiceOverSource != null && vo.clip != null)
        {
            voiceOverSource.Stop(); 
            voiceOverSource.PlayOneShot(vo.clip);
        }
        if (subtitleDisplay != null) subtitleDisplay.text = vo.subtitleText;

        float waitTime = (vo.clip != null) ? vo.clip.length : 2.0f;
        yield return new WaitForSeconds(waitTime);

        if (subtitleDisplay != null && subtitleDisplay.text == vo.subtitleText) subtitleDisplay.text = "";
    }

    public void ModifyAnxiety(float amount)
    {
        if (isGameOver) return;
        currentAnxiety += amount;
        currentAnxiety = Mathf.Clamp(currentAnxiety, 0, maxAnxiety);
        if (anxietyBarSlider != null) anxietyBarSlider.value = currentAnxiety;
    }

    public float GetTimeProgress()
    {
        return 1f - (timeRemaining / timeLimit);
    }

    private void WipeLeftoverObjects()
    {
        MinigameObject[] leftoverNpcs = FindObjectsOfType<MinigameObject>();
        foreach (MinigameObject npc in leftoverNpcs) Destroy(npc.gameObject);

        PositiveThought[] thoughts = FindObjectsOfType<PositiveThought>();
        foreach (PositiveThought thought in thoughts) Destroy(thought.gameObject);
    }

    // ---> AMENDED: WinGame now triggers the Coroutine sequence
    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        PlayerPrefs.SetInt("MinigameCompleted", 1); 
        
        StartCoroutine(WinSequenceCoroutine());
    }

    // ---> NEW: Cinematic Win Sequence Coroutine
    private IEnumerator WinSequenceCoroutine()
    {
        // 1. Wipe enemies (Spawning stops automatically because isGameOver is true)
        WipeLeftoverObjects(); 
        
        // 2. Shut off the HUD and in-game intense audio
        if (mainHUD != null) mainHUD.SetActive(false);
        if (minigameBGM != null) minigameBGM.Stop();
        if (crowdAudioSource != null) crowdAudioSource.Stop();
        if (heartbeatAudio != null) heartbeatAudio.Stop(); 
        if (voiceOverSource != null) voiceOverSource.Stop();
        
        // 3. Play the specific Win Dialogue & Subtitles
        if (voiceOverSource != null && winVoiceOverClip != null)
        {
            voiceOverSource.PlayOneShot(winVoiceOverClip);
        }
        if (subtitleDisplay != null) subtitleDisplay.text = winSubtitleText;

        // Wait for the exact length of the voice over before doing anything else
        float voWaitTime = (winVoiceOverClip != null) ? winVoiceOverClip.length : 3.0f;
        yield return new WaitForSeconds(voWaitTime);
        
        // Clear the subtitle
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        // 4. Fade to Black and play the cheering crowd
        if (winCrowdAudioSource != null) winCrowdAudioSource.Play();

        if (fadeScreen != null) 
        {
            fadeScreen.gameObject.SetActive(true);
            float timer = 0f;
            Color startColor = new Color(0f, 0f, 0f, 0f); // Clear
            Color endColor = new Color(0f, 0f, 0f, 1f);   // Solid Black

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeScreen.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
                yield return null;
            }
            fadeScreen.color = endColor;
        }

        // 5. Let the player sit in the dark listening to the cheering crowd for a moment
        yield return new WaitForSeconds(waitBeforeSceneLoad);

        // 6. Finally transition to the next scene
        SceneManager.LoadScene(concertSceneName);
    }

    private void LoseGame()
    {
        isGameOver = true;
        WipeLeftoverObjects(); 
        
        if (mainHUD != null) mainHUD.SetActive(false);
        if (minigameBGM != null) minigameBGM.Stop();
        if (crowdAudioSource != null) crowdAudioSource.Stop();
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
            Color c = Color.black; c.a = 1f; fadeScreen.color = c;
        }

        if (failureUIContainer != null) failureUIContainer.SetActive(true);
    }

    public void RestartMinigame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void StartMinigame()
    {
        gameHasStarted = true;
        Debug.Log("Intro finished: Timer and Meter have now started!");
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

        c.a = endAlpha; fadeScreen.color = c;
        if (endAlpha == 0f) fadeScreen.gameObject.SetActive(false);
        onComplete?.Invoke(); 
    }
}

[System.Serializable]
public class TimedVoiceOver
{
    public float triggerTime; 
    [TextArea(2, 3)] public string subtitleText;
    public AudioClip clip;
    [HideInInspector] public bool hasPlayed = false;
}