using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro; // Required for text UI

public class OCDGameManager : MonoBehaviour
{
    public static OCDGameManager Instance;

    [Header("Win Conditions")]
    public int totalItemsToClean = 5;
    private int itemsCleaned = 0;

    [Header("UI Tracker")]
    public TextMeshProUGUI progressText; // Drag your UI text here

    [Header("Stage 3 Transition")]
    public string partySceneName = "PartyScene"; 
    public float delayBeforeTransition = 3f; 

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Set the initial text to 0 / X when the scene loads
        UpdateUI(); 
    }

    public void ItemRestored()
    {
        itemsCleaned++;
        UpdateUI(); // Update the UI every time a point is scored
        
        Debug.Log("Item restored! Total: " + itemsCleaned + "/" + totalItemsToClean);

        if (itemsCleaned >= totalItemsToClean)
        {
            CompleteRoom();
        }
    }

    private void UpdateUI()
    {
        if (progressText != null)
        {
            progressText.text = "Items Restored: " + itemsCleaned + " / " + totalItemsToClean;
        }
    }

    private void CompleteRoom()
    {
        Debug.Log("Room is perfectly clean. Triggering transition to Stage 3...");
        Invoke("LoadNextScene", delayBeforeTransition);
    }

    private void LoadNextScene()
    {
        SceneManager.LoadScene(partySceneName);
    }
}