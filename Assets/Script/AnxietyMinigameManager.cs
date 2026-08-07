using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal; 

public class AnxietyMinigameManager : MonoBehaviour
{
    public static AnxietyMinigameManager Instance;

    [Header("Game State")]
    public bool gameHasStarted = false;

    [Header("Player Control")]
    [Tooltip("Drag your XR Locomotion or movement script here")]
    public MonoBehaviour playerMovementScript;

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

    [Header("Post Processing (VFX)")] 
    public Volume globalVolume;
    [Tooltip("How intense the color splitting gets during the voice over (0 to 1)")]
    public float targetChromaticIntensity = 1f;
    
    [Tooltip("How intense the tunnel vision vignette gets (0 to 1)")]
    public float targetVignetteIntensity = 0.8f; 
    
    public float vfxFadeDuration = 1.0f;
    
    private ChromaticAberration chromaticAberration;
    private Vignette vignette; 

    [Header("Win Sequence Settings")] 
    public AudioClip winVoiceOverClip;
    [TextArea(2, 3)]
    public string winSubtitleText;
    public AudioSource winCrowdAudioSource;
    
    // ---> NEW: Transition Text Variables
    [Tooltip("The text that fades in while the screen is black")]
    public TMP_Text transitionText; 
    [Tooltip("How long the transition text stays fully visible on screen")]
    public float transitionTextDisplayTime = 3.0f; 
    
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

    [Header("Collision UI")] 
    public Image hitFlashScreen;
    public Color hitFlashColor = new Color(0.8f, 0f, 0f, 0.6f); 
    public float flashDuration = 0.5f;

    [HideInInspector] public bool isGameOver = false;
    [HideInInspector] public bool isPhaseTwo = false;

    private bool isLowHealthHeartbeatPlaying = false; 
    private Coroutine currentSubtitleCoroutine;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerHitFlash()
    {
        if (hitFlashScreen != null)
        {
            StopCoroutine(HitFlashCoroutine()); 
            StartCoroutine(HitFlashCoroutine());
        }
    }

