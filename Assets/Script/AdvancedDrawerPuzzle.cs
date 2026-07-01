using UnityEngine;

using System.Collections;
using TMPro; // Crucial: This lets us control the TextMeshPro UI!

public class AdvancedDrawerPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [Tooltip("Drag the 3 sockets here")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] drawerSockets;
    
    [Tooltip("Drag the 3 items here IN THE EXACT SAME ORDER as the sockets above")]
    public GameObject[] expectedItems; 

    [Header("Feedback UI")]
    public GameObject feedbackCanvas; // Drag the DrawerFeedbackCanvas here
    public TMP_Text feedbackText;     // Drag the FeedbackText here
    public string successMessage = "Perfect.";
    public string errorMessage = "Order is incorrect.";

    [Header("Drawer Mechanics")]
    public UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable drawerGrabScript;
    public AudioSource successAudio;
    public AudioSource errorAudio; 

    private bool isSolved = false;
    private bool isChecking = false;
    private int lastFilledCount = 0; // Tracks how many items are currently in the drawer

    void Start()
    {
        if (drawerGrabScript != null) drawerGrabScript.enabled = false;
        if (feedbackCanvas != null) feedbackCanvas.SetActive(false);
    }

    void Update()
    {
        if (isSolved || isChecking) return;

        // Count how many sockets currently have an item in them
        int filledSockets = 0;
        foreach (var socket in drawerSockets)
        {
            if (socket.hasSelection) filledSockets++;
        }
        
        // This stops the console from lagging by only printing when the number actually changes
        if (filledSockets != lastFilledCount) 
        {
            Debug.Log("Sockets filled right now: " + filledSockets + " / " + drawerSockets.Length);
        }

        // Only evaluate on the exact moment the 3rd item is placed
        if (filledSockets == drawerSockets.Length && lastFilledCount < drawerSockets.Length)
        {
            StartCoroutine(EvaluatePuzzle());
        }
        // If they realize they are wrong and pull an item out, hide the error UI
        else if (filledSockets < drawerSockets.Length && feedbackCanvas.activeSelf)
        {
            feedbackCanvas.SetActive(false);
        }

        lastFilledCount = filledSockets;
    }

    private IEnumerator EvaluatePuzzle()
    {
        isChecking = true;
        
        // Wait half a second to let the final item snap into place visually
        yield return new WaitForSeconds(0.5f); 

        bool isCorrect = true;

        // Check if every item matches the expected slot
        for (int i = 0; i < drawerSockets.Length; i++)
        {
            GameObject itemInSocket = drawerSockets[i].firstInteractableSelected.transform.gameObject;
            
            if (itemInSocket != expectedItems[i])
            {
                isCorrect = false;
                break; 
            }
        }

        // Show the UI Canvas
        if (feedbackCanvas != null) feedbackCanvas.SetActive(true);

        if (isCorrect)
        {
            // SUCCESS
            isSolved = true;
            if (feedbackText != null) feedbackText.text = successMessage;
            if (successAudio != null) successAudio.Play();
            
            // Permanently lock the items AND attach them to the drawer so they don't fall out
            foreach (var socket in drawerSockets) 
            { 
                if (socket.hasSelection)
                {
                    GameObject item = socket.firstInteractableSelected.transform.gameObject;

                    // 1. Freeze the item's physics so gravity doesn't pull it through the drawer floor
                    Rigidbody itemRb = item.GetComponent<Rigidbody>();
                    if (itemRb != null) itemRb.isKinematic = true;

                    // 2. Prevent the player from grabbing the item again
                    var itemGrab = item.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (itemGrab != null) itemGrab.enabled = false;

                    // 3. Physically parent (glue) the item to the drawer itself
                    item.transform.SetParent(this.transform, true);
                }

                // 4. Now it's safe to turn the socket off
                socket.enabled = false; 
            }
            
            // Unlock drawer so the player can push it shut
            if (drawerGrabScript != null) drawerGrabScript.enabled = true; 

            Debug.Log("Puzzle Solved! Items glued down and Drawer unlocked.");
        }
        
        isChecking = false;
    }
}