using UnityEngine;
using System.Collections;
using TMPro;

public class AdvancedBookPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [Tooltip("Drag the 5 shelf sockets here in order (e.g., left to right)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] bookSockets;
    
    [Tooltip("Drag the 5 books here IN THE EXACT SAME ORDER as the sockets above")]
    public GameObject[] expectedBooks; 

    [Header("Feedback UI")]
    public GameObject feedbackCanvas; 
    public TMP_Text feedbackText;     
    public string successMessage = "Books Organized.";
    public string errorMessage = "Order is incorrect.";

    [Header("Audio Feedback")]
    public AudioSource successAudio;
    public AudioSource errorAudio; 

    private bool isSolved = false;
    private bool isChecking = false;
    private int lastFilledCount = 0; 

    void Start()
    {
        if (feedbackCanvas != null) feedbackCanvas.SetActive(false);
    }

    void Update()
    {
        if (isSolved) return; 

        int filledSockets = 0;
        foreach (var socket in bookSockets)
        {
            if (socket.hasSelection) filledSockets++;
        }

        if (!isChecking)
        {
            // Only evaluate on the exact moment the 5th book is placed
            if (filledSockets == bookSockets.Length && lastFilledCount < bookSockets.Length)
            {
                StartCoroutine(EvaluatePuzzle());
            }
            // Hide error UI if they realize they are wrong and pull a book out
            else if (filledSockets < bookSockets.Length && feedbackCanvas != null && feedbackCanvas.activeSelf)
            {
                feedbackCanvas.SetActive(false);
            }
        }

        lastFilledCount = filledSockets;
    }

    private IEnumerator EvaluatePuzzle()
    {
        isChecking = true;
        
        // Wait half a second to let the final book snap into place visually
        yield return new WaitForSeconds(0.5f); 

        bool isCorrect = true;

        for (int i = 0; i < bookSockets.Length; i++)
        {
            // Safety check in case a book falls out during the 0.5s wait
            if (!bookSockets[i].hasSelection)
            {
                isCorrect = false;
                break;
            }

            GameObject bookInSocket = bookSockets[i].firstInteractableSelected.transform.gameObject;
            
            if (bookInSocket != expectedBooks[i])
            {
                isCorrect = false;
                break; 
            }
        }

        if (feedbackCanvas != null) feedbackCanvas.SetActive(true);

        if (isCorrect)
        {
            // SUCCESS
            isSolved = true;
            if (feedbackText != null) feedbackText.text = successMessage;
            if (successAudio != null) successAudio.Play();
            if (OCDGameManager.Instance != null)
            {
                for (int i = 0; i < bookSockets.Length; i++)
                {
                    OCDGameManager.Instance.ItemRestored();
                }
            }
            
            // Permanently lock the books in place
            foreach (var socket in bookSockets) 
            { 
                if (socket.hasSelection)
                {
                    GameObject book = socket.firstInteractableSelected.transform.gameObject;

                    // 1. Destroy XR Grab so the system immediately forgets about the book
                    var bookGrab = book.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (bookGrab != null) Destroy(bookGrab);

                    // 2. Destroy Rigidbody so it completely freezes in place! No physics needed.
                    Rigidbody bookRb = book.GetComponent<Rigidbody>();
                    if (bookRb != null) Destroy(bookRb);
                }

                // Safely turn off the socket
                socket.enabled = false; 
            }
            
            Debug.Log("Book Puzzle Solved! Books permanently frozen in place.");
        }
        else
        {
            // FAILED
            if (feedbackText != null) feedbackText.text = errorMessage;
            if (errorAudio != null) errorAudio.Play();
        }
        
        isChecking = false;
    }
}