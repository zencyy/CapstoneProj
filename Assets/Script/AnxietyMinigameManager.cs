using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections; // Required for Coroutines

public class AnxietyMinigameManager : MonoBehaviour
{
    public static AnxietyMinigameManager Instance;

    [Header("Anxiety Bar Settings")]
    public float maxAnxiety = 100f;
    public float currentAnxiety;
    public Slider anxietyBarSlider;

    [Header("Timer Settings")]
    public float timeLimit = 60f; 
    private float timeRemaining;
    public TextMeshProUGUI timerText;

    [Header("Scene Transition")]
    public string concertSceneName = "ConcertScene";
    [Tooltip("Drag a Black Screen Image from your Minigame UI here")]
    public Image fadeScreen;
    public float fadeDuration = 1.5f;

    private bool isGameOver = false;

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

        // Fade in from black when the minigame starts
        if (fadeScreen != null)
        {
            StartCoroutine(FadeRoutine(1f, 0f, null));
        }
    }

    private void Update()
    {
        if (isGameOver) return;

        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString() + "s";

        if (timeRemaining <= 0)
        {
            WinGame();
            return;
        }

        if (currentAnxiety <= 0) LoseGame();
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

    private void WinGame()
    {
        isGameOver = true;
        PlayerPrefs.SetInt("MinigameCompleted", 1); 
        
        if (fadeScreen != null) StartCoroutine(FadeRoutine(0f, 1f, () => SceneManager.LoadScene(concertSceneName)));
        else SceneManager.LoadScene(concertSceneName);
    }

    private void LoseGame()
    {
        isGameOver = true;
        
        if (fadeScreen != null) StartCoroutine(FadeRoutine(0f, 1f, () => SceneManager.LoadScene(SceneManager.GetActiveScene().name)));
        else SceneManager.LoadScene(SceneManager.GetActiveScene().name); 
    }

    // Helper Coroutine to handle both fading in and fading out
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

        // If we are fading out to 0 (clear screen), turn the image off to save performance
        if (endAlpha == 0f) fadeScreen.gameObject.SetActive(false);

        // Run whatever function we passed in (like LoadScene) once the fade is fully complete
        onComplete?.Invoke();
    }
}