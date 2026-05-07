using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AnxietyMinigameManager : MonoBehaviour
{
    public static AnxietyMinigameManager Instance;

    [Header("Anxiety Bar Settings")]
    public float maxAnxiety = 100f;
    public float currentAnxiety;
    public float passiveDrainRate = 5f; // How much it drops per second
    public Slider anxietyBarSlider;

    [Header("Timer Settings")]
    public float timeLimit = 60f; // 60 seconds to survive
    private float timeRemaining;
    public TextMeshProUGUI timerText;

    private bool isGameOver = false;

    private void Awake()
    {
        // Singleton pattern for easy access from other scripts
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
    }

    private void Update()
    {
        if (isGameOver) return;

        // Update Timer
        timeRemaining -= Time.deltaTime;
        timerText.text = "Time: " + Mathf.Ceil(timeRemaining).ToString() + "s";

        if (timeRemaining <= 0)
        {
            WinGame();
            return;
        }

        // Passively drain anxiety bar
        currentAnxiety -= passiveDrainRate * Time.deltaTime;
        UpdateUI();

        if (currentAnxiety <= 0)
        {
            LoseGame();
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
        if (anxietyBarSlider != null)
        {
            anxietyBarSlider.value = currentAnxiety;
        }
    }

    private void WinGame()
    {
        isGameOver = true;
        Debug.Log("Player Survived! Returning to normal...");
        // Add logic to return to Hawker Centre or show victory screen
    }

    private void LoseGame()
    {
        isGameOver = true;
        Debug.Log("Anxiety Hit! Player lost.");
        // Add panic attack effects or restart scene logic
    }
}