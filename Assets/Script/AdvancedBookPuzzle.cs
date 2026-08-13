using UnityEngine;
using System.Collections;
using TMPro;

public class AdvancedBookPuzzle : MonoBehaviour
{
    [Header("Puzzle Configuration")]
    [Tooltip("Drag the 3 shelf sockets here in order (e.g., left to right)")]
    public UnityEngine.XR.Interaction.Toolkit.Interactors.XRSocketInteractor[] bookSockets;
    
    [Tooltip("Drag the 3 books here IN THE EXACT SAME ORDER as the sockets above")]
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
            if (filledSockets == bookSockets.Length && lastFilledCount < bookSockets.Length)
            {
                StartCoroutine(EvaluatePuzzle());
            }
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
        
        // 1. Wait a full second BEFORE touching the physics. 
        // This gives the OCDSocketValidator time to trigger its doubt mechanic and reject the book.
        yield return new WaitForSeconds(1.0f); 

        bool allFilled = true;
        bool isCorrect = true;

        for (int i = 0; i < bookSockets.Length; i++)
        {
            if (!bookSockets[i].hasSelection)
            {
                allFilled = false;
                break;
            }

            GameObject bookInSocket = bookSockets[i].firstInteractableSelected.transform.gameObject;
            if (bookInSocket != expectedBooks[i])
            {
                isCorrect = false;
            }
        }

        // If a doubt ejected a book during our 1-second wait, just cancel the check.
        if (!allFilled)
        {
            isChecking = false;
            yield break;
        }

        if (feedbackCanvas != null) feedbackCanvas.SetActive(true);

        if (isCorrect)
        {
            // SUCCESS
            isSolved = true;
            if (feedbackText != null) feedbackText.text = successMessage;
            if (successAudio != null) successAudio.Play();
            
            // ---> AMENDED: Only call this ONCE for the entire 3-book puzzle!
            if (OCDGameManager.Instance != null)
            {
                OCDGameManager.Instance.ItemRestored();
            }
            
            foreach (var socket in bookSockets) 
            { 
                if (socket.hasSelection)
                {
                    GameObject book = socket.firstInteractableSelected.transform.gameObject;

                    Transform targetAttach = socket.attachTransform != null ? socket.attachTransform : socket.transform;
                    book.transform.position = targetAttach.position;
                    book.transform.rotation = targetAttach.rotation;

                    // ---> AMENDED: Lock physics entirely BEFORE destroying the grab script so it doesn't fall!
                    Rigidbody bookRb = book.GetComponent<Rigidbody>();
                    if (bookRb != null) 
                    {
                        bookRb.isKinematic = true;
                        bookRb.useGravity = false;
                        bookRb.constraints = RigidbodyConstraints.FreezeAll;
                    }

                    var bookGrab = book.GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
                    if (bookGrab != null) Destroy(bookGrab);
                }

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