    private IEnumerator HitFlashCoroutine()
    {
        hitFlashScreen.gameObject.SetActive(true);
        hitFlashScreen.color = hitFlashColor;

        float timer = 0f;
        while (timer < flashDuration)
        {
            timer += Time.deltaTime;
            Color c = hitFlashColor;
            c.a = Mathf.Lerp(hitFlashColor.a, 0f, timer / flashDuration);
            hitFlashScreen.color = c;
            yield return null;
        }

        Color finalColor = hitFlashColor;
        finalColor.a = 0f;
        hitFlashScreen.color = finalColor;
        hitFlashScreen.gameObject.SetActive(false);
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
        
        // Ensure transition text starts hidden
        if (transitionText != null)
        {
            Color c = transitionText.color;
            c.a = 0f;
            transitionText.color = c;
            transitionText.gameObject.SetActive(false);
        }

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

        if (globalVolume != null && globalVolume.profile != null)
        {
            globalVolume.profile.TryGet(out chromaticAberration);
            if (chromaticAberration != null)
            {
                chromaticAberration.intensity.value = 0f;
            }

            globalVolume.profile.TryGet(out vignette);
            if (vignette != null)
            {
                vignette.intensity.value = 0f;
            }
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
        if (playerMovementScript != null) playerMovementScript.enabled = false;

        if (voiceOverSource != null && vo.clip != null)
        {
            voiceOverSource.Stop(); 
            voiceOverSource.PlayOneShot(vo.clip);
        }
        if (subtitleDisplay != null) subtitleDisplay.text = vo.subtitleText;

        StartCoroutine(AnimateVisualEffects(true, vfxFadeDuration));

        float waitTime = (vo.clip != null) ? vo.clip.length : 2.0f;
        yield return new WaitForSeconds(waitTime);

        StartCoroutine(AnimateVisualEffects(false, vfxFadeDuration));

        if (subtitleDisplay != null && subtitleDisplay.text == vo.subtitleText) subtitleDisplay.text = "";

        if (playerMovementScript != null) playerMovementScript.enabled = true;
    }

    private IEnumerator AnimateVisualEffects(bool isFadingIn, float duration)
    {
        float timer = 0f;
        
        float startCA = chromaticAberration != null ? chromaticAberration.intensity.value : 0f;
        float endCA = isFadingIn ? targetChromaticIntensity : 0f;
        
        float startVig = vignette != null ? vignette.intensity.value : 0f;
        float endVig = isFadingIn ? targetVignetteIntensity : 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            
            if (chromaticAberration != null) chromaticAberration.intensity.value = Mathf.Lerp(startCA, endCA, t);
            if (vignette != null) vignette.intensity.value = Mathf.Lerp(startVig, endVig, t);
            
            yield return null;
        }
        
        if (chromaticAberration != null) chromaticAberration.intensity.value = endCA;
        if (vignette != null) vignette.intensity.value = endVig;
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

    public void WinGame()
    {
        if (isGameOver) return;
        isGameOver = true;
        PlayerPrefs.SetInt("MinigameCompleted", 1); 
        
        StartCoroutine(WinSequenceCoroutine());
    }

    private IEnumerator WinSequenceCoroutine()
    {
        WipeLeftoverObjects(); 
        
        if (anxietyBarSlider != null) anxietyBarSlider.gameObject.SetActive(false);
        if (timerText != null) timerText.gameObject.SetActive(false);
        if (phaseTwoPromptText != null) phaseTwoPromptText.gameObject.SetActive(false);

        if (minigameBGM != null) minigameBGM.Stop();
        if (crowdAudioSource != null) crowdAudioSource.Stop();
        if (heartbeatAudio != null) heartbeatAudio.Stop(); 
        if (voiceOverSource != null) voiceOverSource.Stop();
        
        if (voiceOverSource != null && winVoiceOverClip != null)
        {
            voiceOverSource.PlayOneShot(winVoiceOverClip);
        }
        
        if (subtitleDisplay != null) 
        {
            subtitleDisplay.gameObject.SetActive(true);
            subtitleDisplay.text = winSubtitleText;
        }

        float voWaitTime = (winVoiceOverClip != null) ? winVoiceOverClip.length : 3.0f;
        yield return new WaitForSeconds(voWaitTime);
        
        if (subtitleDisplay != null) subtitleDisplay.text = "";

        if (winCrowdAudioSource != null) winCrowdAudioSource.Play();

        if (fadeScreen != null) 
        {
            fadeScreen.gameObject.SetActive(true);
            float timer = 0f;
            Color startColor = new Color(0f, 0f, 0f, 0f); 
            Color endColor = new Color(0f, 0f, 0f, 1f);   

            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                fadeScreen.color = Color.Lerp(startColor, endColor, timer / fadeDuration);
                yield return null;
            }
            fadeScreen.color = endColor;
        }

        // ---> AMENDED: Fade in the Transition Text while the screen is black
        if (transitionText != null)
        {
            transitionText.gameObject.SetActive(true);
            
            // Fade text IN
            float textTimer = 0f;
            float textFadeDuration = 1.5f;
            Color tColor = transitionText.color;
            
            while (textTimer < textFadeDuration)
            {
                textTimer += Time.deltaTime;
                tColor.a = Mathf.Lerp(0f, 1f, textTimer / textFadeDuration);
                transitionText.color = tColor;
                yield return null;
            }
            
            tColor.a = 1f;
            transitionText.color = tColor;

            // Wait and let the player read it while the crowd cheers
            yield return new WaitForSeconds(transitionTextDisplayTime);

            // Fade text OUT before jumping scenes
            textTimer = 0f;
            while (textTimer < textFadeDuration)
            {
                textTimer += Time.deltaTime;
                tColor.a = Mathf.Lerp(1f, 0f, textTimer / textFadeDuration);
                transitionText.color = tColor;
                yield return null;
            }
            
            tColor.a = 0f;
            transitionText.color = tColor;
            transitionText.gameObject.SetActive(false);
        }
        else
        {
            // Fallback if you forget to assign the text
            yield return new WaitForSeconds(waitBeforeSceneLoad);
        }

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

/*private void LoseGame()
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
    } */