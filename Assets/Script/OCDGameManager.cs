using UnityEngine;
using UnityEngine.SceneManagement; // Required to load the next scene

public class OCDGameManager : MonoBehaviour
{
    public static OCDGameManager Instance;

    [Header("Win Conditions")]
    public int totalItemsToClean = 5; // Set this to the number of sockets in your room
    private int itemsCleaned = 0;

    [Header("Stage 3 Transition")]
    [Tooltip("The exact name of your Stage 3 Party scene in the Build Settings")]
    public string partySceneName = "PartyScene"; 
    public float delayBeforeTransition = 3f; // Gives the player 3 seconds to admire the clean room

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void ItemRestored()
    {
        itemsCleaned++;
        Debug.Log("Item restored! Total: " + itemsCleaned + "/" + totalItemsToClean);

        if (itemsCleaned >= totalItemsToClean)
        {
            CompleteRoom();
        }
    }

    private void CompleteRoom()
    {
        Debug.Log("Room is perfectly clean. Triggering transition to Stage 3...");
        
        // This waits for a few seconds so the transition isn't instantly jarring, then loads the scene
        Invoke("LoadNextScene", delayBeforeTransition);
    }

    private void LoadNextScene()
    {
        // Remember to add your Party Scene to File > Build Settings!
        SceneManager.LoadScene(partySceneName);
    }
}